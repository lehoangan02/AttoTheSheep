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

    private float personalSpeedMultiplier = 1f; 
    private Color originalColor;
    private bool isMovementLocked = false; 

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private FlockManager myManager;

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
    }

    public void Initialize(FlockManager manager)
    {
        myManager = manager;
    }

    public override void OnNetworkSpawn()
    {
        // QUAN TRỌNG: Gọi base để khởi tạo Máu, Năng lượng và Tốc độ từ class cha
        base.OnNetworkSpawn(); 
        
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    // GHI ĐÈ HÀM CHẾT TỪ NETWORK ENTITY
    protected override void Die()
    {
        // Xóa cừu khỏi bầy trước khi nó biến mất
        if (myManager != null) 
        {
            myManager.RemoveLamb(this);
        }
        
        // Gọi base để thực hiện logic hủy object mặc định (NetworkObject.Despawn)
        base.Die(); 
    }

    public void SetFlockData(Vector2 center, float radius)
    {
        flockCenter = center;
        flockRadius = radius;
        hasTarget = true;
        PickNewOffset(); 
    }

    private void PickNewOffset()
    {
        localOffset = Random.insideUnitCircle * flockRadius;
    }

    void FixedUpdate()
    {
        if (!IsSpawned || !IsServer || isMovementLocked) return; 

        if (!hasTarget) 
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, accelerationRate * Time.fixedDeltaTime);
            UpdateAnimationLocal(rb.linearVelocity.magnitude > 0.1f);
            return;
        }

        Vector2 actualTarget = flockCenter + localOffset;
        float distToTarget = Vector2.Distance(transform.position, actualTarget);
        
        if (distToTarget > stoppingDistance)
        {
            Vector2 direction = (actualTarget - (Vector2)transform.position).normalized;
            
            // Dùng trực tiếp biến currentMoveSpeed của class cha NetworkEntity
            float maxSpeedWithNoise = currentMoveSpeed.Value * personalSpeedMultiplier;
            float targetSpeed = maxSpeedWithNoise;
            
            if (distToTarget < slowingRadius)
            {
                targetSpeed = maxSpeedWithNoise * (distToTarget / slowingRadius);
            }

            Vector2 desiredVelocity = direction * targetSpeed;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, accelerationRate * Time.fixedDeltaTime);
            UpdateAnimationLocal(true);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            hasTarget = false; 
            UpdateAnimationLocal(false);
        }
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
    // GHI ĐÈ HÀM NHẬN SÁT THƯƠNG (TỪ NETWORK ENTITY)
    // ==========================================
    public override void TakeDamage(int damage, NetworkEntity source)
    {
        int healthBefore = currentHealth.Value; 

        // 1. Gọi logic trừ máu mặc định của class cha
        base.TakeDamage(damage, source);

        // 2. Thêm hiệu ứng Knockback (Đẩy lùi) nếu bị mất máu và có người tấn công
        if (IsServer && currentHealth.Value < healthBefore && source != null)
        {
            // Lấy vị trí của kẻ tấn công (source) để tính hướng đẩy lùi
            Vector2 knockbackDirection = ((Vector2)transform.position - (Vector2)source.transform.position).normalized;
            Vector2 appliedForce = knockbackDirection * knockbackForce;

            ApplyKnockbackClientRpc(appliedForce);
        }
    }

    // ==========================================
    // TỰ ĐỘNG ÁM ĐỎ KHI MÁU THAY ĐỔI
    // ==========================================
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

    // ==========================================
    // CƠ CHẾ ĐẨY LÙI (KNOCKBACK)
    // ==========================================
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
    }
}