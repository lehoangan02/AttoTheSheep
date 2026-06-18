using UnityEngine;

public class WoodcutterNPC : MonoBehaviour
{
    public enum WoodcutterState 
    { 
        Chopping,           // Đang chặt cây (Animation "Woodcutter_Chop")
        RunningToHouse,     // Cầm gỗ chạy về nhà (Animation "Woodcutter_Run_With_Wood")
        InHouse,            // Đang cất gỗ trong nhà (Ẩn hình ảnh)
        RunningBackToTree   // Cầm rìu chạy lại vị trí cũ (Animation "Woodcutter_Run_With_Axe")
    }

    [Header("References")]
    public Transform targetTree;        // Kéo Transform của Cây vào đây để xác định hướng chặt
    public Transform house;             // Kéo Transform của Nhà vào đây
    public Animator animator;           // Kéo Animator của NPC vào đây
    public Renderer npcRenderer;        // Kéo SpriteRenderer của NPC vào đây

    [Header("Stats")]
    public float moveSpeed = 3f;
    public float chopDuration = 5f;     // Thời gian chặt cây
    public float houseStayDuration = 3f; // Thời gian ở trong nhà

    private WoodcutterState currentState;
    private Vector3 initialPosition;    // Vị trí gốc lúc bắt đầu (bên cạnh cây)
    private float currentTimer = 0f;

    void Start()
    {
        // Lưu vị trí xuất phát làm điểm chặt cây cố định
        initialPosition = transform.position;
        
        // Bắt đầu bằng việc chặt cây
        ChangeState(WoodcutterState.Chopping);
    }

    void Update()
    {
        switch (currentState)
        {
            case WoodcutterState.Chopping:
                // Ép Tiều phu luôn quay mặt về hướng cái cây khi đang chặt
                FaceTarget(targetTree.position);

                currentTimer += Time.deltaTime;
                if (currentTimer >= chopDuration)
                {
                    ChangeState(WoodcutterState.RunningToHouse);
                }
                break;

            case WoodcutterState.RunningToHouse:
                MoveTo(house.position);
                if (Vector2.Distance(transform.position, house.position) < 0.1f)
                {
                    ChangeState(WoodcutterState.InHouse);
                }
                break;

            case WoodcutterState.InHouse:
                currentTimer += Time.deltaTime;
                if (currentTimer >= houseStayDuration)
                {
                    ChangeState(WoodcutterState.RunningBackToTree);
                }
                break;

            case WoodcutterState.RunningBackToTree:
                MoveTo(initialPosition);
                if (Vector2.Distance(transform.position, initialPosition) < 0.1f)
                {
                    ChangeState(WoodcutterState.Chopping);
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
        if (targetPosition.x > transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (targetPosition.x < transform.position.x)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    void ChangeState(WoodcutterState newState)
    {
        currentState = newState;
        currentTimer = 0f;

        // Xử lý ẩn/hiện hình ảnh khi vào/ra nhà
        if (npcRenderer != null)
        {
            npcRenderer.enabled = (currentState != WoodcutterState.InHouse);
        }

        // Reset toàn bộ các biến Bool cũ trong Animator
        animator.SetBool("IsChopping", false);
        animator.SetBool("IsCarrying", false);
        animator.SetBool("IsRunning", false);

        // Kích hoạt biến Bool tương ứng với trạng thái mới
        switch (currentState)
        {
            case WoodcutterState.Chopping:
                animator.SetBool("IsChopping", true);
                break;
            case WoodcutterState.RunningToHouse:
                animator.SetBool("IsCarrying", true); // Chạy cầm gỗ
                break;
            case WoodcutterState.RunningBackToTree:
                animator.SetBool("IsRunning", true);  // Chạy cầm rìu
                break;
        }
    }
}