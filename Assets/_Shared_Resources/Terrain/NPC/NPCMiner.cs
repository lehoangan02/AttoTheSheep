using UnityEngine;

public class MinerNPC : MonoBehaviour
{
    public enum MinerState 
    { 
        Mining,
        RunningToHouse,
        InHouse,
        RunningBackToMine
    }

    [Header("References")]
    public Transform goldMine;          // Kéo Transform của Mỏ Vàng vào đây để xác định hướng đào
    public Transform house;             // Kéo Transform của Nhà vào đây
    public Animator animator;           // Kéo Animator của NPC vào đây
    public Renderer npcRenderer;        // Kéo SpriteRenderer của NPC vào đây

    [Header("Stats")]
    public float moveSpeed = 3f;
    public float miningDuration = 5f;   // Thời gian đào tại mỏ
    public float houseStayDuration = 3f; // Thời gian ở trong nhà

    private MinerState currentState;
    private Vector3 initialPosition;    // Vị trí mỏ ban đầu
    private float currentTimer = 0f;

    void Start()
    {
        // Lưu vị trí xuất phát làm điểm đào cố định
        initialPosition = transform.position;
        
        // Bắt đầu bằng việc đào
        ChangeState(MinerState.Mining);
    }

    void Update()
    {
        switch (currentState)
        {
            case MinerState.Mining:
                // Ép NPC luôn quay mặt về hướng mỏ vàng khi đang đào
                FaceTarget(goldMine.position);

                currentTimer += Time.deltaTime;
                if (currentTimer >= miningDuration)
                {
                    ChangeState(MinerState.RunningToHouse);
                }
                break;

            case MinerState.RunningToHouse:
                MoveTo(house.position);
                if (Vector2.Distance(transform.position, house.position) < 0.1f)
                {
                    ChangeState(MinerState.InHouse);
                }
                break;

            case MinerState.InHouse:
                currentTimer += Time.deltaTime;
                if (currentTimer >= houseStayDuration)
                {
                    ChangeState(MinerState.RunningBackToMine);
                }
                break;

            case MinerState.RunningBackToMine:
                MoveTo(initialPosition);
                if (Vector2.Distance(transform.position, initialPosition) < 0.1f)
                {
                    ChangeState(MinerState.Mining);
                }
                break;
        }
    }

    // Hàm di chuyển tịnh tiến 2D và tự động lật mặt theo hướng đi
    void MoveTo(Vector3 destination)
    {
        Vector3 targetPosition = new Vector3(destination.x, destination.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Lật mặt theo hướng di chuyển
        FaceTarget(destination);
    }

    // Hàm bổ trợ xử lý lật mặt (Flip) trong Game 2D dựa trên tọa độ X
    void FaceTarget(Vector3 targetPosition)
    {
        // Nếu mục tiêu ở bên phải NPC -> nhìn sang phải (scale x = 1 hoặc giá trị dương ban đầu)
        if (targetPosition.x > transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        // Nếu mục tiêu ở bên trái NPC -> nhìn sang trái (scale x = số âm)
        else if (targetPosition.x < transform.position.x)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    void ChangeState(MinerState newState)
    {
        currentState = newState;
        currentTimer = 0f;

        // Xử lý ẩn/hiện hình ảnh khi vào/ra nhà
        if (npcRenderer != null)
        {
            npcRenderer.enabled = (currentState != MinerState.InHouse);
        }

        // Reset toàn bộ animation booleans
        animator.SetBool("IsMining", false);
        animator.SetBool("IsCarrying", false);
        animator.SetBool("IsRunning", false);

        // Kích hoạt animation mới khớp với trạng thái
        switch (currentState)
        {
            case MinerState.Mining:
                animator.SetBool("IsMining", true);
                break;
            case MinerState.RunningToHouse:
                animator.SetBool("IsCarrying", true);
                break;
            case MinerState.RunningBackToMine:
                animator.SetBool("IsRunning", true);
                break;
        }
    }
}