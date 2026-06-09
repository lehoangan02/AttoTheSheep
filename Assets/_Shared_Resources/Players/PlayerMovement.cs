using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    private Rigidbody2D rb;
    private PlayerController controller;
    private NetworkEntity entity; // Kéo lõi chỉ số từ Cha
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Coroutine speedBoostRoutine;

    public bool isMovementLocked = false; 

    private NetworkVariable<Vector2> netMoveInput = new NetworkVariable<Vector2>(
        Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    
    private Vector2 localMoveInput;

    void Awake()
    {
        // Tìm các thành phần cốt lõi ở Object Cha (Atto)
        rb = GetComponentInParent<Rigidbody2D>();
        controller = GetComponentInParent<PlayerController>();
        entity = GetComponentInParent<NetworkEntity>();

        // Tìm kiếm hình ảnh và hoạt ảnh trong toàn bộ các Object con của Atto
        if (transform.parent != null)
        {
            spriteRenderer = transform.parent.GetComponentInChildren<SpriteRenderer>();
            animator = transform.parent.GetComponentInChildren<Animator>();
        }

        // Bẫy lỗi tự động để check nhanh trong Inspector
        if (rb == null) Debug.LogError($"[{gameObject.name}]: Thiếu Rigidbody2D trên Object Cha!");
        if (entity == null) Debug.LogError($"[{gameObject.name}]: Thiếu NetworkEntity trên Object Cha!");
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner && controller != null)
        {
            controller.OnMoveInputChanged += SendInputToServer;
        }
    }

    void Update()
    {
        // Nếu là Owner thì dùng input nội bộ cho mượt, người khác nhìn vào thì dùng input mạng
        Vector2 currentInput = IsOwner ? localMoveInput : netMoveInput.Value;
        if (animator != null) animator.SetBool("IsMoving", currentInput != Vector2.zero);
        
        if (spriteRenderer != null)
        {
            if (currentInput.x > 0) spriteRenderer.flipX = false;
            else if (currentInput.x < 0) spriteRenderer.flipX = true;
        }
    }

    void FixedUpdate()
    {
        // Đổi IsServer thành IsOwner để khớp với Authority Mode: Owner trên NetworkTransform
        if (!IsOwner || isMovementLocked || rb == null) return;

        // Lấy tốc độ từ NetworkEntity của cha, nếu cha chưa có thì dùng tạm tốc độ mặc định là 5
        float currentSpeed = (entity != null) ? entity.currentMoveSpeed.Value : 5f;
        
        rb.linearVelocity = localMoveInput * currentSpeed;
    }

    private void SendInputToServer(Vector2 moveInput)
    {
        localMoveInput = moveInput; // Lưu ngay lại trên máy Client để di chuyển tức thời
        if (IsSpawned) SetMoveInputServerRpc(moveInput);
    }

    [ServerRpc]
    private void SetMoveInputServerRpc(Vector2 input)
    {
        netMoveInput.Value = input;
    }

    public void ApplyTemporarySpeedMultiplier(float multiplier, float duration, float accelerationDuration = 0f)
    {
        if (!IsServer || entity == null) return;

        if (speedBoostRoutine != null)
        {
            StopCoroutine(speedBoostRoutine);
            entity.currentMoveSpeed.Value = entity.BaseMoveSpeed;
        }

        speedBoostRoutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration, accelerationDuration));
    }

    private System.Collections.IEnumerator SpeedBoostRoutine(float multiplier, float duration, float accelerationDuration)
    {
        float baseSpeed = entity.BaseMoveSpeed;
        float boostedSpeed = baseSpeed * multiplier;
        float effectiveAccelerationDuration = Mathf.Min(accelerationDuration, duration);

        if (effectiveAccelerationDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < effectiveAccelerationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / effectiveAccelerationDuration);
                entity.currentMoveSpeed.Value = Mathf.Lerp(baseSpeed, boostedSpeed, progress);
                yield return null;
            }
        }

        entity.currentMoveSpeed.Value = boostedSpeed;
        yield return new WaitForSeconds(Mathf.Max(0f, duration - effectiveAccelerationDuration));

        entity.currentMoveSpeed.Value = baseSpeed;
        speedBoostRoutine = null;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && controller != null)
        {
            controller.OnMoveInputChanged -= SendInputToServer;
        }

        if (speedBoostRoutine != null) StopCoroutine(speedBoostRoutine);
        if (IsServer && entity != null) entity.currentMoveSpeed.Value = entity.BaseMoveSpeed;
    }
}