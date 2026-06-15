using UnityEngine;

public class MeleeEnemyAI : EnemyAI
{
    [Header("Melee")]
    [SerializeField] private bool dealDamageOnAttack = true;
    [SerializeField] private EnemyHitbox[] attackHitboxes;

    private bool isAttackMovementLocked;

    protected override void ProcessAttack(float distance)
    {
        if (KeepAttackMovementLocked()) return;

        if (distance > AttackRange)
        {
            currentState = State.Chase;
            return;
        }

        StopMoving();

        if (Time.time < lastAttackTime + AttackCooldown) return;
        AttackTarget();
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

    public void LockMovementForAttack()
    {
        isAttackMovementLocked = true;
        StopMoving();
    }

    public void UnlockMovementAfterAttack()
    {
        isAttackMovementLocked = false;
    }

    private bool KeepAttackMovementLocked()
    {
        if (!isAttackMovementLocked) return false;

        StopMoving();
        return true;
    }

    private void AttackTarget()
    {
        lastAttackTime = Time.time;
        LockMovementForAttack();
        SetAnimatorTrigger("Attack");

        if (dealDamageOnAttack && IsValidTarget(target))
        {
            target.TakeDamage(AttackDamage);
        }
    }
}
