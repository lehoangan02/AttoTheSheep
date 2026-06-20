using UnityEngine;

// Dòng này cực kỳ quan trọng: Bắt script này chạy SAU TẤT CẢ các script khác (mặc định là 0)
[DefaultExecutionOrder(100)]
public class SlowDebuff : MonoBehaviour
{
    public float slowMultiplier = 1f;
    
    private Rigidbody2D rb;
    private Animator anim;
    private float originalAnimSpeed = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // (Tùy chọn) Tìm Animator để làm chậm luôn cả hoạt ảnh bước đi cho chân thực
        anim = GetComponentInChildren<Animator>();
        if (anim != null) 
        {
            originalAnimSpeed = anim.speed;
        }
    }

    private void Start()
    {
        if (anim != null) anim.speed = originalAnimSpeed * slowMultiplier;
    }

    private void FixedUpdate()
    {
        // Ngay sau khi EnemyMotor/PlayerMovement gán vận tốc, ta bóp nhỏ nó lại
        if (rb != null && slowMultiplier < 1f)
        {
            rb.linearVelocity *= slowMultiplier;
        }
    }

    private void OnDestroy()
    {
        // Khi vũng nước bị hủy hoặc đối tượng bước ra ngoài, trả lại tốc độ hoạt ảnh
        if (anim != null) anim.speed = originalAnimSpeed;
    }
}