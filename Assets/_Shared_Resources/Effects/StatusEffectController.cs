using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages active status effects on an entity. Server-authoritative: effects
/// tick on the server in <see cref="FixedUpdate"/>. Aggregated state (speed
/// multiplier, movement/attack locks) is synced to clients via NetworkVariables
/// so client-authoritative movement (Player) can read effect modifiers.
/// VFX is replicated via ClientRpc — the server does not spawn VFX locally.
/// </summary>
[RequireComponent(typeof(NetworkEntity))]
public class StatusEffectController : NetworkBehaviour
{
    [System.Serializable]
    public class EffectVfxEntry
    {
        [Tooltip("Which effect kind this VFX represents.")]
        public EffectKind kind;
        [Tooltip("Prefab to spawn at the entity's VfxAnchor when this effect is active.")]
        public GameObject prefab;
    }

    [Header("VFX Mapping (per-entity)")]
    [Tooltip("Maps effect kinds to VFX prefabs. Assign one entry per effect that should show visuals on this entity.")]
    [SerializeField] private EffectVfxEntry[] vfxEntries;

    private List<StatusEffect> activeEffects = new List<StatusEffect>();
    private IStatusTarget target;

    // Aggregated state synced to clients — read by GetSpeedMultiplier / IsMovementLocked / IsAttackLocked.
    private NetworkVariable<float> netSpeedMultiplier = new NetworkVariable<float>(
        1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> netMovementLocked = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> netAttackLocked = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Client-side tracking of spawned VFX instances, keyed by effect kind.
    private Dictionary<EffectKind, GameObject> activeVfx = new Dictionary<EffectKind, GameObject>();
    private Dictionary<EffectKind, GameObject> vfxPrefabs = new Dictionary<EffectKind, GameObject>();

    public NetworkEntity Owner { get; private set; }

    // ------------------------------------------------------------------ //
    //  Lifecycle
    // ------------------------------------------------------------------ //

    void Awake()
    {
        Owner = GetComponent<NetworkEntity>();
    }

    public override void OnNetworkSpawn()
    {
        target = Owner as IStatusTarget;
        BuildVfxLookup();

        if (IsServer)
            RefreshAggregatedState();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            ClearAllEffects();

        // Clean up any client-side VFX
        ClearAllVfxClient();
    }

    private void BuildVfxLookup()
    {
        vfxPrefabs.Clear();
        if (vfxEntries == null) return;
        foreach (var entry in vfxEntries)
        {
            if (entry.prefab != null)
                vfxPrefabs[entry.kind] = entry.prefab;
        }
    }

    // ------------------------------------------------------------------ //
    //  Ticking (server-only)
    // ------------------------------------------------------------------ //

    void FixedUpdate()
    {
        if (!IsServer) return;
        TickEffects();
    }

    private void TickEffects()
    {
        float dt = Time.fixedDeltaTime;
        bool changed = false;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeEffects[i];

            // Indefinite effects don't expire by timer — only via RemoveEffect.
            if (!effect.IsIndefinite)
            {
                effect.RemainingDuration -= dt;
                if (effect.RemainingDuration <= 0f)
                {
                    effect.OnExpire(target, this);
                    RemoveVfxForKind(effect.Data.kind);
                    activeEffects.RemoveAt(i);
                    changed = true;
                    continue;
                }
            }

            if (effect.Data.tickRate > 0f)
            {
                effect.TickTimer += dt;
                while (effect.TickTimer >= effect.Data.tickRate)
                {
                    effect.TickTimer -= effect.Data.tickRate;
                    effect.OnTick(target, this);
                }
            }
        }

        if (changed)
            RefreshAggregatedState();
    }

    // ------------------------------------------------------------------ //
    //  Public API (server-only for mutations)
    // ------------------------------------------------------------------ //

