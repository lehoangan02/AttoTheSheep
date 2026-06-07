using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class PlayerFartSkill : BaseSkillComponent
{
    [SerializeField] private ParticleSystem fartGasParticle; 

    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        // Ép kiểu Data an toàn
        if (data is FartSkillData fartData && controller != null)
        {
            StartCoroutine(DashAndDamageRoutine(fartData, controller)); // Gọi Coroutine cũ của bạn
        }
    }

    public override void ClientPlayVisual(SkillData data)
    {
        if (fartGasParticle != null) fartGasParticle.Play();
    }

    private IEnumerator DashAndDamageRoutine(FartSkillData data, PlayerController controller)
    {
        Rigidbody2D rb = controller.GetComponent<Rigidbody2D>();
        
        // 1. Tìm Script di chuyển để chuẩn bị khóa
        PlayerMovement movement = controller.GetComponentInChildren<PlayerMovement>();
        if (movement == null) movement = controller.GetComponentInParent<PlayerMovement>();

        if (rb == null) 
        {
            Debug.LogError("❌ [LOGIC LỖI] Player không có Rigidbody2D, không thể lướt!");
            yield break;
        }

        // 2. Xác định hướng lướt (ưu tiên hướng đang chạy, nếu đứng im thì lướt theo hướng mặt quay về)
       // 2. Xác định hướng lướt (Ưu tiên hướng đang chạy)
        Vector2 dashDir = rb.linearVelocity.normalized;
        
        // Nếu nhân vật đang đứng im, check xem hình ảnh đang lật sang trái hay phải
        if (dashDir == Vector2.zero) 
        {
            SpriteRenderer sprite = controller.GetComponentInChildren<SpriteRenderer>();
            
            if (sprite != null && sprite.flipX == true) 
            {
                dashDir = Vector2.left;  // Đang lật hình sang trái -> Lướt trái
            }
            else 
            {
                dashDir = Vector2.right; // Mặc định -> Lướt phải
            }
        }

        Debug.Log($"🚀 [LOGIC LOG] Đang lướt về hướng {dashDir} với lực {data.dashForce} trong {data.dashDuration} giây.");

        // 3. KHÓA DI CHUYỂN TỪ NGƯỜI CHƠI
        if (movement != null) movement.isMovementLocked = true;

        float elapsed = 0f;
        // HashSet đảm bảo mỗi quái vật chỉ nhận sát thương MỘT LẦN duy nhất trong 1 lần lướt
        HashSet<Collider2D> damagedEnemies = new HashSet<Collider2D>(); 

        while (elapsed < data.dashDuration)
        {
            // Ép vận tốc lướt
            rb.linearVelocity = dashDir * data.dashForce;
            
            // Quét vùng sát thương xung quanh nhân vật
            Collider2D[] hits = Physics2D.OverlapCircleAll(controller.transform.position, data.hitRadius, data.enemyLayer);
            foreach (var hit in hits)
            {
                if (damagedEnemies.Add(hit)) 
                {
                    Debug.Log($"💥 [LOGIC LOG] Tông trúng mục tiêu: {hit.gameObject.name}! Gây {data.damage} sát thương.");
                    
                    // Trừ máu quái
                    NetworkHealth enemyHealth = hit.GetComponent<NetworkHealth>();
                    if (enemyHealth != null) enemyHealth.TakeDamage((int)data.damage);

                    // Hất tung quái
                    Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                    if (enemyRb != null)
                    {
                        enemyRb.linearVelocity = Vector2.zero; 
                        enemyRb.AddForce(Vector2.up * data.knockupForce, ForceMode2D.Impulse);
                    }
                }
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate(); // Chờ đến khung hình vật lý tiếp theo
        }

        // 4. Lướt xong, hãm phanh dừng lại
        rb.linearVelocity = Vector2.zero;
        Debug.Log("🛑 [LOGIC LOG] Lướt xong, đã dừng lại.");

        // 5. MỞ KHÓA DI CHUYỂN TRẢ QUYỀN ĐIỀU KHIỂN CHO BÀN PHÍM
        if (movement != null) movement.isMovementLocked = false;
    }

    // Vẽ vòng tròn vàng trong Editor để bạn dễ căn chỉnh vùng sát thương trúng
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f); // Bán kính hiển thị nháp
    }
}