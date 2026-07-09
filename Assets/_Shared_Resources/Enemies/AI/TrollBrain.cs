using UnityEngine;
using Unity.Netcode;
using UnityEngine.VFX;
using System.Collections;

public class TrollBrain : EnemyBrain
{
    public enum TrollAttack { Smash, Charge, Tornado }

    private TrollEnemyData trollData;

    // Attack Selection
    [Header("Troll - Attack Selection")]
    [SerializeField] private float aiDecisionInterval = 0.6f;
    private float lastDecisionTime;
    private float lastSmashTime;
    private float lastChargeTime;
    private float lastTornadoTime;

    // Windup
    [Header("Troll - Windup")]
    [SerializeField] private VisualEffect windupAuraVfx;

    // Smash
    [Header("Troll - Smash")]
    [SerializeField] private EnemyHitbox smashHitbox;

    // Charge
    [Header("Troll - Charge")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private EnemyHitbox chargeHitbox;

    // Tornado
    [Header("Troll - Tornado")]
    [SerializeField] private EnemyHitbox tornadoHitbox;
    [SerializeField] private VisualEffect tornadoVfx;

    // Recovery
    [Header("Troll - Recovery")]
    [SerializeField] private bool logRecoveryState = false;

    // Death
    public override bool HandlesOwnDeath => true;
    private const float DEATH_ANIMATION_DURATION = 1f;

    // Runtime state
    private TrollAttack currentAttack;
    private bool windupComplete;
    private Vector2 dashDir;
    private float dashDistanceLeft;
    private float tornadoTimer;
    private Collider2D bodyCollider;
    private float recoveryTimer;
    private bool recoveryPending;

    protected override void Init()
    {
        bodyCollider = GetComponent<Collider2D>();
        trollData = entity.GetData<TrollEnemyData>();
    }

    protected void FixedUpdate()
    {
        if (!IsServer) return;
        if (entity == null || !entity.IsAlive) { if (CurrentState != EnemyState.Dead) SetState(EnemyState.Dead); return; }
        if (CurrentState == EnemyState.Dead) return;
        if (IsCCLocked()) { if (CurrentState != EnemyState.Hurt) SetState(EnemyState.Hurt); return; }
        if (CurrentState == EnemyState.Hurt) { if (!IsCCLocked()) SetState(EnemyState.Idle); return; }

        AcquireTarget();

        // Recovery timer (pauses during Hurt/Dead — those early-return before this point)
        if (CurrentState != EnemyState.Recovery)
            recoveryTimer += Time.fixedDeltaTime;

        // Recovery trigger: enter Recovery if timer elapsed and not attacking
        if (recoveryTimer >= trollData.recoveryInterval && CurrentState != EnemyState.Attack && CurrentState != EnemyState.Recovery)
        {
            SetState(EnemyState.Recovery);
        }
        else if (recoveryTimer >= trollData.recoveryInterval && CurrentState == EnemyState.Attack)
        {
            recoveryPending = true;
            if (logRecoveryState) Debug.Log($"[TrollRecovery] pending=true (timer={recoveryTimer:F1}, attacking)");
        }

        if (CurrentState != EnemyState.Attack && CurrentState != EnemyState.Recovery)
            DecideNextState();

        if (CurrentState == EnemyState.Chase)
            MoveChaseTarget();

        if (CurrentState == EnemyState.Attack && !windupComplete && stateTimer >= GetWindupTime())
            CompleteWindup();

        if (CurrentState == EnemyState.Attack && currentAttack == TrollAttack.Tornado && windupComplete && target != null)
        {
            motor.MoveToward(target.transform.position, trollData.tornadoSpeed);
            Vector3 scale = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(scale.x), scale.y, scale.z);
            tornadoTimer += Time.fixedDeltaTime;
            if (tornadoTimer >= trollData.tornadoDuration)
                EndTornado();
        }

        if (CurrentState == EnemyState.Attack && currentAttack == TrollAttack.Charge && windupComplete)
        {
            float step = trollData.chargeSpeed * Time.fixedDeltaTime;
            if (dashDistanceLeft <= 0) { EndCharge(); return; }
            var hit = Physics2D.Raycast(ColliderCenter, dashDir, step + 0.1f, obstacleMask);
            if (hit.collider != null) { EndCharge(); return; }
            motor.MoveWith(dashDir, trollData.chargeSpeed);
            dashDistanceLeft -= step;
        }

        stateTimer += Time.fixedDeltaTime;

        if (CurrentState == EnemyState.Recovery && stateTimer >= trollData.recoveryDuration)
        {
            SetState(EnemyState.Idle);
            if (logRecoveryState) Debug.Log("[TrollRecovery] Idle (recovery ended)");
        }

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

        // Beyond max attack range: close the distance instead of attacking.
        if (dist > trollData.tornadoRange) { SetState(EnemyState.Chase); return; }

        bool smashReady   = Time.time - lastSmashTime   >= trollData.smashCooldown;
        bool chargeReady  = Time.time - lastChargeTime  >= trollData.chargeCooldown;
        bool tornadoReady = Time.time - lastTornadoTime >= trollData.tornadoCooldown;

        bool smashInRange   = dist <= trollData.smashRange;
        bool chargeInRange  = dist <= trollData.chargeRange;
        bool tornadoInRange = dist <= trollData.tornadoRange;

        // Each range prioritizes its matching attack, but falls back to any other
        // off-cooldown attack that is still within its own usable range.
        if (smashInRange)
        {
            // Close range: Smash > Charge > Tornado
            if (smashReady)                     { currentAttack = TrollAttack.Smash;   SetState(EnemyState.Attack); return; }
            if (chargeReady && chargeInRange)   { currentAttack = TrollAttack.Charge;  SetState(EnemyState.Attack); return; }
            if (tornadoReady && tornadoInRange) { currentAttack = TrollAttack.Tornado; SetState(EnemyState.Attack); return; }
        }
        else if (chargeInRange)
        {
            // Mid range: Charge > Tornado > Smash
            if (chargeReady)                    { currentAttack = TrollAttack.Charge;  SetState(EnemyState.Attack); return; }
            if (tornadoReady && tornadoInRange) { currentAttack = TrollAttack.Tornado; SetState(EnemyState.Attack); return; }
            if (smashReady && smashInRange)     { currentAttack = TrollAttack.Smash;   SetState(EnemyState.Attack); return; }
        }
        else
        {
            // Far range: Tornado > Charge > Smash
            if (tornadoReady && tornadoInRange) { currentAttack = TrollAttack.Tornado; SetState(EnemyState.Attack); return; }
            if (chargeReady && chargeInRange)   { currentAttack = TrollAttack.Charge;  SetState(EnemyState.Attack); return; }
            if (smashReady && smashInRange)     { currentAttack = TrollAttack.Smash;   SetState(EnemyState.Attack); return; }
        }

        SetState(EnemyState.Chase);
    }

