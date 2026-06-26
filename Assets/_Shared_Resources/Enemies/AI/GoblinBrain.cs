using UnityEngine;

public class GoblinBrain : EnemyBrain
{
    [SerializeField] bool isFastAttack = true;
    [SerializeField] private EnemyHitbox fastHitbox;
    [SerializeField] private EnemyHitbox strongHitbox;

    protected void FixedUpdate()
    {
        if (!IsServer) return;
        if (entity == null || !entity.IsAlive) { if (CurrentState != EnemyState.Dead) SetState(EnemyState.Dead); return; }
        if (CurrentState == EnemyState.Dead) return;
        if (IsCCLocked()) { if (CurrentState != EnemyState.Hurt) SetState(EnemyState.Hurt); return; }
        if (CurrentState == EnemyState.Hurt) { if (!IsCCLocked()) SetState(EnemyState.Idle); return; }
        AcquireTarget();
        if (CurrentState != EnemyState.Attack) DecideNextState();
        if (CurrentState == EnemyState.Chase && target != null)
            motor.MoveToward(target.transform.position, entity.MoveSpeed * (effectController?.GetSpeedMultiplier() ?? 1f));
        stateTimer += Time.fixedDeltaTime;
        if (CurrentState == EnemyState.Attack && stateTimer > 3f)
            SetState(EnemyState.Idle);
    }

    protected override void DecideNextState()
    {
        if (target == null) { SetState(EnemyState.Idle); return; }
        float dist = DistanceTo(target);
        if (dist > entity.Data.attackRange) { SetState(EnemyState.Chase); return; }
        if (IsAttackReady()) { SetState(EnemyState.Attack); return; }
        SetState(EnemyState.Idle);
    }

    protected override void OnStateEnter(EnemyState state)
    {
        float speedMult = effectController?.GetSpeedMultiplier() ?? 1f;
        switch (state)
        {
            case EnemyState.Chase:
                anim.SetBool("IsChasing", true);
                motor.MoveToward(target.transform.position, entity.MoveSpeed * speedMult);
                break;
            case EnemyState.Attack:
                bool useFastAttack = isFastAttack;
                string trigger = useFastAttack ? "AttackFast" : "AttackStrong";
                SetAttackAudioId(useFastAttack ? "Fast" : "Strong");
                anim.SetTrigger(trigger);
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
                anim.SetBool("IsChasing", false);
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
        fastHitbox.Enable(entity.Data.attackDamage);
    }
    public void OnFastAttackHitEnd() => fastHitbox?.Disable();
    public void OnStrongAttackHitStart()
    {
        if (strongHitbox == null) { Debug.LogWarning("GoblinBrain: strongHitbox not assigned.", this); return; }
        strongHitbox.Enable(entity.Data.attackDamage * 2);
    }
    public void OnStrongAttackHitEnd() => strongHitbox?.Disable();
    public void OnAttackEnd() => SetState(EnemyState.Idle);
}
