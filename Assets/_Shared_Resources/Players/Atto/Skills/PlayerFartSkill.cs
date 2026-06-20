using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerFartSkill : BaseSkillComponent
{
    [Header("References")]
    [SerializeField] private ParticleSystem fartGasParticle; 
    [SerializeField] private Collider2D playerCollider; 
    
    // THÊM BIẾN NÀY ĐỂ KÉO THẢ TRONG INSPECTOR
    [SerializeField] private SpriteRenderer playerSprite; 

    private FartSkillData currentFartData;
    private PlayerController fartController;

    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        // Code cũ của bạn giữ nguyên...
        if (data is FartSkillData fartData && controller != null)
        {
            currentFartData = fartData;
            fartController = controller;
            StartCoroutine(DashAndDamageRoutine(fartData, controller));
        }
    }

    public override void ClientPlayVisual(SkillData data)
    {
        if (fartGasParticle != null) 
        {
            // 1. Giữ nguyên Transform gốc của Particle, không bẻ nó nữa để tránh lỗi mất Stretch
            fartGasParticle.transform.localRotation = Quaternion.identity;

            if (playerSprite != null)
            {
                // Truy cập trực tiếp vào module Shape bằng Code
                var shapeModule = fartGasParticle.shape;

                Debug.Log($"🌬️ [VFX LOG] Trạng thái nhân vật lật (flipX) = {playerSprite.flipX}");

                if (playerSprite.flipX) 
                {
                    // Nhân vật nhìn TRÁI -> Dash TRÁI -> Gió thổi sang PHẢI
                    // Xoay góc trục Y của module Shape thành -90
                    shapeModule.rotation = new Vector3(0, 90, 0); 
                }
                else 
                {
                    // Nhân vật nhìn PHẢI -> Dash PHẢI -> Gió thổi sang TRÁI
                    // Xoay góc trục Y của module Shape thành 90
                    shapeModule.rotation = new Vector3(0, -90, 0); 
                }
            }
            else
            {
                Debug.LogWarning("⚠️ [VFX LỖI] Bạn chưa kéo thả SpriteRenderer vào ô Player Sprite!");
            }

            fartGasParticle.Play();
        }
    }

    private IEnumerator DashAndDamageRoutine(FartSkillData data, PlayerController controller)
    {
        // Code Coroutine cũ của bạn giữ nguyên...
        Rigidbody2D rb = controller.GetComponent<Rigidbody2D>();
        
        PlayerMovement movement = controller.GetComponentInChildren<PlayerMovement>();
        if (movement == null) movement = controller.GetComponentInParent<PlayerMovement>();

        if (rb == null) 
        {
            Debug.LogError("❌ [LOGIC LỖI] Player không có Rigidbody2D, không thể lướt!");
            yield break;
        }

        Vector2 dashDir = rb.linearVelocity.normalized;
        if (dashDir == Vector2.zero) 
        {
            SpriteRenderer sprite = controller.GetComponentInChildren<SpriteRenderer>();
            dashDir = (sprite != null && sprite.flipX) ? Vector2.left : Vector2.right;
        }

        Debug.Log($"🚀 [LOGIC LOG] Đang lướt về hướng {dashDir} với lực {data.dashForce} trong {data.dashDuration} giây.");

        if (movement != null) movement.isMovementLocked = true;
        if (playerCollider != null) playerCollider.isTrigger = true;

        float elapsed = 0f;
        HashSet<Collider2D> damagedEnemies = new HashSet<Collider2D>(); 

        while (elapsed < data.dashDuration)
        {
            rb.linearVelocity = dashDir * data.dashForce;
            
            Collider2D[] hits = Physics2D.OverlapCircleAll(controller.transform.position, data.hitRadius, data.enemyLayer);
            foreach (var hit in hits)
            {
                if (damagedEnemies.Add(hit)) 
                {
                    Debug.Log($"💥 [OVERLAP] Tông trúng mục tiêu: {hit.gameObject.name}! Gây {data.damage} sát thương.");
                    
                    NetworkEntity enemyEntity = hit.GetComponent<NetworkEntity>();
                    if (enemyEntity != null)
                    {
                        enemyEntity.TakeDamage((int)data.damage);

                        Vector2 knockbackDir = ((Vector2)hit.transform.position - (Vector2)controller.transform.position).normalized;
                        enemyEntity.ApplyKnockback(knockbackDir * data.knockupForce, 0.3f);
                    }
                }
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        Debug.Log("🛑 [LOGIC LOG] Lướt xong, đã dừng lại.");

        if (movement != null) movement.isMovementLocked = false;
        if (playerCollider != null) playerCollider.isTrigger = false;

        currentFartData = null;
        fartController = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f); 
    }
}