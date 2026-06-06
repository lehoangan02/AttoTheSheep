using UnityEngine;

public class LambAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float stoppingDistance = 0.1f; 

    [Tooltip("Khoảng cách bắt đầu rà phanh (chậm dần trước khi dừng)")]
    [SerializeField] private float slowingRadius = 2f; 
    
    [Tooltip("Độ trễ tăng/giảm tốc (tạo cảm giác ì lúc mới chạy - số càng nhỏ càng ì)")]
    [SerializeField] private float accelerationRate = 5f; 

    // --- THÊM VÀO: Cấu hình Noise cho tốc độ ---
    [Header("Noise Settings")]
    [Tooltip("Độ lệch tốc độ tối đa (Ví dụ: nếu là 0.3, tốc độ thực tế sẽ biến thiên trong khoảng [-30%, +30%] của moveSpeed)")]
    [Range(0f, 0.9f)]
    [SerializeField] private float speedNoiseRange = 0.25f;

    private float personalSpeedMultiplier = 1f; // Hệ số tốc độ riêng của từng con cừu
    // -------------------------------------------

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
    }

    // --- THÊM VÀO: Khởi tạo Noise ngẫu nhiên khi cừu được sinh ra ---
    void Start()
    {
        // Chọn ngẫu nhiên một hệ số nằm trong khoảng [1 - noise, 1 + noise]
        // Ví dụ speedNoiseRange = 0.2, thì multiplier sẽ từ 0.8 đến 1.2
        personalSpeedMultiplier = Random.Range(1f - speedNoiseRange, 1f + speedNoiseRange);
    }
    // ----------------------------------------------------------------

    public void Initialize(FlockManager manager)
    {
        myManager = manager;
    }

    public void SetFlockData(Vector2 center, float radius)
    {
        flockCenter = center;
        flockRadius = radius;
        hasTarget = true;
        
        // Chỉ bốc số vị trí MỚI khi có lệnh Click chuột
        PickNewOffset(); 
    }

    private void PickNewOffset()
    {
        // Chọn một tọa độ ngẫu nhiên LUÔN NẰM TRONG hình tròn của bầy
        localOffset = Random.insideUnitCircle * flockRadius;
    }

    void FixedUpdate()
    {
        if (!hasTarget) 
        {
            // Khi không có lệnh, cho cừu phanh từ từ lại thay vì khựng ngang lập tức
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, accelerationRate * Time.fixedDeltaTime);
            UpdateAnimation(rb.linearVelocity.magnitude > 0.1f);
            return;
        }

        Vector2 actualTarget = flockCenter + localOffset;
        float distToTarget = Vector2.Distance(transform.position, actualTarget);
        
        if (distToTarget > stoppingDistance)
        {
            // Hướng đi tới đích
            Vector2 direction = (actualTarget - (Vector2)transform.position).normalized;
            
            // --- CẬP NHẬT: Áp dụng hệ số Noise vào tốc độ tối đa ---
            float maxSpeedWithNoise = moveSpeed * personalSpeedMultiplier;
            float targetSpeed = maxSpeedWithNoise;
            
            // 1. Nếu đi vào vùng rà phanh -> Vận tốc mong muốn giảm dần theo khoảng cách (Ease-out)
            if (distToTarget < slowingRadius)
            {
                targetSpeed = maxSpeedWithNoise * (distToTarget / slowingRadius);
            }

            Vector2 desiredVelocity = direction * targetSpeed;

            // 2. Dùng Lerp để chuyển đổi mượt từ vận tốc hiện tại sang vận tốc mong muốn (Ease-in lúc khởi hành)
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, accelerationRate * Time.fixedDeltaTime);
            // ------------------------------------

            UpdateAnimation(true);
        }
        else
        {
            // ĐÃ ĐẾN NƠI: Dừng lại hoàn toàn, tắt cờ hasTarget để chờ lệnh click tiếp theo
            rb.linearVelocity = Vector2.zero;
            hasTarget = false; 
            UpdateAnimation(false); // Chuyển về trạng thái Idle đứng im
        }
    }

    private void UpdateAnimation(bool isMoving)
    {
        if (animator != null) animator.SetBool("IsMoving", isMoving);

        if (spriteRenderer != null)
        {
            // Lật mặt dựa trên vận tốc thay vì hướng đi, giúp cừu nhìn tự nhiên hơn khi phanh
            if (rb.linearVelocity.x > 0.01f) spriteRenderer.flipX = false;
            else if (rb.linearVelocity.x < -0.01f) spriteRenderer.flipX = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (myManager != null) myManager.RemoveLamb(this);
            gameObject.SetActive(false); 
        }
    }
}