using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine; 
using System.Collections; 

public class PlayerEntity : NetworkEntity
{
    [Header("Damage Feedback (Hiệu ứng trúng đòn)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color damageColor = Color.red; 
    [SerializeField] private float flashDuration = 0.2f;    
    [SerializeField] private float knockbackForce = 15f; // Tăng lực lên chút để dễ thấy    
    [SerializeField] private float knockbackDuration = 0.15f; 

    private Color originalColor;
    private Rigidbody2D rb;
    private PlayerMovement playerMovement; // Khai báo tham chiếu đến PlayerMovement

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        // Tìm PlayerMovement nằm ở Object con (giống cách bạn setup GetComponentInParent bên PlayerMovement)
        playerMovement = GetComponentInChildren<PlayerMovement>();
        
        if (spriteRenderer != null) 
        {
            originalColor = spriteRenderer.color;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner)
        {
            Debug.Log("Player Entity has been spawned!");
            SetupVirtualCamera();
        }

        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void SetupVirtualCamera()
    {
        CinemachineCamera vCam = FindAnyObjectByType<CinemachineCamera>();

        if (vCam != null)
        {
            vCam.Follow = this.transform; 
            Debug.Log("🎥 [Camera] Đã setup Cinemachine focus vào Local Player!");
        }
        else
        {
            Debug.LogWarning("⚠️ [Camera] Không tìm thấy CinemachineCamera nào trong Scene!");
        }
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("Player has been defeated! Showing Game Over screen...");
    }

    // ==========================================
    // 1. HIỆU ỨNG ÁM ĐỎ KHI MẤT MÁU
    // ==========================================
    private void OnHealthChanged(int previousValue, int newValue)
    {
        if (newValue < previousValue)
        {
            if (spriteRenderer != null)
            {
                StopAllCoroutines(); 
                StartCoroutine(FlashRedRoutine());
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
    // 2. HIỆU ỨNG LÙI LẠI (KNOCKBACK) 
    // ==========================================
    public override void TakeDamage(int damage, NetworkEntity source)
    {
        int healthBefore = currentHealth.Value; 

        base.TakeDamage(damage, source);

        if (IsServer && currentHealth.Value < healthBefore && source != null)
        {
            Vector2 knockbackDirection = (transform.position - source.transform.position).normalized;
            Vector2 appliedForce = knockbackDirection * knockbackForce;

            ApplyPlayerKnockbackClientRpc(appliedForce);
        }
    }

    [ClientRpc]
    private void ApplyPlayerKnockbackClientRpc(Vector2 force)
    {
        if (rb != null)
        {
            StartCoroutine(PlayerKnockbackRoutine(force));
        }
    }

    private IEnumerator PlayerKnockbackRoutine(Vector2 force)
    {
        // 1. Kích hoạt cờ khóa di chuyển bên PlayerMovement để chặn FixedUpdate
        if (playerMovement != null) playerMovement.isMovementLocked = true;

        // 2. Ép vận tốc để đẩy lùi
        rb.linearVelocity = force;

        // 3. Đợi hết thời gian đẩy lùi
        yield return new WaitForSeconds(knockbackDuration);

        // 4. Dừng lại và trả lại quyền di chuyển cho người chơi
        rb.linearVelocity = Vector2.zero; 
        if (playerMovement != null) playerMovement.isMovementLocked = false;
    }
}