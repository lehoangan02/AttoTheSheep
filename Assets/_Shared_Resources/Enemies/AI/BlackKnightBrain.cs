using UnityEngine;

public class BlackKnightBrain : EnemyBrain
{
    [SerializeField] float guardChance = 0.3f;
    bool isLeftAttack = true;
    [SerializeField] private EnemyHitbox leftHitbox;
    [SerializeField] private EnemyHitbox rightHitbox;
    private MeleeEnemyData meleeData;

    protected override void Awake()
    {
        base.Awake();
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
        if (dist > meleeData.attackRange) { SetState(EnemyState.Chase); return; }
        if (IsAttackReady()) { SetState(ShouldGuard() ? EnemyState.Guard : EnemyState.Attack); return; }
        SetState(EnemyState.Chase);
    }

    protected override void OnStateEnter(EnemyState state)
    {
        float speedMult = effectController?.GetSpeedMultiplier() ?? 1f;
        switch (state)
        {
            case EnemyState.Chase:
                anim.SetBool("IsChasing", true);
                motor.MoveToward(target.transform.position, entity.Data.moveSpeed * speedMult);
                break;
            case EnemyState.Attack:
                bool useLeftAttack = isLeftAttack;
                enemyAudio.Play(useLeftAttack ? "LeftStart" : "RightStart");
                anim.SetTrigger(useLeftAttack ? "AttackLeft" : "AttackRight");
                isLeftAttack = !isLeftAttack;
                motor.Stop();
                lastAttackTime = Time.time;
                break;
            case EnemyState.Guard:
                anim.SetTrigger("Guard");
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
                anim.SetBool("IsChasing", false);
                break;
            case EnemyState.Attack:
                leftHitbox?.Disable();
                rightHitbox?.Disable();
                break;
        }
    }

    public override bool ShouldBlockDamage() => CurrentState == EnemyState.Guard;

    bool ShouldGuard() => Random.value < guardChance;

    // Animation events
    public void OnLeftAttackHitStart()
    {
        if (leftHitbox == null) { Debug.LogWarning("[BlackKnightBrain] OnLeftAttackHitStart: leftHitbox is not assigned."); return; }
        leftHitbox.Enable(meleeData.attackDamage);
    }
    public void OnLeftAttackHitEnd() => leftHitbox?.Disable();
    public void OnRightAttackHitStart()
    {
        if (rightHitbox == null) { Debug.LogWarning("[BlackKnightBrain] OnRightAttackHitStart: rightHitbox is not assigned."); return; }
        rightHitbox.Enable(meleeData.attackDamage);
    }
    public void OnRightAttackHitEnd() => rightHitbox?.Disable();
    public void OnAttackEnd() => SetState(EnemyState.Chase);
    public void OnGuardEnd() => SetState(EnemyState.Chase);
}
