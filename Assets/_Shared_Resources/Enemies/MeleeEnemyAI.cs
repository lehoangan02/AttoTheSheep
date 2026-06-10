using UnityEngine;

public class MeleeEnemyAI : EnemyAI
{
    [Header("Melee")]
    [SerializeField] private bool dealDamageOnAttack = true;
    [SerializeField] private EnemyHitbox[] attackHitboxes;

    protected override void ProcessAttack(float distance)
    {
        EnemyData data = Data;
        float attackRange = data != null ? data.attackRange : entity.AttackRange;
        float attackCooldown = data != null ? data.attackCooldown : entity.AttackCooldown;

        if (distance > attackRange)
        {
            currentState = State.Chase;
            return;
        }

        movement?.Stop();

        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;
        SetAnimatorTrigger("Attack");

        if (dealDamageOnAttack && IsValidTarget(target))
        {
            int damage = data != null ? data.attackDamage : entity.AttackDamage;
            target.TakeDamage(damage);
        }
    }

    public void ActivateAttackHitboxes()
    {
        if (attackHitboxes == null) return;

        foreach (EnemyHitbox hitbox in attackHitboxes)
        {
            if (hitbox != null)
            {
                hitbox.Activate();
            }
        }
    }

    public void DeactivateAttackHitboxes()
    {
        if (attackHitboxes == null) return;

        foreach (EnemyHitbox hitbox in attackHitboxes)
        {
            if (hitbox != null)
            {
                hitbox.Deactivate();
            }
        }
    }
}
