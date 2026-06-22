using System.Collections.Generic;
using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] Collider2D hitboxCollider;

    int currentDamage;
    EffectData[] currentEffects;
    bool currentCausesKnockback;
    float currentKnockbackForce;
    float currentKnockbackDuration;
    EnemyBrain brain;
    EnemyAudio enemyAudio;
    HashSet<NetworkEntity> hitTargets = new HashSet<NetworkEntity>();

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

    public void Enable(int damage, EffectData[] onHitEffects = null, bool causesKnockback = false, float knockbackForce = 0f, float knockbackDuration = 0f)
    {
        currentDamage = damage;
        currentEffects = onHitEffects;
        currentCausesKnockback = causesKnockback;
        currentKnockbackForce = knockbackForce;
        currentKnockbackDuration = knockbackDuration;
        hitTargets.Clear();

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
        hitTargets.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (currentDamage == 0 || brain == null) return;
        if (!brain.IsServer) return;

        NetworkEntity target = other.GetComponentInParent<NetworkEntity>();
        if (target == null) return;
        if (target == brain.Entity) return;
        if (!target.IsAlive) return;
        if (hitTargets.Contains(target)) return;

        hitTargets.Add(target);
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