    protected override void OnStateEnter(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Chase:
                SyncSetBool("IsChasing", true);
                if (target != null)
                    motor.MoveToward(target.transform.position, entity.Data.moveSpeed * (effectController?.GetSpeedMultiplier() ?? 1f));
                break;
            case EnemyState.Attack:
                windupComplete = false;
                SyncSetBool("IsAttacking", true);
                motor.Stop();
                switch (currentAttack)
                {
                    case TrollAttack.Smash: lastSmashTime = Time.time; break;
                    case TrollAttack.Charge: lastChargeTime = Time.time; break;
                    case TrollAttack.Tornado: lastTornadoTime = Time.time; break;
                }
                SyncSetTrigger("Windup" + currentAttack);
                break;
            case EnemyState.Idle:
                motor.Stop();
                SyncSetBool("IsAttacking", false);
                smashHitbox?.Disable();
                chargeHitbox?.Disable();
                CleanupTornado();
                break;
            case EnemyState.Hurt:
                recoveryPending = false;
                motor.Stop();
                smashHitbox?.Disable();
                chargeHitbox?.Disable();
                CleanupTornado();
                break;
            case EnemyState.Dead:
                recoveryPending = false;
                ForceEndAttack();
                SyncSetTrigger("Dead");
                StartCoroutine(DeathSequenceAfterAnimation());
                break;
            case EnemyState.Recovery:
                motor.Stop();
                SyncSetBool("IsRecovering", true);
                recoveryTimer = 0f;
                recoveryPending = false;
                if (logRecoveryState) Debug.Log($"[TrollRecovery] Recovery (timer={trollData.recoveryInterval:F1})");
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
                smashHitbox?.Disable();
                SyncSetBool("IsAttacking", false);
                break;
            case EnemyState.Recovery:
                SyncSetBool("IsRecovering", false);
                break;
        }
    }

    private float GetWindupTime()
    {
        switch (currentAttack)
        {
            case TrollAttack.Smash: return trollData.smashWindupTime;
            case TrollAttack.Charge: return trollData.chargeWindupTime;
            case TrollAttack.Tornado: return trollData.tornadoWindupTime;
            default: return 0.5f;
        }
    }

    private float CurrentAttackMaxTime()
    {
        switch (currentAttack)
        {
            case TrollAttack.Smash: return trollData.smashMaxAttackTime;
            case TrollAttack.Charge: return trollData.chargeMaxAttackTime;
            case TrollAttack.Tornado: return trollData.tornadoMaxAttackTime;
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

    private void EndAttackTransition()
    {
        if (recoveryPending)
            SetState(EnemyState.Recovery);
        else
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
                dashDir = target != null
                    ? ((Vector2)(target.transform.position - (Vector3)ColliderCenter)).normalized
                    : transform.right;
                dashDistanceLeft = trollData.chargeMaxDistance;
                chargeHitbox?.Enable(trollData.chargeDamage, trollData.chargeEffects, true, trollData.chargeKnockbackForce, trollData.chargeKnockbackDuration);
                break;
            case TrollAttack.Tornado:
                if (NetworkObject.IsSpawned)
                    entity.isInvulnerable.Value = true;
                if (bodyCollider != null)
                    bodyCollider.enabled = false;
                tornadoHitbox?.Enable(trollData.tornadoDamagePerTick, null, false, 0f, 0f, trollData.tornadoTickInterval);
                if (tornadoVfx != null)
                    tornadoVfx.Play();
                break;
        }
        SyncSetTrigger("Attack" + currentAttack);
        enemyAudio.Play(currentAttack.ToString() + "Start");
    }

    public void EmitWindupAura()
    {
        if (windupAuraVfx != null)
        {
            // Number of aura particle to emit: Smash = 1, Charge = 2, Tornado = 3
            int auraCount = 1;
            switch (currentAttack)
            {
                case TrollAttack.Smash: auraCount = 1; break;
                case TrollAttack.Charge: auraCount = 2; break;
                case TrollAttack.Tornado: auraCount = 3; break;
            }
            VFXEventAttribute eventAttribute = windupAuraVfx.CreateVFXEventAttribute();
            eventAttribute.SetInt("BurstAmount", auraCount);
            windupAuraVfx.SendEvent("OnPlay", eventAttribute);
        }
    }

    public void OnSmashImpact()
    {
        if (!IsServer) return;
        smashHitbox?.Enable(trollData.smashDamage, trollData.smashEffects, true, trollData.smashKnockbackForce, trollData.smashKnockbackDuration);
    }
    public void OnSmashEnd()
    {
        if (!IsServer) return;
        
        if (trollData.spikePrefab != null && target != null)
        {
            Vector2 dir = ((Vector2)(target.transform.position - transform.position)).normalized;
            StartCoroutine(SpawnSpikesRoutine(dir));
        }

        smashHitbox?.Disable();
        SyncSetBool("IsAttacking", false);
        EndAttackTransition();
    }
    private System.Collections.IEnumerator SpawnSpikesRoutine(Vector2 dir)
    {
        for (int i = 1; i <= trollData.spikeCount; i++)
        {
            Vector3 pos = transform.position + (Vector3)(dir * (i * trollData.spikeSpacing));
            GameObject spikeObj = Instantiate(trollData.spikePrefab, pos, Quaternion.identity);
            Vector3 spikeScale = spikeObj.transform.localScale;
            float travelFacingSign = dir.x >= 0 ? 1f : -1f;
            spikeObj.transform.localScale = new Vector3(travelFacingSign * Mathf.Abs(spikeScale.x), spikeScale.y, spikeScale.z);

            NetworkObject netObj = spikeObj.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
            EarthSpike spike = spikeObj.GetComponent<EarthSpike>();
            if (spike != null)
                spike.Initialize(trollData.smashDamage, trollData.smashEffects, trollData.smashKnockbackForce > 0f, trollData.smashKnockbackForce, trollData.smashKnockbackDuration, entity, dir);
            if (i < trollData.spikeCount)
                yield return new WaitForSeconds(trollData.spikeSpawnInterval);
        }
    }

    private void EndCharge() { chargeHitbox?.Disable(); SyncSetBool("IsAttacking", false); EndAttackTransition(); }
    private void CleanupTornado()
    {
        tornadoHitbox?.Disable();
        if (NetworkObject.IsSpawned)
            entity.isInvulnerable.Value = false;
        if (bodyCollider != null)
            bodyCollider.enabled = true;
        if (tornadoVfx != null)
            tornadoVfx.Stop();
        tornadoTimer = 0f;
    }
    private void EndTornado()
    {
        CleanupTornado();
        SyncSetBool("IsAttacking", false);
        EndAttackTransition();
    }

    private IEnumerator DeathSequenceAfterAnimation()
    {
        yield return new WaitForSeconds(DEATH_ANIMATION_DURATION);
        GetComponent<EnemySpawnDeath>()?.PlayDeathSequence();
    }
}
