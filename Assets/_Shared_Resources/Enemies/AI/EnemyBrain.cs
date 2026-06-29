using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(EnemyEntity))]
public abstract class EnemyBrain : NetworkBehaviour
{
    [SerializeField] protected float scanRadius = 30f;
    [SerializeField] protected string targetTag = "Player";
    [SerializeField] protected LayerMask targetLayers = ~0;
    [SerializeField] protected ContextSteering2D steering;

    [HideInInspector] public NetworkEntity target;
    [HideInInspector] public float lastAttackTime;
    [HideInInspector] public bool IsFrozen;
    [HideInInspector] public bool IsStunned;

    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    protected float stateTimer;

    protected EnemyEntity entity;
    protected EnemyMotor motor;
    protected StatusEffectController effectController;
    protected EnemyAudio enemyAudio;
    protected Animator anim;
    protected string currentAttackAudioId;

    public EnemyEntity Entity => entity;
    public EnemyMotor Motor => motor;
    public StatusEffectController EffectController => effectController;
    public string CurrentAttackAudioId => currentAttackAudioId;

    void Awake()
    {
        entity = GetComponent<EnemyEntity>();
        motor = GetComponent<EnemyMotor>();
        if (steering == null) steering = GetComponent<ContextSteering2D>();
        effectController = GetComponent<StatusEffectController>();
        enemyAudio = GetComponent<EnemyAudio>();
        if (enemyAudio == null)
            enemyAudio = gameObject.AddComponent<EnemyAudio>();
        anim = GetComponent<Animator>();
    }

    protected void SetState(EnemyState state)
    {
        if (CurrentState == state) return;
        OnStateExit(CurrentState);
        CurrentState = state;
        stateTimer = 0f;
        OnStateEnter(state);
        PlayStateAudio(state);
    }

    protected virtual void OnStateEnter(EnemyState state) { }
    protected virtual void OnStateExit(EnemyState state) { }

    public virtual bool ShouldBlockDamage() => false;

    protected float DistanceTo(NetworkEntity t) => Vector2.Distance(transform.position, t.transform.position);

    protected bool IsCCLocked() => IsFrozen || (effectController != null && effectController.IsMovementLocked());

    protected bool IsAttackReady() => Time.time >= lastAttackTime + (entity?.Data?.attackCooldown ?? 1.25f);

    public virtual void AcquireTarget()
    {
        if (IsValidTarget(target)) return;
        target = FindTarget();
    }

    /// <summary>Context-steered chase move. If ContextSteering2D is attached, uses the
    /// 8-way compass against obstacle/ally layers; otherwise falls back to pure seek
    /// (identical to the old motor.MoveToward(target.position, speed)).</summary>
    public void MoveChaseTarget()
    {
        if (target == null)
        {
            motor.Stop();
            return;
        }
        if (IsCCLocked()) { motor.Stop(); return; }
        float speed = entity.MoveSpeed * (effectController?.GetSpeedMultiplier() ?? 1f);
        if (steering == null)
        {
            motor.MoveToward(target.transform.position, speed);
            return;
        }
        Vector2 toTarget = (Vector2)(target.transform.position - transform.position);
        Vector2 dir = steering.ComputeDirection(toTarget.normalized, toTarget.magnitude);
        if (dir == Vector2.zero)
            motor.Stop();
        else
            motor.MoveWith(dir, speed);
    }

    protected virtual NetworkEntity FindTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, scanRadius, targetLayers);
        NetworkEntity nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var hit in hits)
        {
            NetworkEntity candidate = hit.GetComponentInParent<NetworkEntity>();
            if (!IsValidTarget(candidate)) continue;
            float d = ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < nearestDist) { nearest = candidate; nearestDist = d; }
        }
        return nearest;
    }

    protected virtual bool IsValidTarget(NetworkEntity c)
    {
        if (c == null || c == entity || !c.IsAlive) return false;
        return string.IsNullOrEmpty(targetTag) || c.CompareTag(targetTag);
    }

    public void SetTarget(NetworkEntity t) => target = t;
    public void ClearTarget() => target = null;

    protected abstract void DecideNextState();

    protected void SetAttackAudioId(string attackId)
    {
        currentAttackAudioId = attackId;
    }

    private void PlayStateAudio(EnemyState state)
    {
        if (enemyAudio == null) return;

        switch (state)
        {
            case EnemyState.Attack:
                enemyAudio.PlayAttackStart(currentAttackAudioId);
                break;
            case EnemyState.Hurt:
                enemyAudio.Play(EnemyAudioCueType.Hurt);
                break;
            case EnemyState.Dead:
                enemyAudio.Play(EnemyAudioCueType.Death);
                break;
        }
    }
}
