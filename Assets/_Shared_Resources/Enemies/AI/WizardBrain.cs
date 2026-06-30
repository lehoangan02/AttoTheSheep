using UnityEngine;
using Unity.Netcode;

public class WizardBrain : EnemyBrain
{
    [Header("Throw Ball")]
    [SerializeField] private Transform throwSpawn;

    private WizardEnemyData wizData;

    private float lastThrowTime;
    private float lastTransformTime;
    private LambAI currentTransformTarget;

    protected override void Awake()
    {
        base.Awake();
        wizData = entity.GetData<WizardEnemyData>();
    }

    protected void FixedUpdate()
    {
        if (!IsServer) return;
        if (entity == null || !entity.IsAlive) { if (CurrentState != EnemyState.Dead) SetState(EnemyState.Dead); return; }
        if (CurrentState == EnemyState.Dead) return;
        if (IsCCLocked()) { if (CurrentState != EnemyState.Hurt) SetState(EnemyState.Hurt); return; }
        if (CurrentState == EnemyState.Hurt) { if (!IsCCLocked()) SetState(EnemyState.Idle); return; }

        AcquireTarget();
        if (CurrentState != EnemyState.Attack && CurrentState != EnemyState.Cast)
            DecideNextState();

        if (CurrentState == EnemyState.Chase)
            MoveChaseTarget();

        stateTimer += Time.fixedDeltaTime;

        if (CurrentState == EnemyState.Attack && stateTimer > 5f)
            SetState(EnemyState.Idle);
        if (CurrentState == EnemyState.Cast && stateTimer > 5f)
            SetState(EnemyState.Idle);
    }

    protected override NetworkEntity FindTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, scanRadius, targetLayers);
        NetworkEntity nearestLamb = null;
        float nearestLambDist = float.MaxValue;
        NetworkEntity nearestAtto = null;
        float nearestAttoDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            NetworkEntity candidate = hit.GetComponentInParent<NetworkEntity>();
            if (!IsValidTarget(candidate)) continue;

            LambAI lamb = candidate as LambAI;
            if (lamb != null)
            {
                float d = ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < nearestLambDist) { nearestLamb = candidate; nearestLambDist = d; }
                continue;
            }

            PlayerController pc = candidate.gameObject.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                float d = ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < nearestAttoDist) { nearestAtto = candidate; nearestAttoDist = d; }
            }
        }

        return nearestLamb ?? nearestAtto;
    }

    protected override void DecideNextState()
    {
        if (target == null) { SetState(EnemyState.Idle); return; }

        float dist = DistanceTo(target);

        LambAI lambTarget = target as LambAI;
        if (lambTarget != null && lambTarget.IsAlive
            && dist <= wizData.transformRange && IsTransformReady())
        {
            SetState(EnemyState.Cast);
            return;
        }

        if (dist <= wizData.throwRange && IsThrowReady())
        {
            SetState(EnemyState.Attack);
            return;
        }

        SetState(EnemyState.Chase);
    }

    protected override void OnStateEnter(EnemyState state)
    {
        float speedMult = effectController?.GetSpeedMultiplier() ?? 1f;
        switch (state)
        {
            case EnemyState.Chase:
                anim.SetBool("IsChasing", true);
                if (target != null)
                    motor.MoveToward(target.transform.position, entity.Data.moveSpeed * speedMult);
                break;
            case EnemyState.Attack:
                anim.SetTrigger("Attack");
                motor.Stop();
                lastThrowTime = Time.time;
                enemyAudio.Play("ThrowStart");
                break;
            case EnemyState.Cast:
                Debug.Log("[WizardBrain] Transforming lamb: " + target.name);
                anim.SetTrigger("Transform");
                motor.Stop();
                lastTransformTime = Time.time;
                currentTransformTarget = target as LambAI;
                enemyAudio.Play("TransformStart");
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
                break;
            case EnemyState.Cast:
                break;
        }
    }

    private bool IsThrowReady() => Time.time >= lastThrowTime + wizData.throwCooldown;
    private bool IsTransformReady() => Time.time >= lastTransformTime + wizData.transformCooldown;

    public void OnThrowSpawnBall()
    {
        if (!IsServer) return;
        if (wizData.explosionBallPrefab == null || throwSpawn == null) return;
        if (target == null) return;

        Vector2 dir = (target.transform.position - throwSpawn.position).normalized;
        GameObject ballObj = Instantiate(wizData.explosionBallPrefab, throwSpawn.position, Quaternion.identity);
        NetworkObject netObj = ballObj.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn();

        ExplosionBallProjectile proj = ballObj.GetComponent<ExplosionBallProjectile>();
        if (proj != null)
            proj.Initialize(dir, wizData.ballSpeed, wizData.throwDamage, null, entity);
    }

    public void OnThrowEnd()
    {
        if (!IsServer) return;
        SetState(EnemyState.Idle);
    }

    public void OnTransformHit()
    {
        if (!IsServer) return;
        if (currentTransformTarget == null || !currentTransformTarget.IsAlive) return;

        Vector3 spawnPos = currentTransformTarget.transform.position;

        if (wizData.transformSpellVFXPrefab != null)
        {
            GameObject vfx = Instantiate(wizData.transformSpellVFXPrefab, spawnPos, Quaternion.identity);
            NetworkObject vfxNetObj = vfx.GetComponent<NetworkObject>();
            if (vfxNetObj != null) vfxNetObj.Spawn();
        }

        currentTransformTarget.TakeDamage(9999, entity);

        if (wizData.pigPrefab != null)
        {
            GameObject pig = Instantiate(wizData.pigPrefab, spawnPos, Quaternion.identity);
            NetworkObject pigNetObj = pig.GetComponent<NetworkObject>();
            if (pigNetObj != null) pigNetObj.Spawn();
        }
    }

    public void OnTransformEnd()
    {
        if (!IsServer) return;
        SetState(EnemyState.Idle);
    }
}
