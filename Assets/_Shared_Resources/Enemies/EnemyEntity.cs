using UnityEngine;

public class EnemyEntity : NetworkEntity
{
    [SerializeField] private EnemyData data;

    private EnemyBrain brain;
    private EnemyMotor motor;
    private EnemyHitbox hitbox;
    private EnemyAudio enemyAudio;

    public EnemyData Data => data;
    public EnemyKind EnemyKind => Data != null ? Data.enemyKind : EnemyKind.BlueKnight;
    public EnemyBrain Brain => brain;
    public EnemyMotor Motor => motor;
    public EnemyHitbox Hitbox => hitbox;

    public float MoveSpeed => Data != null ? Data.MoveSpeed : 5f;
    public int AttackDamage => Data != null ? Data.attackDamage : 20;
    public float AttackRange => Data != null ? Data.attackRange : 1.2f;
    public float AttackCooldown => Data != null ? Data.attackCooldown : 1.25f;

    void Awake()
    {
        brain = GetComponent<EnemyBrain>();
        motor = GetComponent<EnemyMotor>();
        hitbox = GetComponentInChildren<EnemyHitbox>(true);
        enemyAudio = GetComponent<EnemyAudio>();
        if (enemyAudio == null)
            enemyAudio = gameObject.AddComponent<EnemyAudio>();
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
        baseAttackDamage = data.attackDamage;
        baseAttackRange = data.attackRange;
    }

    public override void TakeDamage(int damage)
    {
        if (brain != null && brain.ShouldBlockDamage())
        {
            enemyAudio?.Play(EnemyAudioCueType.Guard);
            return;
        }

        int previousHealth = currentHealth.Value;
        base.TakeDamage(damage);
        PlayDamageAudio(previousHealth);
    }

    public override void TakeDamage(int damage, NetworkEntity source)
    {
        if (brain != null && brain.ShouldBlockDamage())
        {
            enemyAudio?.Play(EnemyAudioCueType.Guard);
            return;
        }

        int previousHealth = currentHealth.Value;
        base.TakeDamage(damage, source);
        PlayDamageAudio(previousHealth);
    }

    private void PlayDamageAudio(int previousHealth)
    {
        if (!IsServer || enemyAudio == null) return;
        if (previousHealth <= 0 || currentHealth.Value >= previousHealth) return;

        enemyAudio.Play(currentHealth.Value <= 0 ? EnemyAudioCueType.Death : EnemyAudioCueType.Hurt);
    }
}
