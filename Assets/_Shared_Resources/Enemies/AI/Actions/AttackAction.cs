using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/AI/Actions/Attack")]
public class AttackAction : EnemyAction
{
    [Tooltip("The attack configuration to use. If null, falls back to EnemyData.attacks[0]")]
    public AttackData attackData;

    [Tooltip("Multiplier applied to the attack's base damage (e.g., 1.3 for strong attacks)")]
    public float damageMultiplier = 1f;

    public override void Execute(EnemyBrain brain)
    {
        if (brain.target == null) return;
        brain.Animator?.SetChasing(false);
        brain.Motor?.Stop();

        // Determine which attack data to use
        AttackData data = attackData;
        if (data == null)
        {
            if (brain.Entity?.Data?.attacks != null && brain.Entity.Data.attacks.Length > 0)
                data = brain.Entity.Data.attacks[0];
        }

        if (data == null) return; // No attack configuration available

        // Cooldown check
        float cooldown = data.cooldown;
        if (Time.time < brain.lastAttackTime + cooldown) return;

        // Range check against primary target
        float range = data.range;
        float dist = Vector2.Distance(brain.transform.position, brain.target.transform.position);
        if (dist > range) return;

        brain.lastAttackTime = Time.time;
        brain.Animator?.PlayAttack(data.animationTrigger);

        int baseDamage = Mathf.RoundToInt(data.damage * damageMultiplier);

        if (data.isAreaAttack)
        {
            // Area attack: hit all valid targets within areaRadius
            float radius = data.areaRadius > 0 ? data.areaRadius : range;
            Collider2D[] hits = Physics2D.OverlapCircleAll(brain.transform.position, radius, data.targetLayers);

            int hitCount = 0;
            foreach (var hit in hits)
            {
                NetworkEntity target = hit.GetComponentInParent<NetworkEntity>();
                if (target == null) continue;
                if (target == brain.Entity) continue;
                if (!target.IsAlive) continue;
                if (data.maxTargets >= 0 && hitCount >= data.maxTargets) break;

                // Damage falloff based on distance from attack center
                float targetDist = Vector2.Distance(brain.transform.position, target.transform.position);
                float t = radius > 0 ? Mathf.Clamp01(targetDist / radius) : 0f;
                float falloff = data.damageFalloff.Evaluate(t);
                int finalDamage = Mathf.RoundToInt(baseDamage * falloff);

                target.TakeDamage(finalDamage, brain.Entity);

                // Apply on-hit effects (enemy-level passive + attack-specific)
                ApplyEffects(target, data, brain.Entity);

                // Apply knockback
                if (data.causesKnockback)
                {
                    Vector2 dir = (target.transform.position - brain.transform.position).normalized;
                    Vector2 force = dir * data.knockbackForce;
                    target.ApplyKnockback(force, data.knockbackDuration);
                }

                hitCount++;
            }
        }
        else
        {
            // Single target attack
            brain.target.TakeDamage(baseDamage, brain.Entity);

            // Apply on-hit effects
            ApplyEffects(brain.target, data, brain.Entity);

            // Apply knockback
            if (data.causesKnockback)
            {
                Vector2 dir = (brain.target.transform.position - brain.transform.position).normalized;
                Vector2 force = dir * data.knockbackForce;
                brain.target.ApplyKnockback(force, data.knockbackDuration);
            }
        }
    }

    void ApplyEffects(NetworkEntity target, AttackData data, NetworkEntity source)
    {
        // Apply enemy-level passive effects (e.g., PoisonSnake, PoisonSpider)
        if (source is EnemyEntity enemy && enemy.Data?.onHitEffects != null)
        {
            foreach (var effectData in enemy.Data.onHitEffects)
            {
                ApplyEffect(target, effectData, source);
            }
        }

        // Apply attack-specific effects
        if (data?.onHitEffects != null)
        {
            foreach (var effectData in data.onHitEffects)
            {
                ApplyEffect(target, effectData, source);
            }
        }
    }

    void ApplyEffect(NetworkEntity target, EffectData effectData, NetworkEntity source)
    {
        if (effectData == null || effectData.effect == null) return;

        StatusEffectController targetEffectCtrl = target.GetComponent<StatusEffectController>();
        if (targetEffectCtrl == null) return;

        float duration = effectData.duration > 0 ? effectData.duration : effectData.effect.duration;
        targetEffectCtrl.ApplyEffect(effectData.effect, duration, source);
    }
}
