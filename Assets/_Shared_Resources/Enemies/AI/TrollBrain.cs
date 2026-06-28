using UnityEngine;
using Unity.Netcode;
using UnityEngine.VFX;

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

    // Windup
    [Header("Troll - Windup")]
    [SerializeField] private VisualEffect windupAuraVfx;

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
    [SerializeField] private VisualEffect tornadoVfx;

    // Earth Spikes
    [Header("Troll - Earth Spikes")]
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private int spikeCount = 5;
    [SerializeField] private float spikeSpacing = 1.2f;
    [SerializeField] private float spikeSpawnInterval = 0.12f;

    // Recovery
    [Header("Troll - Recovery")]
    [SerializeField] private float recoveryInterval = 30f;
    [SerializeField] private float recoveryDuration = 3f;
    [SerializeField] private bool logRecoveryState = false;

    // Runtime state
    private TrollAttack currentAttack;
    private bool windupComplete;
    private Vector2 dashDir;
    private float dashDistanceLeft;
    private float tornadoTimer;
    private float recoveryTimer;
    private bool recoveryPending;

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
        if (recoveryTimer >= recoveryInterval && CurrentState != EnemyState.Attack && CurrentState != EnemyState.Recovery)
        {
            SetState(EnemyState.Recovery);
        }
        else if (recoveryTimer >= recoveryInterval && CurrentState == EnemyState.Attack)
        {
            recoveryPending = true;
            if (logRecoveryState) Debug.Log($"[TrollRecovery] pending=true (timer={recoveryTimer:F1}, attacking)");
        }

        if (CurrentState != EnemyState.Attack && CurrentState != EnemyState.Recovery)
            DecideNextState();

        if (CurrentState == EnemyState.Chase && target != null)
            motor.MoveToward(target.transform.position, entity.MoveSpeed * (effectController?.GetSpeedMultiplier() ?? 1f));

        if (CurrentState == EnemyState.Attack && !windupComplete && stateTimer >= GetWindupTime())
            CompleteWindup();

        if (CurrentState == EnemyState.Attack && currentAttack == TrollAttack.Tornado && windupComplete && target != null)
        {
            motor.MoveToward(target.transform.position, tornadoSpeed);
            Vector3 scale = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(scale.x), scale.y, scale.z);
            tornadoTimer += Time.fixedDeltaTime;
            if (tornadoTimer >= tornadoDuration)
                EndTornado();
        }

        if (CurrentState == EnemyState.Attack && currentAttack == TrollAttack.Charge && windupComplete)
        {
            float step = chargeSpeed * Time.fixedDeltaTime;
            if (dashDistanceLeft <= 0) { EndCharge(); return; }
            var hit = Physics2D.Raycast(transform.position, dashDir, step + 0.1f, obstacleMask);
            if (hit.collider != null) { EndCharge(); return; }
            motor.MoveToward((Vector2)transform.position + dashDir * step, chargeSpeed);
            dashDistanceLeft -= step;
        }

        stateTimer += Time.fixedDeltaTime;

        if (CurrentState == EnemyState.Recovery && stateTimer >= recoveryDuration)
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

        bool smashAvailable  = dist <= smashRange   && Time.time - lastSmashTime   >= smashCooldown;
        bool chargeAvailable = dist <= chargeRange  && Time.time - lastChargeTime  >= chargeCooldown;
        bool tornadoAvailable = dist > chargeRange  && Time.time - lastTornadoTime >= tornadoCooldown;

        if (smashAvailable)
        { currentAttack = TrollAttack.Smash; SetState(EnemyState.Attack); return; }

        if (chargeAvailable)
        { currentAttack = TrollAttack.Charge; SetState(EnemyState.Attack); return; }

        if (tornadoAvailable)
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
                anim.SetBool("IsAttacking", true);
                motor.Stop();
                switch (currentAttack)
                {
                    case TrollAttack.Smash: lastSmashTime = Time.time; break;
                    case TrollAttack.Charge: lastChargeTime = Time.time; break;
                    case TrollAttack.Tornado: lastTornadoTime = Time.time; break;
                }
                if (currentAttack == TrollAttack.Charge)
                {
                    if (target != null)
                        dashDir = ((Vector2)(target.transform.position - transform.position)).normalized;
                    else
                        dashDir = transform.right;
                }
                anim.SetTrigger("Windup" + currentAttack);
                SetAttackAudioId(currentAttack.ToString());
                break;
            case EnemyState.Idle:
                motor.Stop();
                anim.SetBool("IsAttacking", false);
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
                break;
            case EnemyState.Recovery:
                motor.Stop();
                anim.SetBool("IsRecovering", true);
                recoveryTimer = 0f;
                recoveryPending = false;
                if (logRecoveryState) Debug.Log($"[TrollRecovery] Recovery (timer={recoveryInterval:F1})");
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
                anim.SetBool("IsAttacking", false);
                break;
            case EnemyState.Recovery:
                anim.SetBool("IsRecovering", false);
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
                dashDistanceLeft = chargeMaxDistance;
                chargeHitbox?.Enable(chargeDamage, chargeEffects, true, chargeKnockbackForce, chargeKnockbackDuration);
                break;
            case TrollAttack.Tornado:
                if (NetworkObject.IsSpawned)
                    entity.isInvulnerable.Value = true;
                tornadoHitbox?.Enable(tornadoDamagePerTick, null, false, 0f, 0f, tornadoTickInterval);
                if (tornadoVfx != null)
                    tornadoVfx.Play();
                break;
        }
        anim.SetTrigger("Attack" + currentAttack);
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
        smashHitbox?.Enable(smashDamage, smashEffects, true, smashKnockbackForce, smashKnockbackDuration);
    }
    public void OnSmashEnd()
    {
        if (!IsServer) return;
        
        if (spikePrefab != null && target != null)
        {
            Vector2 dir = ((Vector2)(target.transform.position - transform.position)).normalized;
            StartCoroutine(SpawnSpikesRoutine(dir));
        }

        smashHitbox?.Disable();
        anim.SetBool("IsAttacking", false);
        EndAttackTransition();
    }
    private System.Collections.IEnumerator SpawnSpikesRoutine(Vector2 dir)
    {
        for (int i = 1; i <= spikeCount; i++)
        {
            Vector3 pos = transform.position + (Vector3)(dir * (i * spikeSpacing));
            GameObject spikeObj = Instantiate(spikePrefab, pos, Quaternion.identity);
            NetworkObject netObj = spikeObj.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
            EarthSpike spike = spikeObj.GetComponent<EarthSpike>();
            if (spike != null)
                spike.Initialize(smashDamage, smashEffects, smashKnockbackForce > 0f, smashKnockbackForce, smashKnockbackDuration, entity, dir);
            if (i < spikeCount)
                yield return new WaitForSeconds(spikeSpawnInterval);
        }
    }

    private void EndCharge() { chargeHitbox?.Disable(); anim.SetBool("IsAttacking", false); EndAttackTransition(); }
    private void CleanupTornado()
    {
        tornadoHitbox?.Disable();
        if (NetworkObject.IsSpawned)
            entity.isInvulnerable.Value = false;
        if (tornadoVfx != null)
            tornadoVfx.Stop();
        tornadoTimer = 0f;
    }
    private void EndTornado()
    {
        CleanupTornado();
        anim.SetBool("IsAttacking", false);
        EndAttackTransition();
    }
}
