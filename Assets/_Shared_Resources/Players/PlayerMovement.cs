using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    
    private Rigidbody2D rb;
    private PlayerController controller;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // Biến mạng đồng bộ Input di chuyển từ Server xuống các Client
    private NetworkVariable<Vector2> netMoveInput = new NetworkVariable<Vector2>(
        Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        // Tìm ngược lên GameObject gốc Atto để lấy các thành phần cốt lõi
        rb = GetComponentInParent<Rigidbody2D>();
        controller = GetComponentInParent<PlayerController>();
        
        // Tìm sang anh em hoặc chính nó để lấy hình ảnh
        spriteRenderer = GetComponentInParent<PlayerController>().GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInParent<PlayerController>().GetComponentInChildren<Animator>();
    }
    public override void OnNetworkSpawn()
    {
        // Đăng ký nhận Input từ Controller tổng
        if (IsOwner)
        {
            controller.OnMoveInputChanged += SendInputToServer;
        }
    }

    void Update()
    {
        // Đồng bộ hiệu ứng hình ảnh (Chạy được ở cả máy Owner và các máy Client khác)
        if (animator != null) animator.SetBool("IsMoving", netMoveInput.Value != Vector2.zero);
        
        if (spriteRenderer != null)
        {
            if (netMoveInput.Value.x > 0) spriteRenderer.flipX = false;
            else if (netMoveInput.Value.x < 0) spriteRenderer.flipX = true;
        }
    }

    void FixedUpdate()
    {
        // Chỉ Server mới được tính toán vật lý di chuyển
        if (!IsServer) return;
        rb.linearVelocity = netMoveInput.Value * moveSpeed;
    }

    private void SendInputToServer(Vector2 moveInput)
    {
        // Gửi tọa độ nút bấm lên Server thông qua RPC
        SetMoveInputServerRpc(moveInput);
    }

    [ServerRpc]
    private void SetMoveInputServerRpc(Vector2 input)
    {
        netMoveInput.Value = input;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && controller != null)
        {
            controller.OnMoveInputChanged -= SendInputToServer;
        }
    }
}