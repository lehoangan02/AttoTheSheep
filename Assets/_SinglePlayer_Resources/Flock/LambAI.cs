using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class LambAI : NetworkEntity
{
    [Header("Shield / Invulnerability Settings")]
    public NetworkVariable<bool> isShielded = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private Color shieldColor = new Color(0f, 0.7f, 1f, 0.5f);
    [SerializeField] private float shieldFadeDuration = 0.5f;
    [SerializeField] private Color shieldAuraColor = new Color(0.2f, 0.85f, 1f, 0.22f);
    [SerializeField] private float shieldAuraScale = 1.28f;
    [SerializeField] private float shieldPulseScale = 1.08f;
    [SerializeField] private float shieldPulseDuration = 0.18f;

    [Header("Speed Boost Settings")]
    private Coroutine shieldRoutine;
    private Coroutine speedBoostRoutine;
    private Coroutine shieldAuraRoutine;
    private GameObject shieldAuraObject;
    private SpriteRenderer shieldAuraRenderer;
    [SerializeField] private ParticleSystem reviveSpawnParticles;

    [Header("Movement Settings")]
    [SerializeField] private float stoppingDistance = 0.1f;
    [SerializeField] private float slowingRadius = 2f;
    [SerializeField] private float accelerationRate = 5f;

    [Header("Noise Settings")]
    [Range(0f, 0.9f)]
    [SerializeField] private float speedNoiseRange = 0.25f;

    [Header("Damage Feedback (Hiệu ứng)")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float knockbackDuration = 0.15f;

    // ==========================================

    // ==========================================
    [Header("Panic & Out of Bounds Settings")]
    [Tooltip("Tốc độ bầy cừu nhân thêm khi hoảng loạn (VD: 1.6 = nhanh hơn 60%)")]
    [SerializeField] private float panicSpeedMultiplier = 1.6f;
    [Tooltip("Tốc độ nhân thêm khi cố chạy ngược về bầy")]
    [SerializeField] private float returnSpeedMultiplier = 1.3f;

    [Header("Stuck Recovery Settings")]
    [SerializeField] private float stuckCheckInterval = 0.25f;
    [SerializeField] private float stuckMinMoveDistance = 0.03f;
    [SerializeField] private float stuckTimeToRecover = 0.75f;
    [SerializeField] private float unstuckDuration = 0.8f;
    [SerializeField] private float unstuckSideStepDistance = 1.2f;
    [SerializeField] private float unstuckClearanceRadius = 0.25f;

    private bool isPanicking = false;
    private float panicTimer = 0f;
    private Vector2 panicTargetPos;
    private bool isForceReturning = false;
    private bool isRecoveringFromStuck = false;
    private float unstuckTimer = 0f;
    private Vector2 unstuckTargetPos;
    private int unstuckSideSign = 1;
    private float stuckCheckTimer = 0f;
    private float stuckTimer = 0f;
    private Vector2 lastStuckCheckPosition;

    private float personalSpeedMultiplier = 1f;
    private Color originalColor;
    private bool isMovementLocked = false;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private FlockManager myManager;
    private LayerMask obstacleLayer;

    private Vector2 flockCenter;
    private float flockRadius;
    private Vector2 localOffset;
    private bool hasTarget = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (reviveSpawnParticles == null) reviveSpawnParticles = GetComponentInChildren<ParticleSystem>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            EnsureShieldAura();
        }
    }

    public void PlayReviveSpawnFx()
    {
        if (reviveSpawnParticles == null) return;

        reviveSpawnParticles.Play(true);
    }

    void Start()
    {
        personalSpeedMultiplier = Random.Range(1f - speedNoiseRange, 1f + speedNoiseRange);
        lastStuckCheckPosition = rb.position;
    }

    public void Initialize(FlockManager manager)
    {
        myManager = manager;
    }

    public void Initialize(FlockManager manager, LayerMask recoveryObstacleLayer)
    {
        myManager = manager;
        obstacleLayer = recoveryObstacleLayer;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    protected override void Die()
    {
        if (myManager != null)
        {
            myManager.RemoveLamb(this);
        }
        base.Die();
    }

    public void SetFlockData(Vector2 center, float radius)
    {
        flockCenter = center;
        flockRadius = radius;
        hasTarget = true;

        if (!isPanicking && !isForceReturning)
        {
            PickNewOffset();
        }
    }

    private void PickNewOffset()
    {
        localOffset = Random.insideUnitCircle * flockRadius;
    }

    // ==========================================

    // ==========================================
    public void TriggerPanic(float duration)
    {
        if (!IsServer) return;

        isPanicking = true;
        isForceReturning = false;
        panicTimer = duration;
        PickRandomPanicTarget();
    }

    public void NotifyOutOfBounds(Vector2 center)
    {
        if (!IsServer) return;

        if (isPanicking) return;

        flockCenter = center;
        isForceReturning = true;
    }

    private void PickRandomPanicTarget()
    {

        panicTargetPos = (Vector2)transform.position + Random.insideUnitCircle * 2.5f;
    }

    void FixedUpdate()
    {
        if (!IsSpawned || !IsServer || isMovementLocked) return;
        if (effectController != null && effectController.IsMovementLocked()) return;

        if (isPanicking)
        {
            panicTimer -= Time.fixedDeltaTime;
            if (panicTimer <= 0f)
            {
                isPanicking = false;
                PickNewOffset();
            }
        }

        Vector2 actualTarget = Vector2.zero;
        float effectMult = effectController != null ? effectController.GetSpeedMultiplier() : 1f;
        float maxSpeedWithNoise = currentMoveSpeed.Value * personalSpeedMultiplier * effectMult;
        float targetSpeed = maxSpeedWithNoise;

        bool shouldMove = false;
        bool applySlowingRadius = false;

        if (isPanicking)
        {

            actualTarget = panicTargetPos;
            targetSpeed = maxSpeedWithNoise * panicSpeedMultiplier;
            shouldMove = true;

            if (Vector2.Distance(transform.position, panicTargetPos) < 0.4f)
            {
                PickRandomPanicTarget();
            }
        }
        else if (isForceReturning)
        {

            actualTarget = flockCenter;
            targetSpeed = maxSpeedWithNoise * returnSpeedMultiplier;
            shouldMove = true;

            if (Vector2.Distance(transform.position, flockCenter) < flockRadius * 0.6f)
            {
                isForceReturning = false;
                PickNewOffset();
            }
        }
        else if (hasTarget)
        {

            actualTarget = flockCenter + localOffset;
            shouldMove = true;
            applySlowingRadius = true;
        }

        if (shouldMove)
        {
            float distToTarget = Vector2.Distance(transform.position, actualTarget);

            UpdateStuckRecovery(actualTarget, distToTarget);
            if (isRecoveringFromStuck)
            {
                actualTarget = unstuckTargetPos;
                targetSpeed = maxSpeedWithNoise * returnSpeedMultiplier;
                applySlowingRadius = false;
                distToTarget = Vector2.Distance(transform.position, actualTarget);
            }

            if (distToTarget > stoppingDistance)
            {
                Vector2 direction = (actualTarget - (Vector2)transform.position).normalized;

                if (applySlowingRadius && distToTarget < slowingRadius)
                {
                    targetSpeed = maxSpeedWithNoise * (distToTarget / slowingRadius);
                }

                Vector2 desiredVelocity = direction * targetSpeed;
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, accelerationRate * Time.fixedDeltaTime);
                UpdateAnimationLocal(true);
            }
            else
            {

                if (!isPanicking && !isForceReturning)
                {
                    rb.linearVelocity = Vector2.zero;
                    hasTarget = false;
                    UpdateAnimationLocal(false);
                }
            }
        }
        else
        {

            isRecoveringFromStuck = false;
            ResetStuckTracking();
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, accelerationRate * Time.fixedDeltaTime);
            UpdateAnimationLocal(rb.linearVelocity.magnitude > 0.1f);
        }
    }

    private void UpdateStuckRecovery(Vector2 desiredTarget, float distToTarget)
    {
        if (distToTarget <= stoppingDistance)
        {
            ResetStuckTracking();
            return;
        }

        if (isRecoveringFromStuck)
        {
            unstuckTimer -= Time.fixedDeltaTime;
            if (unstuckTimer <= 0f || Vector2.Distance(transform.position, unstuckTargetPos) <= stoppingDistance)
            {
                isRecoveringFromStuck = false;
                ResetStuckTracking();
            }
            return;
        }

        stuckCheckTimer += Time.fixedDeltaTime;
        if (stuckCheckTimer < stuckCheckInterval) return;

        float movedDistance = Vector2.Distance(rb.position, lastStuckCheckPosition);
        if (movedDistance < stuckMinMoveDistance)
        {
            stuckTimer += stuckCheckTimer;
        }
        else
        {
            stuckTimer = 0f;
        }

        stuckCheckTimer = 0f;
        lastStuckCheckPosition = rb.position;

        if (stuckTimer >= stuckTimeToRecover)
        {
            StartStuckRecovery(desiredTarget);
        }
    }

    private void StartStuckRecovery(Vector2 desiredTarget)
    {
        Vector2 currentPosition = rb.position;
        Vector2 toTarget = desiredTarget - currentPosition;
        if (toTarget.sqrMagnitude < 0.001f)
        {
            toTarget = Random.insideUnitCircle;
            if (toTarget.sqrMagnitude < 0.001f)
            {
                toTarget = Vector2.right;
            }
        }

        toTarget.Normalize();

        Vector2 side = new Vector2(-toTarget.y, toTarget.x) * unstuckSideSign;
        unstuckSideSign *= -1;

        Vector2[] candidates =
        {
            currentPosition + side * unstuckSideStepDistance + toTarget * (unstuckSideStepDistance * 0.35f),
            currentPosition - side * unstuckSideStepDistance + toTarget * (unstuckSideStepDistance * 0.35f),
            currentPosition + side * unstuckSideStepDistance - toTarget * (unstuckSideStepDistance * 0.35f),
            currentPosition - side * unstuckSideStepDistance - toTarget * (unstuckSideStepDistance * 0.35f),
            currentPosition - toTarget * unstuckSideStepDistance
        };

        unstuckTargetPos = candidates[0];
        for (int i = 0; i < candidates.Length; i++)
        {
            if (IsRecoveryPointClear(candidates[i]))
            {
                unstuckTargetPos = candidates[i];
                break;
            }
        }

        isRecoveringFromStuck = true;
        unstuckTimer = unstuckDuration;
        stuckTimer = 0f;
        stuckCheckTimer = 0f;
        lastStuckCheckPosition = currentPosition;
    }

    private bool IsRecoveryPointClear(Vector2 point)
    {
        if (obstacleLayer.value == 0) return true;
        return Physics2D.OverlapCircle(point, unstuckClearanceRadius, obstacleLayer) == null;
    }

    private void ResetStuckTracking()
    {
        stuckTimer = 0f;
        stuckCheckTimer = 0f;
        lastStuckCheckPosition = rb.position;
    }

    private void UpdateAnimationLocal(bool isMoving)
    {
        if (animator != null) animator.SetBool("IsMoving", isMoving);

        if (spriteRenderer != null)
        {
            if (rb.linearVelocity.x > 0.01f) spriteRenderer.flipX = false;
            else if (rb.linearVelocity.x < -0.01f) spriteRenderer.flipX = true;
        }
    }

    // ==========================================

    // ==========================================
    // ==========================================
    // SHIELD / INVULNERABILITY SYSTEM
    // ==========================================
    public void SetShieldedState(bool shielded, float duration)
    {
        if (!IsServer) return;

        if (shieldRoutine != null)
        {
            StopCoroutine(shieldRoutine);
            shieldRoutine = null;
        }

        isShielded.Value = shielded;
        UpdateShieldVisualClientRpc(shielded);

        if (shielded && duration > 0f)
        {
            shieldRoutine = StartCoroutine(ShieldTimerRoutine(duration));
        }
    }

    private System.Collections.IEnumerator ShieldTimerRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (isShielded.Value)
        {
            isShielded.Value = false;
            UpdateShieldVisualClientRpc(false);
        }

        shieldRoutine = null;
    }

    [ClientRpc]
    private void UpdateShieldVisualClientRpc(bool shielded)
    {
        if (spriteRenderer != null)
        {
            if (shielded)
            {
                // Store original color and apply shield color
                originalColor = spriteRenderer.color;
                spriteRenderer.color = shieldColor;
                EnsureShieldAura();
                ShowShieldAura(true);
                StartShieldAuraPulse();
            }
            else
            {
                spriteRenderer.color = originalColor;
                ShowShieldAura(false);
            }
        }
    }

    private void EnsureShieldAura()
    {
        if (spriteRenderer == null || shieldAuraObject != null) return;

        shieldAuraObject = new GameObject("ShieldAura");
        shieldAuraObject.transform.SetParent(spriteRenderer.transform, false);
        shieldAuraObject.transform.localPosition = Vector3.zero;
        shieldAuraObject.transform.localRotation = Quaternion.identity;
        shieldAuraObject.transform.localScale = Vector3.one * shieldAuraScale;

        shieldAuraRenderer = shieldAuraObject.AddComponent<SpriteRenderer>();
        shieldAuraRenderer.sprite = spriteRenderer.sprite;
        shieldAuraRenderer.flipX = spriteRenderer.flipX;
        shieldAuraRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        shieldAuraRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        shieldAuraRenderer.color = shieldAuraColor;
        shieldAuraRenderer.enabled = false;
    }

    private void ShowShieldAura(bool show)
    {
        if (shieldAuraRenderer == null) return;
        shieldAuraRenderer.enabled = show;
    }

    private void StartShieldAuraPulse()
    {
        if (shieldAuraRenderer == null) return;

        if (shieldAuraRoutine != null)
        {
            StopCoroutine(shieldAuraRoutine);
        }

        shieldAuraRoutine = StartCoroutine(ShieldAuraPulseRoutine());
    }

    private IEnumerator ShieldAuraPulseRoutine()
    {
        if (shieldAuraObject == null)
        {
            shieldAuraRoutine = null;
            yield break;
        }

        Vector3 baseScale = Vector3.one * shieldAuraScale;
        Vector3 pulseScale = baseScale * shieldPulseScale;

        float elapsed = 0f;
        while (elapsed < shieldPulseDuration)
        {
            float t = elapsed / Mathf.Max(0.01f, shieldPulseDuration);
            float pulseT = Mathf.PingPong(t * 2f, 1f);
            shieldAuraObject.transform.localScale = Vector3.Lerp(baseScale, pulseScale, pulseT);
            shieldAuraRenderer.color = Color.Lerp(shieldAuraColor, Color.white, pulseT * 0.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (shieldAuraObject != null)
        {
            shieldAuraObject.transform.localScale = baseScale;
        }

        if (shieldAuraRenderer != null)
        {
            shieldAuraRenderer.color = shieldAuraColor;
        }

        shieldAuraRoutine = null;
    }

    public override void TakeDamage(int damage, NetworkEntity source)
    {
        // BLOCK DAMAGE IF SHIELDED
        if (isShielded.Value)
        {

            // Still play a little visual feedback to show the shield absorbed hit
            if (spriteRenderer != null)
            {
                StopCoroutine(nameof(FlashShieldRoutine));
                StartCoroutine(nameof(FlashShieldRoutine));
            }
            return; // NO DAMAGE TAKEN
        }

        int healthBefore = currentHealth.Value;

        base.TakeDamage(damage, source);

        if (IsServer && currentHealth.Value < healthBefore)
        {

            if (myManager != null)
            {
                myManager.ReportLambAttacked(this);
            }

            if (source != null)
            {
                Vector2 knockbackDirection = ((Vector2)transform.position - (Vector2)source.transform.position).normalized;
                Vector2 appliedForce = knockbackDirection * knockbackForce;

                ApplyKnockbackClientRpc(appliedForce);
            }
        }
    }

    private IEnumerator FlashShieldRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.color = shieldColor;
        }
    }

    // ==========================================
    // SPEED BOOST SYSTEM
    // ==========================================
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (!IsServer) return;

        if (speedBoostRoutine != null)
        {
            StopCoroutine(speedBoostRoutine);
            currentMoveSpeed.Value = BaseMoveSpeed;
        }

        speedBoostRoutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    public void RevertSpeedBoost()
    {
        if (!IsServer) return;

        if (speedBoostRoutine != null)
        {
            StopCoroutine(speedBoostRoutine);
            speedBoostRoutine = null;
        }
        currentMoveSpeed.Value = BaseMoveSpeed;
    }

    private System.Collections.IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        float baseSpeed = BaseMoveSpeed;
        float boostedSpeed = baseSpeed * multiplier;

        currentMoveSpeed.Value = boostedSpeed;
        yield return new WaitForSeconds(duration);

        currentMoveSpeed.Value = baseSpeed;
        speedBoostRoutine = null;
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        if (newValue < previousValue)
        {
            if (spriteRenderer != null)
            {
                StopCoroutine(nameof(FlashRedRoutine));
                StartCoroutine(nameof(FlashRedRoutine));
            }
        }
    }

    private IEnumerator FlashRedRoutine()
    {
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    [ClientRpc]
    private void ApplyKnockbackClientRpc(Vector2 force)
    {
        if (rb != null)
        {
            StartCoroutine(KnockbackRoutine(force));
        }
    }

    private IEnumerator KnockbackRoutine(Vector2 force)
    {
        isMovementLocked = true;
        rb.linearVelocity = force;
        yield return new WaitForSeconds(knockbackDuration);
        rb.linearVelocity = Vector2.zero;
        isMovementLocked = false;
        ResetStuckTracking();
    }
}
