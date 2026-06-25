using UnityEngine;
using Unity.Netcode;

public class WizardBrain : EnemyBrain
{
    [Header("Throw Ball")]
    [SerializeField] private float throwRange = 8f;
    [SerializeField] private float throwCooldown = 1.5f;
    [SerializeField] private int throwDamage = 40;
    [SerializeField] private float ballSpeed = 7f;
    [SerializeField] private GameObject explosionBallPrefab;
    [SerializeField] private Transform throwSpawn;

    [Header("Transform")]
    [SerializeField] private float transformRange = 3f;
    [SerializeField] private float transformCooldown = 8f;
    [SerializeField] private GameObject transformSpellVFXPrefab;
    [SerializeField] private GameObject pigPrefab;

    private float lastThrowTime;
    private float lastTransformTime;
    private LambAI currentTransformTarget;

    protected void FixedUpdate()
    {
        if (!IsServer) return;
        if (entity == null || !entity.IsAlive) { if (CurrentState != EnemyState.Dead) SetState(EnemyState.Dead); return; }
        if (CurrentState == EnemyState.Dead) return;
        if (IsCCLocked()) { if (CurrentState != EnemyState.Hurt) SetState(EnemyState.Hurt); return; }
        if (CurrentState == EnemyState.Hurt) { if (!IsCCLocked()) SetState(EnemyState.Idle); return; }

        AcquireTarget();
        if (CurrentState != EnemyState.Attack && CurrentState != EnemyState.Guard)
            DecideNextState();

        if (CurrentState == EnemyState.Chase && target != null)
            motor.MoveToward(target.transform.position, entity.MoveSpeed * (effectController?.GetSpeedMultiplier() ?? 1f));

        stateTimer += Time.fixedDeltaTime;

        if (CurrentState == EnemyState.Attack && stateTimer > 5f)
            SetState(EnemyState.Idle);
        if (CurrentState == EnemyState.Guard && stateTimer > 5f)
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
            && dist <= transformRange && IsTransformReady())
        {
            SetState(EnemyState.Guard);
            return;
        }

        if (dist <= throwRange && IsThrowReady())
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
                    motor.MoveToward(target.transform.position, entity.MoveSpeed * speedMult);
                break;
            case EnemyState.Attack:
                anim.SetTrigger("Attack");
                motor.Stop();
                lastThrowTime = Time.time;
                SetAttackAudioId("Throw");
                break;
            case EnemyState.Guard:
                Debug.Log("[WizardBrain] Transforming lamb: " + target.name);
                anim.SetTrigger("Transform");
                motor.Stop();
                lastTransformTime = Time.time;
                currentTransformTarget = target as LambAI;
                SetAttackAudioId("Transform");
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
            case EnemyState.Guard:
                break;
        }
    }

    private bool IsThrowReady() => Time.time >= lastThrowTime + throwCooldown;
    private bool IsTransformReady() => Time.time >= lastTransformTime + transformCooldown;

    public void OnThrowSpawnBall()
    {
        if (!IsServer) return;
        if (explosionBallPrefab == null || throwSpawn == null) return;
        if (target == null) return;

        Vector2 dir = (target.transform.position - throwSpawn.position).normalized;
        GameObject ballObj = Instantiate(explosionBallPrefab, throwSpawn.position, Quaternion.identity);
        NetworkObject netObj = ballObj.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn();

        ExplosionBallProjectile proj = ballObj.GetComponent<ExplosionBallProjectile>();
        if (proj != null)
            proj.Initialize(dir, ballSpeed, throwDamage, null, entity);
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

        if (transformSpellVFXPrefab != null)
        {
            GameObject vfx = Instantiate(transformSpellVFXPrefab, spawnPos, Quaternion.identity);
            NetworkObject vfxNetObj = vfx.GetComponent<NetworkObject>();
            if (vfxNetObj != null) vfxNetObj.Spawn();
        }

        currentTransformTarget.TakeDamage(9999, entity);

        if (pigPrefab != null)
        {
            GameObject pig = Instantiate(pigPrefab, spawnPos, Quaternion.identity);
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
