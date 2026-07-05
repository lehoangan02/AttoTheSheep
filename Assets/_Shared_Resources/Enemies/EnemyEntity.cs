using UnityEngine;

public class EnemyEntity : NetworkEntity
{
    [SerializeField] private EnemyData data;

    private EnemyBrain brain;
    private EnemyMotor motor;
    private EnemyAudio enemyAudio;

    public EnemyData Data => data;
    public EnemyBrain Brain => brain;
    public EnemyMotor Motor => motor;

    public T GetData<T>() where T : EnemyData => data as T;

    void Awake()
    {
        brain = GetComponent<EnemyBrain>();
        motor = GetComponent<EnemyMotor>();
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
        baseMoveSpeed = data.moveSpeed;
    }

    public override void TakeDamage(int damage)
    {
        if (brain != null && brain.ShouldBlockDamage())
        {
            enemyAudio?.Play("Guard");
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
            enemyAudio?.Play("Guard");
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

        enemyAudio.Play(currentHealth.Value <= 0 ? "Death" : "Hurt");
    }

    protected override void Die()
    {
        InvokeOnDied();

        if (brain != null && brain.HandlesOwnDeath)
            return;

        var fx = GetComponent<EnemySpawnDeath>();
        if (fx != null && IsServer)
        {
            fx.PlayDeathSequence();
        }
        else if (fx == null && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}
