using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Skills/Smash")]
public class SmashSkill : EnemySkill
{
    public override void Execute(EnemyBrain brain, Vector3 targetPosition)
    {
        StatusEffectController controller = brain.GetComponent<StatusEffectController>();

        Collider2D[] hits = Physics2D.OverlapCircleAll(brain.transform.position, aoeRadius);
        foreach (var hit in hits)
        {
            NetworkEntity target = hit.GetComponentInParent<NetworkEntity>();
            if (target == null || target == brain.Entity || !target.IsAlive) continue;

            target.TakeDamage(damage);

            // Apply on-hit effects (KnockbackEffect etc)
            if (onHitEffects != null && controller != null)
            {
                StatusEffectController targetController = target.GetComponent<StatusEffectController>();
                if (targetController != null)
                {
                    foreach (var effect in onHitEffects)
                        targetController.ApplyEffect(effect, effect.duration, brain.Entity);
                }
            }
        }
    }
}
