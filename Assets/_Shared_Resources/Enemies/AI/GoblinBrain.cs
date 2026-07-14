using UnityEngine;

public class GoblinBrain : EnemyBrain
{
    [SerializeField] bool isFastAttack = true;
    [SerializeField] private EnemyHitbox fastHitbox;
    [SerializeField] private EnemyHitbox strongHitbox;
    private MeleeEnemyData meleeData;

    protected override void Init()
    {
        meleeData = entity.GetData<MeleeEnemyData>();
    }

    protected void FixedUpdate()
    {
        if (!IsServer) return;
        if (entity == null || !entity.IsAlive) { if (CurrentState != EnemyState.Dead) SetState(EnemyState.Dead); return; }
        if (CurrentState == EnemyState.Dead) return;
        if (IsCCLocked()) { if (CurrentState != EnemyState.Hurt) SetState(EnemyState.Hurt); return; }
        if (CurrentState == EnemyState.Hurt) { if (!IsCCLocked()) SetState(EnemyState.Idle); return; }
        AcquireTarget();
        if (CurrentState != EnemyState.Attack) DecideNextState();
        if (CurrentState == EnemyState.Chase)
            MoveChaseTarget();
        stateTimer += Time.fixedDeltaTime;
        if (CurrentState == EnemyState.Attack && stateTimer > 3f)
            SetState(EnemyState.Idle);
    }

    protected override void DecideNextState()
    {
        if (target == null) { SetState(EnemyState.Idle); return; }
        float dist = DistanceTo(target);
        if (dist > meleeData.attackRange) { SetState(EnemyState.Chase); return; }
        if (IsAttackReady()) { SetState(EnemyState.Attack); return; }
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
                bool useFastAttack = isFastAttack;
                string trigger = useFastAttack ? "AttackFast" : "AttackStrong";
                enemyAudio.Play(useFastAttack ? "FastStart" : "StrongStart");
                SyncSetTrigger(trigger);
                isFastAttack = !isFastAttack;
                lastAttackTime = Time.time;
                motor.Stop();
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
                fastHitbox?.Disable();
                strongHitbox?.Disable();
                break;
        }
    }

    // Animation events
    public void OnFastAttackHitStart()
    {
        if (fastHitbox == null) { Debug.LogWarning("GoblinBrain: fastHitbox not assigned.", this); return; }
        fastHitbox.Enable(meleeData.attackDamage);
    }
    public void OnFastAttackHitEnd() => fastHitbox?.Disable();
    public void OnStrongAttackHitStart()
    {
        if (strongHitbox == null) { Debug.LogWarning("GoblinBrain: strongHitbox not assigned.", this); return; }
        strongHitbox.Enable(meleeData.attackDamage * 2);
    }
    public void OnStrongAttackHitEnd() => strongHitbox?.Disable();
    public void OnAttackEnd() => DecideNextState();
}
