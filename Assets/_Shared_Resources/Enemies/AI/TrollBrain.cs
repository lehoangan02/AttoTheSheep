using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class TrollBrain : EnemyBrain
{
    public enum TrollAttack { Smash, Charge, Tornado }

    // Attack Selection
    [Header("Troll - Attack Selection")]
    [SerializeField] private float smashRange = 2f;
    [SerializeField] private float chargeRange = 8f;
    [SerializeField] private float smashCooldown = 3f;
    [SerializeField] private float chargeCooldown = 5f;
    [SerializeField] private float tornadoCooldown = 9f;
    [SerializeField] private float aiDecisionInterval = 0.6f;
    private float lastDecisionTime;
    private float lastSmashTime;
    private float lastChargeTime;
    private float lastTornadoTime;

    // Smash
    [Header("Troll - Smash")]
    [SerializeField] private int smashDamage = 40;
    [SerializeField] private EffectData[] smashEffects;
    [SerializeField] private float smashKnockbackForce = 8f;
    [SerializeField] private float smashKnockbackDuration = 0.25f;
    [SerializeField] private float smashWindupTime = 0.5f;
    [SerializeField] private float smashMaxAttackTime = 2f;
    [SerializeField] private EnemyHitbox smashHitbox;

    // Charge
    [Header("Troll - Charge")]
    [SerializeField] private float chargeSpeed = 12f;
    [SerializeField] private float chargeMaxDistance = 10f;
    [SerializeField] private int chargeDamage = 35;
    [SerializeField] private EffectData[] chargeEffects;
    [SerializeField] private float chargeKnockbackForce = 10f;
    [SerializeField] private float chargeKnockbackDuration = 0.2f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float chargeWindupTime = 0.5f;
    [SerializeField] private float chargeMaxAttackTime = 3f;
    [SerializeField] private EnemyHitbox chargeHitbox;

    // Tornado
    [Header("Troll - Tornado")]
    [SerializeField] private float tornadoSpeed = 6f;
    [SerializeField] private float tornadoDuration = 4f;
    [SerializeField] private float tornadoTickInterval = 0.5f;
    [SerializeField] private int tornadoDamagePerTick = 8;
    [SerializeField] private float tornadoWindupTime = 0.5f;
    [SerializeField] private float tornadoMaxAttackTime = 8f;
    [SerializeField] private EnemyHitbox tornadoHitbox;
    [SerializeField] private GameObject tornadoVfxPrefab;

    // Runtime state
    private TrollAttack currentAttack;
    private bool windupComplete;
    private Vector2 dashDir;
    private float dashDistanceLeft;
    private float tornadoTimer;
    private GameObject tornadoVfxInstance;

    protected void FixedUpdate()
    {
        if (!IsServer) return;
        if (entity == null || !entity.IsAlive) { if (CurrentState != EnemyState.Dead) SetState(EnemyState.Dead); return; }
        if (CurrentState == EnemyState.Dead) return;
        if (IsCCLocked()) { if (CurrentState != EnemyState.Hurt) SetState(EnemyState.Hurt); return; }
        if (CurrentState == EnemyState.Hurt) { if (!IsCCLocked()) SetState(EnemyState.Idle); return; }

        AcquireTarget();
        if (CurrentState != EnemyState.Attack)
            DecideNextState();

        if (CurrentState == EnemyState.Chase && target != null)
            motor.MoveToward(target.transform.position, entity.MoveSpeed * (effectController?.GetSpeedMultiplier() ?? 1f));

        if (CurrentState == EnemyState.Attack && !windupComplete && stateTimer >= GetWindupTime())
            CompleteWindup();

        if (CurrentState == EnemyState.Attack && currentAttack == TrollAttack.Tornado && target != null)
        {
            motor.MoveToward(target.transform.position, tornadoSpeed);
            tornadoTimer += Time.fixedDeltaTime;
            if (tornadoTimer >= tornadoDuration)
                EndTornado();
        }

        if (CurrentState == EnemyState.Attack && currentAttack == TrollAttack.Charge)
        {
            float step = chargeSpeed * Time.fixedDeltaTime;
            if (dashDistanceLeft <= 0) { EndCharge(); return; }
            var hit = Physics2D.Raycast(transform.position, dashDir, step + 0.1f, obstacleMask);
            if (hit.collider != null) { EndCharge(); return; }
            motor.MoveToward((Vector2)transform.position + dashDir * step, chargeSpeed);
            dashDistanceLeft -= step;
        }

        stateTimer += Time.fixedDeltaTime;

        if (CurrentState == EnemyState.Attack && stateTimer > CurrentAttackMaxTime())
            ForceEndAttack();
        if (CurrentState == EnemyState.Cast && stateTimer > 5f)
            SetState(EnemyState.Idle);
    }

    protected override void DecideNextState()
    {
        if (target == null) { SetState(EnemyState.Idle); return; }
        if (Time.time - lastDecisionTime < aiDecisionInterval) return;
        lastDecisionTime = Time.time;

        float dist = DistanceTo(target);

        if (dist <= smashRange && Time.time - lastSmashTime >= smashCooldown)
        { currentAttack = TrollAttack.Smash; SetState(EnemyState.Attack); return; }

        if (dist <= chargeRange && Time.time - lastChargeTime >= chargeCooldown)
        { currentAttack = TrollAttack.Charge; SetState(EnemyState.Attack); return; }

        if (dist > chargeRange && Time.time - lastTornadoTime >= tornadoCooldown)
        { currentAttack = TrollAttack.Tornado; SetState(EnemyState.Attack); return; }

        SetState(EnemyState.Chase);
    }

    protected override void OnStateEnter(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Chase:
                anim.SetBool("IsChasing", true);
                if (target != null)
                    motor.MoveToward(target.transform.position, entity.MoveSpeed * (effectController?.GetSpeedMultiplier() ?? 1f));
                break;
            case EnemyState.Attack:
                windupComplete = false;
                switch (currentAttack)
                {
                    case TrollAttack.Smash: lastSmashTime = Time.time; break;
                    case TrollAttack.Charge: lastChargeTime = Time.time; break;
                    case TrollAttack.Tornado: lastTornadoTime = Time.time; break;
                }
                anim.SetTrigger("Windup" + currentAttack);
                if (currentAttack != TrollAttack.Tornado)
                    motor.Stop();
                SetAttackAudioId(currentAttack.ToString());
                break;
            case EnemyState.Idle:
                motor.Stop();
                smashHitbox?.Disable();
                chargeHitbox?.Disable();
                CleanupTornado();
                break;
            case EnemyState.Hurt:
                motor.Stop();
                smashHitbox?.Disable();
                chargeHitbox?.Disable();
                CleanupTornado();
                break;
            case EnemyState.Dead:
                ForceEndAttack();
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
                smashHitbox?.Disable();
                break;
        }
    }

    private float GetWindupTime()
    {
        switch (currentAttack)
        {
            case TrollAttack.Smash: return smashWindupTime;
            case TrollAttack.Charge: return chargeWindupTime;
            case TrollAttack.Tornado: return tornadoWindupTime;
            default: return 0.5f;
        }
    }

    private float CurrentAttackMaxTime()
    {
        switch (currentAttack)
        {
            case TrollAttack.Smash: return smashMaxAttackTime;
            case TrollAttack.Charge: return chargeMaxAttackTime;
            case TrollAttack.Tornado: return tornadoMaxAttackTime;
            default: return 3f;
        }
    }

    private void ForceEndAttack()
    {
        smashHitbox?.Disable();
        chargeHitbox?.Disable();
        EndTornado();
        if (CurrentState == EnemyState.Attack)
            SetState(EnemyState.Idle);
    }

    private void CompleteWindup()
    {
        if (!IsServer) return;
        windupComplete = true;
        switch (currentAttack)
        {
            case TrollAttack.Smash:
                /* Smash hitbox is enabled by OnSmashImpact anim event */
                break;
            case TrollAttack.Charge:
                if (target != null)
                    dashDir = ((Vector2)(target.transform.position - transform.position)).normalized;
                else
                    dashDir = transform.right;
                dashDistanceLeft = chargeMaxDistance;
                chargeHitbox.Enable(chargeDamage, chargeEffects, true, chargeKnockbackForce, chargeKnockbackDuration);
                break;
            case TrollAttack.Tornado:
                if (NetworkObject.IsSpawned)
                    entity.isInvulnerable.Value = true;
                tornadoHitbox.Enable(tornadoDamagePerTick, null, false, 0f, 0f, tornadoTickInterval);
                if (tornadoVfxPrefab != null)
                {
                    tornadoVfxInstance = Instantiate(tornadoVfxPrefab, transform);
                    tornadoVfxInstance.transform.localPosition = Vector3.zero;
                }
                break;
        }
        anim.SetTrigger("Attack" + currentAttack);
    }

    public void OnSmashImpact()
    {
        if (!IsServer) return;
        smashHitbox.Enable(smashDamage, smashEffects, true, smashKnockbackForce, smashKnockbackDuration);
    }
    public void OnSmashEnd()
    {
        if (!IsServer) return;
        smashHitbox?.Disable();
        SetState(EnemyState.Idle);
    }

    private void EndCharge() { chargeHitbox?.Disable(); SetState(EnemyState.Idle); }
    private void CleanupTornado()
    {
        tornadoHitbox?.Disable();
        if (NetworkObject.IsSpawned)
            entity.isInvulnerable.Value = false;
        if (tornadoVfxInstance != null) { Destroy(tornadoVfxInstance); tornadoVfxInstance = null; }
        tornadoTimer = 0f;
    }
    private void EndTornado()
    {
        CleanupTornado();
        SetState(EnemyState.Idle);
    }
}
