using UnityEngine;

public class BlueKnightBrain : EnemyBrain
{
    [SerializeField] private EnemyHitbox hitbox;
    private KnightEnemyData knightData;

    protected override void Init()
    {
        knightData = entity.GetData<KnightEnemyData>();
    }

    protected void FixedUpdate()
    {
        if (!IsServer) return;
        if (entity == null || !entity.IsAlive) { if (CurrentState != EnemyState.Dead) SetState(EnemyState.Dead); return; }
        if (CurrentState == EnemyState.Dead) return;
        if (IsCCLocked()) { if (CurrentState != EnemyState.Hurt) SetState(EnemyState.Hurt); return; }
        if (CurrentState == EnemyState.Hurt) { if (!IsCCLocked()) SetState(EnemyState.Idle); return; }
        AcquireTarget();
        if (CurrentState != EnemyState.Attack && CurrentState != EnemyState.Guard) DecideNextState();
        if (CurrentState == EnemyState.Chase)
            MoveChaseTarget();
        stateTimer += Time.fixedDeltaTime;
        if (CurrentState == EnemyState.Attack && stateTimer > 3f)
            SetState(EnemyState.Idle);
        if (CurrentState == EnemyState.Guard)
        {
            motor.Stop();
            if (stateTimer > 0.5f) SetState(EnemyState.Idle);
        }
    }

    protected override void DecideNextState()
    {
        if (target == null) { SetState(EnemyState.Idle); return; }
        float dist = DistanceTo(target);
        if (dist > knightData.attackRange) { SetState(EnemyState.Chase); return; }
        if (IsAttackReady()) { SetState(ShouldGuard() ? EnemyState.Guard : EnemyState.Attack); return; }
        SetState(EnemyState.Chase);
    }

    protected override void OnStateEnter(EnemyState state)
    {
        float speedMult = effectController?.GetSpeedMultiplier() ?? 1f;
        switch (state)
        {
            case EnemyState.Chase:
                SyncSetBool("IsChasing", true);
                motor.MoveToward(target.transform.position, entity.Data.moveSpeed * speedMult);
                break;
            case EnemyState.Attack:
                SyncSetTrigger("Attack");
                motor.Stop();
                lastAttackTime = Time.time;
                enemyAudio.Play("AttackStart");
                break;
            case EnemyState.Guard:
                SyncSetTrigger("Guard");
                motor.Stop();
                lastAttackTime = Time.time;
                break;
            case EnemyState.Idle:
                motor.Stop();
                break;
        }
    }

    protected override void OnStateExit(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Chase:
                motor.Stop();
                SyncSetBool("IsChasing", false);
                break;
            case EnemyState.Attack:
                hitbox?.Disable();
                break;
        }
    }

    public override bool ShouldBlockDamage() => CurrentState == EnemyState.Guard;

    bool ShouldGuard() => Random.value < knightData.guardChance;

    // Animation events
    public void OnAttackHitStart()
    {
        if (hitbox == null) { Debug.LogWarning($"[{GetType().Name}] hitbox not wired on {gameObject.name}"); return; }
        hitbox?.Enable(knightData.attackDamage);
    }
    public void OnAttackHitEnd() => hitbox?.Disable();
    public void OnAttackEnd() => DecideNextState();
    public void OnGuardEnd() => DecideNextState();
}
