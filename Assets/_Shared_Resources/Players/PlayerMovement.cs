using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    private Rigidbody2D rb;
    private PlayerController controller;
    private NetworkEntity entity; // Get core stats from Parent
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private PlayerAudio playerAudio;
    private Coroutine speedBoostRoutine;

    public bool isMovementLocked = false; 

    private NetworkVariable<Vector2> netMoveInput = new NetworkVariable<Vector2>(
        Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    
    private Vector2 localMoveInput;

    void Awake()
    {
        // Find core components on the Parent Object (Atto)
        rb = GetComponentInParent<Rigidbody2D>();
        controller = GetComponentInParent<PlayerController>();
        entity = GetComponentInParent<NetworkEntity>();
        playerAudio = GetComponentInParent<PlayerAudio>();
        if (playerAudio == null && transform.parent != null)
            playerAudio = transform.parent.gameObject.AddComponent<PlayerAudio>();

        // Search for sprite and animator in all child Objects of Atto
        if (transform.parent != null)
        {
            spriteRenderer = transform.parent.GetComponentInChildren<SpriteRenderer>();
            animator = transform.parent.GetComponentInChildren<Animator>();
        }

        // Automatic error trapping for quick Inspector checks
        if (rb == null) Debug.LogError($"[{gameObject.name}]: Missing Rigidbody2D on Parent Object!");
        if (entity == null) Debug.LogError($"[{gameObject.name}]: Missing NetworkEntity on Parent Object!");
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
        // Use local input for smoothness if Owner, others use network input
        Vector2 currentInput = IsOwner ? localMoveInput : netMoveInput.Value;
        bool isMoving = currentInput.sqrMagnitude > 0.01f && !isMovementLocked;
        if (animator != null) animator.SetBool("IsMoving", isMoving);
        
        if (spriteRenderer != null)
        {
            if (currentInput.x > 0 && !isMovementLocked) spriteRenderer.flipX = false;
            else if (currentInput.x < 0 && !isMovementLocked) spriteRenderer.flipX = true;
        }

        if (isMoving && !isMovementLocked && entity != null && entity.IsAlive
            && !(entity.effectController != null && entity.effectController.IsMovementLocked()))
        {
            playerAudio?.TryPlayFootstep(transform.position);
        }
    }

    void FixedUpdate()
    {
        // Change IsServer to IsOwner to match Authority Mode: Owner on NetworkTransform
        if (!IsOwner || isMovementLocked || rb == null) return;

        // Status effect movement lock (e.g. freeze, stun) — synced via NetworkVariables
        if (entity != null && entity.effectController != null && entity.effectController.IsMovementLocked())
            return;

        // Get speed from parent's NetworkEntity, apply status effect speed multiplier
        float effectMult = (entity != null && entity.effectController != null) ? entity.effectController.GetSpeedMultiplier() : 1f;
        float currentSpeed = (entity != null) ? entity.currentMoveSpeed.Value * effectMult : 5f;
        
        rb.linearVelocity = localMoveInput * currentSpeed;
    }

    private void SendInputToServer(Vector2 moveInput)
    {
        localMoveInput = moveInput; // Save locally on Client for immediate movement
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
