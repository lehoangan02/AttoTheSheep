using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    private float nextAttackTime;

    public bool TryAttack(NetworkEntity target, int damage, float range, float cooldown)
    {
        if (target == null || target.currentHealth.Value <= 0) return false;
        if (Time.time < nextAttackTime) return false;

        float distance = Vector2.Distance(transform.position, target.transform.position);
        if (distance > range) return false;

        target.TakeDamage(damage);
        nextAttackTime = Time.time + cooldown;
        return true;
    }
}
