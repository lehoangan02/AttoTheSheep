using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(EnemyEntity))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyTargeting))]
[RequireComponent(typeof(EnemyAttack))]
public class EnemyBrain : NetworkBehaviour
{
    private EnemyEntity entity;
    private EnemyMovement movement;
    private EnemyTargeting targeting;
    private EnemyAttack attack;

    private void Awake()
    {
        entity = GetComponent<EnemyEntity>();
        movement = GetComponent<EnemyMovement>();
        targeting = GetComponent<EnemyTargeting>();
        attack = GetComponent<EnemyAttack>();
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (entity == null || !entity.IsAlive)
        {
            movement?.Stop();
            return;
        }

        NetworkEntity target = targeting.FindNearestTarget(transform.position);
        if (target == null)
        {
            movement.Stop();
            return;
        }

        float distance = Vector2.Distance(transform.position, target.transform.position);
        if (distance <= entity.AttackRange)
        {
            movement.Stop();
            attack.TryAttack(target, entity.AttackDamage, entity.AttackRange, entity.AttackCooldown);
            return;
        }

        movement.MoveToward(target.transform.position, entity.currentMoveSpeed.Value);
    }
}
