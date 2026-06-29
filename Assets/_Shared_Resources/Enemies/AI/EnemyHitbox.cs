using System.Collections.Generic;
using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    [Header("Hitbox Configuration")]
    [SerializeField] private Collider2D hitboxCollider;

    int currentDamage;
    EffectData[] currentEffects;
    bool currentCausesKnockback;
    float currentKnockbackForce;
    float currentKnockbackDuration;
    float tickInterval;
    EnemyBrain brain;
    EnemyAudio enemyAudio;
    HashSet<NetworkEntity> hitTargets = new HashSet<NetworkEntity>();
    Dictionary<Collider2D, float> nextTickTime = new Dictionary<Collider2D, float>();

    [Header("Building Destruction")]
    [SerializeField] private bool canDestroyBuildings = false;
    [SerializeField] private LayerMask buildingLayer = 0;

    void Awake()
    {
        brain = GetComponentInParent<EnemyBrain>();
        enemyAudio = GetComponentInParent<EnemyAudio>();
        if (enemyAudio == null && brain != null)
            enemyAudio = brain.gameObject.AddComponent<EnemyAudio>();
        if (hitboxCollider == null)
            hitboxCollider = GetComponent<Collider2D>();
        Disable();
    }

    public void Enable(int damage, EffectData[] onHitEffects = null, bool causesKnockback = false, float knockbackForce = 0f, float knockbackDuration = 0f, float tickInterval = 0f)
    {
        currentDamage = damage;
        currentEffects = onHitEffects;
        currentCausesKnockback = causesKnockback;
        currentKnockbackForce = knockbackForce;
        currentKnockbackDuration = knockbackDuration;
        this.tickInterval = tickInterval;
        hitTargets.Clear();
        nextTickTime.Clear();

        if (hitboxCollider != null)
            hitboxCollider.enabled = true;
    }

    public void Disable()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;
        currentDamage = 0;
        currentEffects = null;
        currentCausesKnockback = false;
        currentKnockbackForce = 0f;
        currentKnockbackDuration = 0f;
        tickInterval = 0f;
        hitTargets.Clear();
        nextTickTime.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (currentDamage == 0 || brain == null) return;
        if (!brain.IsServer) return;

        // Building destruction check (separate path — buildings are NOT NetworkEntity)
        if (canDestroyBuildings && buildingLayer.value != 0 &&
            ((buildingLayer.value & (1 << other.gameObject.layer)) != 0))
        {
            DestructibleBuilding building = other.GetComponentInParent<DestructibleBuilding>();
            if (building != null)
                building.PlayDestructionSequence();
            return;
        }

        NetworkEntity target = other.GetComponentInParent<NetworkEntity>();
        if (target == null) return;
        if (target == brain.Entity) return;
        if (!target.IsAlive) return;
        if (hitTargets.Contains(target)) return;

        if (target is not PlayerEntity && target is not LambAI) return;

        hitTargets.Add(target);
        if (tickInterval > 0f)
            nextTickTime[other] = Time.time + tickInterval;
        target.TakeDamage(currentDamage, brain.Entity);
        enemyAudio?.PlayAttackHit(brain.CurrentAttackAudioId, target.transform.position);

        if (currentEffects != null)
            foreach (var e in currentEffects) ApplyEffect(target, e);

        if (currentCausesKnockback)
        {
            Vector2 dir = ((Vector2)(target.transform.position - brain.transform.position)).normalized;
            target.ApplyKnockback(dir * currentKnockbackForce, currentKnockbackDuration);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (currentDamage == 0 || brain == null) return;
        if (!brain.IsServer) return;

        // Building destruction check (separate path — buildings are NOT NetworkEntity)
        if (canDestroyBuildings && buildingLayer.value != 0 &&
            ((buildingLayer.value & (1 << other.gameObject.layer)) != 0))
        {
            DestructibleBuilding building = other.GetComponentInParent<DestructibleBuilding>();
            if (building != null)
                building.PlayDestructionSequence();
            return;
        }

        if (tickInterval <= 0f) return;

        NetworkEntity target = other.GetComponentInParent<NetworkEntity>();
        if (target == null) return;
        if (target == brain.Entity) return;
        if (!target.IsAlive) return;
        if (target is not PlayerEntity && target is not LambAI) return;

        if (Time.time < nextTickTime.GetValueOrDefault(other)) return;

        nextTickTime[other] = Time.time + tickInterval;
        target.TakeDamage(currentDamage, brain.Entity);
        enemyAudio?.PlayAttackHit(brain.CurrentAttackAudioId, target.transform.position);

        if (currentEffects != null)
            foreach (var e in currentEffects) ApplyEffect(target, e);

        if (currentCausesKnockback)
        {
            Vector2 dir = ((Vector2)(target.transform.position - brain.transform.position)).normalized;
            target.ApplyKnockback(dir * currentKnockbackForce, currentKnockbackDuration);
        }
    }

    void ApplyEffect(NetworkEntity target, EffectData effectData)
    {
        if (effectData == null || effectData.effect == null) return;
        var ctrl = target.GetComponent<StatusEffectController>();
        if (ctrl == null) return;
        float duration = effectData.duration > 0 ? effectData.duration : effectData.effect.duration;
        ctrl.ApplyEffect(effectData.effect, duration, brain.Entity);
    }
}
