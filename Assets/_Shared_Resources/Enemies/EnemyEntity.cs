using UnityEngine;

public class EnemyEntity : NetworkEntity
{
    [SerializeField] private EnemyData data;

    public EnemyData Data => data;
    public EnemyKind EnemyKind => Data != null ? Data.enemyKind : EnemyKind.BlueKnight;
    public int AttackDamage => Data != null ? Data.attackDamage : 20;
    public float AttackRange => Data != null ? Data.attackRange : 1.2f;
    public float AttackCooldown => Data != null ? Data.attackCooldown : 1.25f;
    public bool IsAlive => currentHealth.Value > 0;

    public override void OnNetworkSpawn()
    {
        ApplyDataToBaseStats();
        base.OnNetworkSpawn();
    }

    public void Configure(EnemyData enemyData)
    {
        data = enemyData;
        ApplyDataToBaseStats();

        if (IsServer)
        {
            currentMoveSpeed.Value = baseMoveSpeed;
            currentHealth.Value = baseMaxHealth;
        }
    }

    private void ApplyDataToBaseStats()
    {
        if (data == null) return;

        baseMaxHealth = data.maxHealth;
        baseMoveSpeed = data.moveSpeed;
        baseAttackDamage = data.attackDamage;
        baseAttackRange = data.attackRange;
    }
}
