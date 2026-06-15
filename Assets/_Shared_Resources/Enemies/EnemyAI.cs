using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(EnemyEntity))]
[RequireComponent(typeof(EnemyMovement))]
public abstract class EnemyAI : NetworkBehaviour
{
    public enum State
    {
        Idle,
        Chase,
        Attack,
        Casting
    }

    [Header("AI State")]
    [SerializeField] protected State currentState = State.Idle;
    [SerializeField] private bool requireServerAuthority;

    [Header("Targeting")]
    [SerializeField] private float scanRadius = 30f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private string targetTag = "Player";

    protected EnemyEntity entity;
    protected EnemyMovement movement;
    protected Animator animator;
    protected NetworkEntity target;
    protected float lastAttackTime;

    public State CurrentState => currentState;
    public NetworkEntity Target => target;
    protected EnemyData Data => entity != null ? entity.Data : null;
    protected bool CanRunAI => !requireServerAuthority || !IsSpawned || IsServer;
    protected float AttackRange => entity != null ? entity.AttackRange : 0f;
    protected float AttackCooldown => entity != null ? entity.AttackCooldown : 0f;
    protected int AttackDamage => entity != null ? entity.AttackDamage : 0;
    protected float MoveSpeed
    {
        get
        {
            if (entity == null) return 0f;
            return IsSpawned && IsServer ? entity.currentMoveSpeed.Value : entity.BaseMoveSpeed;
        }
    }

    protected virtual void Awake()
    {
        entity = GetComponent<EnemyEntity>();
        movement = GetComponent<EnemyMovement>();
        animator = GetComponent<Animator>();
        currentState = State.Idle;
    }

    protected virtual void FixedUpdate()
    {
        HandleStateMachine();
        UpdateAnimations();
    }

    public void SetTarget(NetworkEntity newTarget)
    {
        target = newTarget;
    }

    public void ClearTarget()
    {
        target = null;
    }

    protected virtual void AcquireTarget()
    {
        if (IsValidTarget(target)) return;
        target = FindTarget();
    }

    protected virtual NetworkEntity FindTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, scanRadius, targetLayers);
        NetworkEntity nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            NetworkEntity candidate = hit.GetComponentInParent<NetworkEntity>();
            if (!IsValidTarget(candidate)) continue;

            float distanceSqr = ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distanceSqr >= nearestDistanceSqr) continue;

            nearest = candidate;
            nearestDistanceSqr = distanceSqr;
        }

        return nearest;
    }

    protected virtual void HandleStateMachine()
    {
        switch (currentState)
        {
            case State.Idle:
                ProcessIdle();
                break;
            case State.Chase:
                if (!TryGetTargetDistance(out float chaseDistance))
                {
                    ReturnToIdle();
                    break;
                }

                ProcessChase(chaseDistance);
                break;
            case State.Attack:
                if (!TryGetTargetDistance(out float attackDistance))
                {
                    ReturnToIdle();
                    break;
                }

                ProcessAttack(attackDistance);
                break;
            case State.Casting:
                ProcessCasting();
                break;
        }
    }

    protected virtual void ProcessIdle()
    {
        StopMoving();
        AcquireTarget();

        if (target != null)
        {
            currentState = State.Chase;
        }
    }

    protected virtual void ProcessChase(float distance)
    {
        if (distance <= AttackRange)
        {
            currentState = State.Attack;
            StopMoving();
            return;
        }

        MoveTowardsTarget();
    }

    protected virtual void MoveTowardsTarget()
    {
        if (target == null || movement == null) return;

        movement.MoveToward(target.transform.position, MoveSpeed);
    }

    protected virtual void ReturnToIdle()
    {
        target = null;
        currentState = State.Idle;
        StopMoving();
    }

    protected bool TryGetTargetDistance(out float distance)
    {
        distance = 0f;

        if (!IsValidTarget(target)) return false;

        distance = Vector2.Distance(transform.position, target.transform.position);
        return true;
    }

    protected abstract void ProcessAttack(float distance);

    protected virtual void ProcessCasting()
    {
        StopMoving();
    }

    protected virtual void UpdateAnimations()
    {
        SetAnimatorBool("IsChasing", currentState == State.Chase);
    }

    protected void SetAnimatorTrigger(string parameterName)
    {
        if (animator != null && HasAnimatorParameter(parameterName))
        {
            animator.SetTrigger(parameterName);
        }
    }

    protected void SetAnimatorBool(string parameterName, bool value)
    {
        if (animator != null && HasAnimatorParameter(parameterName))
        {
            animator.SetBool(parameterName, value);
        }
    }

    protected bool HasAnimatorParameter(string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName)) return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName) return true;
        }

        return false;
    }

    protected void StopMoving()
    {
        movement?.Stop();
    }

    protected virtual bool IsValidTarget(NetworkEntity candidate)
    {
        if (candidate == null) return false;
        if (candidate == entity) return false;
        return string.IsNullOrEmpty(targetTag) || candidate.CompareTag(targetTag);
    }
}
