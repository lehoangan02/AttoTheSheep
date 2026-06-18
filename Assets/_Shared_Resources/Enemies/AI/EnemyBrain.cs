using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyEntity))]
public class EnemyBrain : NetworkBehaviour
{
    [SerializeField] EnemyBehavior behavior;
    [SerializeField] float scanRadius = 30f;
    [SerializeField] string targetTag = "Player";
    [SerializeField] LayerMask targetLayers = ~0;

    [HideInInspector] public NetworkEntity target;
    [HideInInspector] public float lastAttackTime;
    [HideInInspector] public bool IsFrozen;
    [HideInInspector] public bool IsStunned;

    EnemyEntity entity;
    EnemyMotor motor;
    EnemyAnimator animator;
    StatusEffectController effectController;
    Dictionary<string, float> cooldownTimers = new Dictionary<string, float>();

    // Public accessors for actions/conditions:
    public EnemyEntity Entity => entity;
    public EnemyMotor Motor => motor;
    public EnemyAnimator Animator => animator;
    public StatusEffectController EffectController => effectController;
    public EnemyBehavior Behavior { get => behavior; set => behavior = value; }
    public float ScanRadius => scanRadius;
    public string TargetTag => targetTag;
    public float DeltaTime => Time.fixedDeltaTime;

    void Awake()
    {
        entity = GetComponent<EnemyEntity>();
        motor = GetComponent<EnemyMotor>();
        animator = GetComponent<EnemyAnimator>();
        effectController = GetComponent<StatusEffectController>();
    }

    void FixedUpdate()
    {
        if (!IsServer) return;
        if (behavior == null) return;
        if (entity == null || !entity.IsAlive) return;

        // Check CC
        bool locked = effectController != null && effectController.IsMovementLocked();
        if (locked) { motor?.Stop(); return; }

        AcquireTarget();

        // Evaluate behavior nodes top-to-bottom
        foreach (BehaviorNode node in behavior.nodes)
        {
            bool conditionMet = node.condition == null || node.condition.Evaluate(this);
            if (conditionMet)
            {
                node.action?.Execute(this);
                break; // only run first matching node per frame
            }
        }
    }

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

    // Cooldown helpers (for skills like Wizard fireball)
    public float GetCooldownTimer(string key) => cooldownTimers.TryGetValue(key, out float v) ? v : 0f;
    public void SetCooldownTimer(string key, float value) => cooldownTimers[key] = value;
    public void IncrementCooldowns(float dt)
    {
        var keys = new List<string>(cooldownTimers.Keys);
        foreach (var k in keys) cooldownTimers[k] += dt;
        // Cleanup very large timers
        foreach (var k in keys) if (cooldownTimers[k] > 1000f) cooldownTimers.Remove(k);
    }
}
