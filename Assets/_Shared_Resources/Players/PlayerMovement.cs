using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Biến này giờ đây sẽ được Server cập nhật và đồng bộ xuống cho các Client khác xem cùng
    private NetworkVariable<Vector2> netMoveInput = new NetworkVariable<Vector2>(
        Vector2.zero, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server // Chỉ Server mới có quyền thay đổi biến này
    );

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Đồng bộ hiệu ứng hoạt ảnh và lật hình dựa trên dữ liệu mạng đã được Server duyệt
        animator.SetBool("IsMoving", netMoveInput.Value != Vector2.zero);

        if (netMoveInput.Value.x > 0) spriteRenderer.flipX = false;
        else if (netMoveInput.Value.x < 0) spriteRenderer.flipX = true;
    }

    void FixedUpdate()
    {
        // CHỈ SERVER mới được phép tính toán vật lý và di chuyển nhân vật
        if (!IsServer) return;

        rb.linearVelocity = netMoveInput.Value * moveSpeed;
    }

    // Hàm nhận Input từ hệ thống Input System của Unity
    public void OnMove(InputValue value)
    {
        // Chỉ Owner (người chơi điều khiển nhân vật này) mới được gửi Input của họ
        if (!IsOwner) return;

        Vector2 input = value.Get<Vector2>();
        
        // Gửi tọa độ nút bấm lên Server
        SendInputServerRpc(input);
    }

    // RPC này giúp Client gửi dữ liệu lên Server một cách an toàn
    [ServerRpc]
    private void SendInputServerRpc(Vector2 input)
    {
        // Server nhận được và lưu vào NetworkVariable công khai
        netMoveInput.Value = input;
    }
}