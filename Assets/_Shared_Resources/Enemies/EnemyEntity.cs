using UnityEngine;

public class EnemyEntity : NetworkEntity
{
    [SerializeField] private EnemyData data;

    private EnemyBrain brain;
    private EnemyMotor motor;
    private EnemyAnimator animator;

    public EnemyData Data => data;
    public EnemyKind EnemyKind => Data != null ? Data.enemyKind : EnemyKind.BlueKnight;
    public EnemyBrain Brain => brain;
    public EnemyMotor Motor => motor;
    public EnemyAnimator Animator => animator;

    public float MoveSpeed => Data != null ? Data.MoveSpeed : 5f;
    public int AttackDamage => Data != null ? Data.AttackDamage : 20;
    public float AttackRange => Data != null ? Data.AttackRange : 1.2f;
    public float AttackCooldown => Data != null ? Data.AttackCooldown : 1.25f;
    public bool IsAlive => currentHealth.Value > 0;

    void Awake()
    {
        brain = GetComponent<EnemyBrain>();
        motor = GetComponent<EnemyMotor>();
        animator = GetComponent<EnemyAnimator>();
    }

    public override void OnNetworkSpawn()
    {
        ApplyDataToBaseStats();
        base.OnNetworkSpawn();
    }

    public void Configure(EnemyData enemyData)
    {
        data = enemyData;
        ApplyDataToBaseStats();

        if (brain != null && enemyData != null && enemyData.behavior != null)
        {
            brain.Behavior = enemyData.behavior;
        }

        if (IsServer)
        {
            currentMoveSpeed.Value = baseMoveSpeed;
            currentHealth.Value = baseMaxHealth;
        }
    }

    void ApplyDataToBaseStats()
    {
        if (data == null) return;
        baseMaxHealth = data.maxHealth;
        baseMoveSpeed = data.MoveSpeed;
        if (data.attacks != null && data.attacks.Length > 0)
        {
            baseAttackDamage = data.attacks[0].damage;
            baseAttackRange = data.attacks[0].range;
        }
    }
}
