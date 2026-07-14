using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Area effect that applies a slow status effect to entities inside it.
/// Uses the unified StatusEffectController system. Server-only: trigger
/// handlers guard with IsServer so effects are not applied on clients.
/// </summary>
public class SlowPuddle : NetworkBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Layers that will be slowed by this puddle.")]
    [SerializeField] private LayerMask affectedLayers;

    [Tooltip("Tags that will be slowed. Leave empty to use only the Layer filter.")]
    [SerializeField] private List<string> affectedTags = new List<string>();

    private StatusEffectData slowEffect;
    private NetworkEntity source;
    private float duration;

    private List<NetworkEntity> entitiesInside = new List<NetworkEntity>();

    public void Initialize(StatusEffectData effect, NetworkEntity src, float time)
    {
        slowEffect = effect;
        source = src;
        duration = time;

        if (IsServer)
        {
            Invoke(nameof(DespawnPuddle), duration);
        }
    }

    private bool IsValidTarget(GameObject target)
    {
        if ((affectedLayers.value & (1 << target.layer)) != 0)
            return true;

        if (affectedTags != null && affectedTags.Count > 0)
        {
            if (affectedTags.Contains(target.tag))
                return true;
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;
        if (!IsValidTarget(other.gameObject)) return;

        NetworkEntity target = other.GetComponentInParent<NetworkEntity>();
        if (target == null) return;

        StatusEffectController ctrl = target.GetComponent<StatusEffectController>();
        if (ctrl == null) return;

        if (!entitiesInside.Contains(target))
        {
            entitiesInside.Add(target);
            // 0 duration = indefinite; removed when entity leaves or puddle despawns.
            ctrl.ApplyEffect(slowEffect, 0f, source);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsServer) return;
        if (!IsValidTarget(other.gameObject)) return;

        NetworkEntity target = other.GetComponentInParent<NetworkEntity>();
        if (target == null) return;

        if (entitiesInside.Contains(target))
        {
            entitiesInside.Remove(target);
            StatusEffectController ctrl = target.GetComponent<StatusEffectController>();
            if (ctrl != null) ctrl.RemoveEffect(EffectKind.Slow);
        }
    }

    public override void OnNetworkDespawn()
    {
        // Remove slow from any entities still inside when the puddle disappears.
        if (IsServer)
        {
            foreach (var entity in entitiesInside)
            {
                if (entity == null) continue;
                StatusEffectController ctrl = entity.GetComponent<StatusEffectController>();
                if (ctrl != null) ctrl.RemoveEffect(EffectKind.Slow);
            }
        }
        entitiesInside.Clear();
        base.OnNetworkDespawn();
    }

    private void DespawnPuddle()
    {
        if (IsServer && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}
