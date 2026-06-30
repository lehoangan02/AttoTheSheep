using UnityEngine;

public class SnakeBrain : EnemyBrain
{
    [SerializeField] private EnemyHitbox hitbox;
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
                anim.SetBool("IsChasing", true);
                motor.MoveToward(target.transform.position, entity.Data.moveSpeed * speedMult);
                break;
            case EnemyState.Attack:
                anim.SetTrigger("Attack");
                motor.Stop();
                lastAttackTime = Time.time;
                enemyAudio.Play("AttackStart");
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
                hitbox?.Disable();
                break;
        }
    }

    // Animation events
    public void OnAttackHitStart()
    {
        if (hitbox == null) { Debug.LogWarning($"[{GetType().Name}] hitbox not wired on {gameObject.name}"); return; }
        hitbox?.Enable(meleeData.attackDamage);
    }
    public void OnAttackHitEnd() => hitbox?.Disable();
    public void OnAttackEnd() => SetState(EnemyState.Chase);
}
