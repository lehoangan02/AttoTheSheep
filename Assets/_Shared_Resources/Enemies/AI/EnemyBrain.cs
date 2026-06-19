using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(EnemyEntity))]
public abstract class EnemyBrain : NetworkBehaviour
{
    [SerializeField] float scanRadius = 30f;
    [SerializeField] string targetTag = "Player";
    [SerializeField] LayerMask targetLayers = ~0;

    [HideInInspector] public NetworkEntity target;
    [HideInInspector] public float lastAttackTime;
    [HideInInspector] public bool IsFrozen;
    [HideInInspector] public bool IsStunned;

    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    protected float stateTimer;

    protected EnemyEntity entity;
    protected EnemyMotor motor;
    protected StatusEffectController effectController;
    protected EnemyHitbox hitbox;
    protected Animator anim;

    public EnemyEntity Entity => entity;
    public EnemyMotor Motor => motor;
    public StatusEffectController EffectController => effectController;
    protected EnemyHitbox Hitbox => hitbox;

    void Awake()
    {
        entity = GetComponent<EnemyEntity>();
        motor = GetComponent<EnemyMotor>();
        effectController = GetComponent<StatusEffectController>();
        hitbox = GetComponentInChildren<EnemyHitbox>(true);
        anim = GetComponent<Animator>();
    }

    protected void SetState(EnemyState state)
    {
        if (CurrentState == state) return;
        OnStateExit(CurrentState);
        CurrentState = state;
        stateTimer = 0f;
        OnStateEnter(state);
    }

    protected virtual void OnStateEnter(EnemyState state) { }
    protected virtual void OnStateExit(EnemyState state) { }

    public virtual bool ShouldBlockDamage() => false;

    protected float DistanceTo(NetworkEntity t) => Vector2.Distance(transform.position, t.transform.position);

    protected bool IsCCLocked() => IsFrozen || (effectController != null && effectController.IsMovementLocked());

    protected bool IsAttackReady() => Time.time >= lastAttackTime + (entity?.Data?.attackCooldown ?? 1.25f);

    public void AcquireTarget()
    {
        if (IsValidTarget(target)) return;
        target = FindTarget();
    }

    NetworkEntity FindTarget()
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

    bool IsValidTarget(NetworkEntity c)
    {
        if (c == null || c == entity || !c.IsAlive) return false;
        return string.IsNullOrEmpty(targetTag) || c.CompareTag(targetTag);
    }

    public void SetTarget(NetworkEntity t) => target = t;
    public void ClearTarget() => target = null;

    protected abstract void DecideNextState();
}
