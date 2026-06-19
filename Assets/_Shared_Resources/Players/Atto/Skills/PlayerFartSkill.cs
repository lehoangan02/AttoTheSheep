using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerFartSkill : BaseSkillComponent
{
    [Header("References")]
    [SerializeField] private ParticleSystem fartGasParticle; 
    [SerializeField] private Collider2D playerCollider; // Collider chính của Player (để chuyển trigger khi dash)

    private FartSkillData currentFartData;
    private PlayerController fartController;

    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        // Ép kiểu Data an toàn
        if (data is FartSkillData fartData && controller != null)
        {
            currentFartData = fartData;
            fartController = controller;
            StartCoroutine(DashAndDamageRoutine(fartData, controller));
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

        // 2. Xác định hướng lướt (ưu tiên hướng đang chạy)
        Vector2 dashDir = rb.linearVelocity.normalized;
        if (dashDir == Vector2.zero) 
        {
            SpriteRenderer sprite = controller.GetComponentInChildren<SpriteRenderer>();
            dashDir = (sprite != null && sprite.flipX) ? Vector2.left : Vector2.right;
        }

        Debug.Log($"🚀 [LOGIC LOG] Đang lướt về hướng {dashDir} với lực {data.dashForce} trong {data.dashDuration} giây.");

        // 3. KHÓA DI CHUYỂN TỪ NGƯỜI CHƠI + CHUYỂN COLLIDER THÀNH TRIGGER
        if (movement != null) movement.isMovementLocked = true;
        if (playerCollider != null) playerCollider.isTrigger = true;

        float elapsed = 0f;
        // HashSet đảm bảo mỗi quái vật chỉ nhận sát thương/knockback MỘT LẦN duy nhất trong 1 lần lướt
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
                    Debug.Log($"💥 [OVERLAP] Tông trúng mục tiêu: {hit.gameObject.name}! Gây {data.damage} sát thương.");
                    
                    // Trừ máu quái
                    NetworkEntity enemyEntity = hit.GetComponent<NetworkEntity>();
                    if (enemyEntity != null)
                    {
                        enemyEntity.TakeDamage((int)data.damage);

                        // Knockback: freeze brain + apply force for 0.3s via ApplyKnockback
                        Vector2 knockbackDir = ((Vector2)hit.transform.position - (Vector2)controller.transform.position).normalized;
                        enemyEntity.ApplyKnockback(knockbackDir * data.knockupForce, 0.3f);
                    }
                }
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate(); // Chờ đến khung hình vật lý tiếp theo
        }

        // 4. Lướt xong, hãm phanh dừng lại
        rb.linearVelocity = Vector2.zero;
        Debug.Log("🛑 [LOGIC LOG] Lướt xong, đã dừng lại.");

        // 5. MỞ KHÓA DI CHUYỂN + TRẢ COLLIDER VỀ TRẠNG THÁI BÌNH THƯỜNG
        if (movement != null) movement.isMovementLocked = false;
        if (playerCollider != null) playerCollider.isTrigger = false;

        currentFartData = null;
        fartController = null;
    }

    // Vẽ vòng tròn vàng trong Editor để bạn dễ căn chỉnh vùng sát thương trúng
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f); // Bán kính hiển thị nháp
    }
}