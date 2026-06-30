using UnityEngine;
using Unity.Netcode;
using System.Collections;

// Kế thừa trực tiếp từ NetworkEntity
public class LambAI : NetworkEntity 
{
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
    // TÍNH NĂNG MỚI: CẤU HÌNH HOẢNG LOẠN & QUAY LẠI BẦY
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

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
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
        
        // Nếu không hoảng loạn hoặc đang bị ép quay về bầy thì mới đổi offset ngẫu nhiên mới
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
    // TÍNH NĂNG MỚI: NHẬN LỆNH TỪ FLOCK MANAGER
    // ==========================================
    public void TriggerPanic(float duration)
    {
        if (!IsServer) return;

        isPanicking = true;
        isForceReturning = false; // Ưu tiên hoảng loạn cao hơn quay về bầy
        panicTimer = duration;
        PickRandomPanicTarget();
    }

    public void NotifyOutOfBounds(Vector2 center)
    {
        if (!IsServer) return;

        // Nếu đang bận hoảng loạn chạy trốn quái thì cứ để nó chạy xong đã
        if (isPanicking) return;

        flockCenter = center;
        isForceReturning = true;
    }

    private void PickRandomPanicTarget()
    {
        // Chọn ngẫu nhiên một hướng trong phạm vi ngắn (2 đến 3 mét) quanh vị trí hiện tại
        panicTargetPos = (Vector2)transform.position + Random.insideUnitCircle * 2.5f;
    }

    void FixedUpdate()
    {
        if (!IsSpawned || !IsServer || isMovementLocked) return;
        if (effectController != null && effectController.IsMovementLocked()) return;

        // 1. CẬP NHẬT TIMER HOẢNG LOẠN
        if (isPanicking)
        {
            panicTimer -= Time.fixedDeltaTime;
            if (panicTimer <= 0f)
            {
                isPanicking = false;
                PickNewOffset(); // Hết hoảng loạn, hồi phục và chọn vị trí trong bầy
            }
        }

        // 2. PHÂN CẤP ƯU TIÊN DI CHUYỂN (STATE MACHINE)
        Vector2 actualTarget = Vector2.zero;
        float effectMult = effectController != null ? effectController.GetSpeedMultiplier() : 1f;
        float maxSpeedWithNoise = currentMoveSpeed.Value * personalSpeedMultiplier * effectMult;
        float targetSpeed = maxSpeedWithNoise;
        
        bool shouldMove = false;
        bool applySlowingRadius = false;

        if (isPanicking)
        {
            // Trạng thái 1: Hoảng loạn chạy loạn xạ
            actualTarget = panicTargetPos;
            targetSpeed = maxSpeedWithNoise * panicSpeedMultiplier;
            shouldMove = true;

            // Nếu chạy gần tới điểm loạn xạ hiện tại, đổi điểm loạn xạ mới lập tức để tạo cảm giác "giãy giụa"
            if (Vector2.Distance(transform.position, panicTargetPos) < 0.4f)
            {
                PickRandomPanicTarget();
            }
        }
        else if (isForceReturning)
        {
            // Trạng thái 2: Bị đẩy văng ra ngoài -> Bỏ qua offset, đâm thẳng trực diện về tâm bầy cừu
            actualTarget = flockCenter;
            targetSpeed = maxSpeedWithNoise * returnSpeedMultiplier;
            shouldMove = true;

            // Khi đã chui sâu lại vào vùng an toàn của bầy (nằm trong 60% bán kính bầy)
            if (Vector2.Distance(transform.position, flockCenter) < flockRadius * 0.6f)
            {
                isForceReturning = false;
                PickNewOffset(); // Trở lại trạng thái phân tán bình thường
            }
        }
        else if (hasTarget)
        {
            // Trạng thái 3: Di chuyển tụ họp quanh bầy bình thường
            actualTarget = flockCenter + localOffset;
            shouldMove = true;
            applySlowingRadius = true; // Chỉ bầy bình thường mới cần giảm tốc khi đến gần
        }

        // 3. THỰC THI DI CHUYỂN RIGIDBODY2D
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
                // Nếu là trạng thái bình thường mà tới đích rồi thì đứng im
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
            // Không có mục tiêu nào -> Giảm tốc về 0
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
    // SỬA ĐỔI LOGIC: BÁO CÁO LÊN FLOCKMANAGER KHI TRÚNG ĐÒN
    // ==========================================
    public override void TakeDamage(int damage, NetworkEntity source)
    {
        int healthBefore = currentHealth.Value; 

        base.TakeDamage(damage, source);

        if (IsServer && currentHealth.Value < healthBefore)
        {
            // TÍNH NĂNG MỚI: Báo cáo bầy trưởng để kích hoạt hoảng loạn diện rộng
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
