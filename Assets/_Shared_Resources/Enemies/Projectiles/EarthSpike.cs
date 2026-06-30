using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Stationary ground hazard spawned by the Troll's Smash attack.
/// Rises from the ground, deals damage once to any player or sheep it touches,
/// then sinks back down and despawns. Lifecycle is driven by animation events:
/// OnSpikeRisen() enables the damage collider; OnSpikeFallen() despawns the spike.
/// The spike does NOT chase, does NOT move, and does NOT deal damage more than once per target.
/// </summary>
public class EarthSpike : NetworkBehaviour
{
    [Header("Earth Spike Configuration")]
    [SerializeField] private Collider2D spikeCollider;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private EnemyAudio enemyAudio;
    [SerializeField] private string attackAudioId = "Smash";

    private int damage;
    private EffectData[] onHitEffects;
    private bool causesKnockback;
    private float knockbackForce;
    private float knockbackDuration;
    private NetworkEntity source;
    private Vector2 direction;
    private HashSet<NetworkEntity> hitTargets = new HashSet<NetworkEntity>();

    /// <summary>
    /// Called by the Troll right after the spike is spawned.
    /// Disables the collider and clears hit history; the collider only becomes
    /// active once OnSpikeRisen() fires from the rise animation.
    /// </summary>
    public void Initialize(int dmg, EffectData[] effects, bool kb, float kbForce, float kbDuration, NetworkEntity src, Vector2 dir)
    {
        damage = dmg;
        onHitEffects = effects;
        causesKnockback = kb;
        knockbackForce = kbForce;
        knockbackDuration = kbDuration;
        source = src;
        direction = dir;
        hitTargets.Clear();

        if (spikeCollider != null)
            spikeCollider.enabled = false;
    }

    /// <summary>
    /// Animation Event: called when the spike has fully risen out of the ground.
    /// Server-only; arms the collider and resets the damage-once tracking.
    /// </summary>
    public void OnSpikeRisen()
    {
        if (!IsServer) return;

        hitTargets.Clear();
        if (spikeCollider != null)
            spikeCollider.enabled = true;
    }

    /// <summary>
    /// Animation Event: called when the spike has fully sunk back into the ground.
    /// Server-only; despawns the NetworkObject so all clients see the spike disappear.
    /// </summary>
    public void OnSpikeFallen()
    {
        if (!IsServer) return;

        Despawn();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

        NetworkEntity target = other.GetComponentInParent<NetworkEntity>();
        if (target == null) return;
        if (target == source) return;
        if (!target.IsAlive) return;
        if (hitTargets.Contains(target)) return;

        // Only players and sheep are valid damage targets.
        if (target is not PlayerEntity && target is not LambAI) return;

        hitTargets.Add(target);
        target.TakeDamage(damage, source);
        enemyAudio?.PlayAttackHit(attackAudioId, target.transform.position);

        if (onHitEffects != null)
            foreach (var e in onHitEffects) ApplyEffect(target, e);

        if (causesKnockback)
        {
            // Knockback uses the precomputed direction (e.g. facing direction at spawn),
            // not the delta to the target, so all targets are pushed the same way.
            Vector2 dir = direction;
            target.ApplyKnockback(dir * knockbackForce, knockbackDuration);
        }
    }

    private void ApplyEffect(NetworkEntity target, EffectData effectData)
    {
        if (effectData == null || effectData.effect == null) return;
        var ctrl = target.GetComponent<StatusEffectController>();
        if (ctrl == null) return;
        float duration = effectData.duration > 0 ? effectData.duration : effectData.effect.duration;
        ctrl.ApplyEffect(effectData.effect, duration, source, effectData.damagePerTick);
    }

    private void Despawn()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
}