    /// <summary>Apply a status effect to this entity. Server-only.</summary>
    /// <param name="data">The effect data asset.</param>
    /// <param name="duration">Override duration. &lt;=0 uses the SO default; if SO duration is also 0, the effect is indefinite.</param>
    /// <param name="source">The entity that applied the effect (for damage attribution, knockback direction).</param>
    /// <param name="damagePerTickOverride">Override DoT damage. &lt;=0 uses the SO default.</param>
    public void ApplyEffect(StatusEffectSO data, float duration, NetworkEntity source, int damagePerTickOverride = 0)
    {
        if (!IsServer || data == null) return;

        // Handle stacking with existing effect of the same kind.
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].Data.kind != data.kind) continue;

            if (data.stacking == EffectStacking.RefreshDuration)
            {
                // Reset timer + damage on existing effect — no new instance, no OnApply/OnExpire.
                activeEffects[i].RemainingDuration = duration > 0f ? duration
                    : (data.duration > 0f ? data.duration : float.MaxValue);
                if (damagePerTickOverride > 0)
                    activeEffects[i].DamagePerTick = damagePerTickOverride;
                RefreshAggregatedState();
                return;
            }

            // Replace: expire old effect first.
            activeEffects[i].OnExpire(target, this);
            RemoveVfxForKind(data.kind);
            activeEffects.RemoveAt(i);
            break;
        }

        StatusEffect effect = StatusEffect.Create(data, source, duration, damagePerTickOverride);
        activeEffects.Add(effect);
        effect.OnApply(target, this);

        // Skip VFX for near-instant effects (knockback) — visually instant, avoids orphaned VFX.
        if (effect.IsIndefinite || effect.RemainingDuration >= 0.1f)
            SpawnVfxForKind(data.kind);

        RefreshAggregatedState();
    }

    /// <summary>Remove the first active effect of the given kind. Server-only.</summary>
    public void RemoveEffect(EffectKind kind)
    {
        if (!IsServer) return;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].Data.kind != kind) continue;
            activeEffects[i].OnExpire(target, this);
            RemoveVfxForKind(kind);
            activeEffects.RemoveAt(i);
            RefreshAggregatedState();
            return;
        }
    }

    /// <summary>True if an effect of the given kind is currently active. Server reads the active list directly; clients approximate via aggregated NetworkVariable state (may be imprecise for overlapping opposing effects — use for UI/cosmetics, not gameplay logic on clients).</summary>
    public bool HasEffect(EffectKind kind)
    {
        if (IsServer)
        {
            foreach (var e in activeEffects)
                if (e.Data.kind == kind) return true;
            return false;
        }
        // Clients can't read the active list — approximate via aggregated state.
        if (kind == EffectKind.Freeze || kind == EffectKind.Stun) return netMovementLocked.Value;
        if (kind == EffectKind.Slow) return netSpeedMultiplier.Value < 1f;
        return false;
    }

    /// <summary>Remove all active effects. Server-only.</summary>
    public void ClearAllEffects()
    {
        if (!IsServer) return;
        foreach (var e in activeEffects)
        {
            e.OnExpire(target, this);
            RemoveVfxForKind(e.Data.kind);
        }
        activeEffects.Clear();
        RefreshAggregatedState();
    }

    /// <summary>Aggregated speed multiplier from all active effects. Works on server and clients.</summary>
    public float GetSpeedMultiplier()
    {
        if (IsServer)
        {
            float mult = 1f;
            foreach (var e in activeEffects)
                mult *= e.Data.speedMultiplier;
            return mult;
        }
        return netSpeedMultiplier.Value;
    }

    /// <summary>True if any active effect locks movement. Works on server and clients.</summary>
    public bool IsMovementLocked()
    {
        if (IsServer)
        {
            foreach (var e in activeEffects)
                if (e.Data.stopsMovement) return true;
            return false;
        }
        return netMovementLocked.Value;
    }

    /// <summary>True if any active effect locks attacks. Works on server and clients.</summary>
    public bool IsAttackLocked()
    {
        if (IsServer)
        {
            foreach (var e in activeEffects)
                if (e.Data.stopsAttack) return true;
            return false;
        }
        return netAttackLocked.Value;
    }

    /// <summary>Get the source entity of the first active effect of the given kind. Server-only.</summary>
    public NetworkEntity GetSource(EffectKind kind)
    {
        foreach (var e in activeEffects)
            if (e.Data.kind == kind) return e.Source;
        return null;
    }

    // ------------------------------------------------------------------ //
    //  Aggregated state sync (server-only)
    // ------------------------------------------------------------------ //

    private void RefreshAggregatedState()
    {
        if (!IsServer) return;

        float speedMult = 1f;
        bool moveLocked = false;
        bool attackLocked = false;

        foreach (var e in activeEffects)
        {
            speedMult *= e.Data.speedMultiplier;
            if (e.Data.stopsMovement) moveLocked = true;
            if (e.Data.stopsAttack) attackLocked = true;
        }

        netSpeedMultiplier.Value = speedMult;
        netMovementLocked.Value = moveLocked;
        netAttackLocked.Value = attackLocked;
    }

    // ------------------------------------------------------------------ //
    //  VFX replication (server calls → clients spawn/destroy)
    // ------------------------------------------------------------------ //

    private void SpawnVfxForKind(EffectKind kind)
    {
        // Skip RPC if no VFX prefab is configured for this kind on this entity.
        if (!vfxPrefabs.ContainsKey(kind)) return;
        // Server tells all clients (including host) to spawn the VFX.
        SpawnEffectVfxClientRpc((int)kind);
    }

    private void RemoveVfxForKind(EffectKind kind)
    {
        if (!vfxPrefabs.ContainsKey(kind)) return;
        RemoveEffectVfxClientRpc((int)kind);
    }

    [ClientRpc]
    private void SpawnEffectVfxClientRpc(int kindIndex)
    {
        SpawnVfxLocal((EffectKind)kindIndex);
    }

    [ClientRpc]
    private void RemoveEffectVfxClientRpc(int kindIndex)
    {
        RemoveVfxLocal((EffectKind)kindIndex);
    }

    private void SpawnVfxLocal(EffectKind kind)
    {
        if (activeVfx.ContainsKey(kind)) return; // already showing
        if (!vfxPrefabs.TryGetValue(kind, out GameObject prefab) || prefab == null) return;
        if (target == null) return;

        GameObject vfx = Instantiate(prefab, target.VfxAnchor);
        vfx.transform.localPosition = Vector3.zero;
        activeVfx[kind] = vfx;
    }

    private void RemoveVfxLocal(EffectKind kind)
    {
        if (!activeVfx.TryGetValue(kind, out GameObject vfx)) return;
        activeVfx.Remove(kind);
        if (vfx != null) Destroy(vfx);
    }

    private void ClearAllVfxClient()
    {
        foreach (var kvp in activeVfx)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        activeVfx.Clear();
    }
}
