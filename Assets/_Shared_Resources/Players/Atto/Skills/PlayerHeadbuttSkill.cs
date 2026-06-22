using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerHeadbuttSkill : BaseSkillComponent
{
    [Header("References - Set in Inspector")]
    [SerializeField] private Animator animator;          
    [SerializeField] private LayerMask enemyLayer;       
    
    // THÊM BIẾN NÀY ĐỂ KÉO THẢ SPRITE CỦA PLAYER
    [SerializeField] private SpriteRenderer playerSprite; 
    
    [Header("Visual Effects")]
    // Kéo object Particle System có sẵn trên nhân vật vào đây
    [SerializeField] private ParticleSystem impactParticle; 

    private HeadbuttSkillData currentHeadbuttData;

    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        if (data is HeadbuttSkillData headbuttData && controller != null)
        {
            currentHeadbuttData = headbuttData;
            StartCoroutine(HeadbuttRoutine(headbuttData, controller));
        }
    }

    public override void ClientPlayVisual(SkillData data)
    {
        base.ClientPlayVisual(data); // Gọi code của class cha để tự động phát SFX nếu có
        Animator anim = animator;
        if (anim == null) anim = GetComponentInParent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (anim != null)
        {
            anim.SetTrigger("Headbutt");
        }
    }

    private IEnumerator HeadbuttRoutine(HeadbuttSkillData data, PlayerController controller)
    {
        PlayerMovement movement = controller.GetComponentInChildren<PlayerMovement>();
        if (movement == null) movement = controller.GetComponentInParent<PlayerMovement>();
        if (movement != null) movement.isMovementLocked = true;

        yield return new WaitForSeconds(data.attackDelay);

        // --- SỬ DỤNG BIẾN playerSprite ĐỂ XÁC ĐỊNH HƯỚNG ---
        Vector2 facingDir = Vector2.right;
        if (playerSprite != null && playerSprite.flipX) 
        {
            facingDir = Vector2.left;
        }

        Vector2 hitCenter = (Vector2)controller.transform.position + new Vector2(data.hitboxOffset.x * facingDir.x, data.hitboxOffset.y);

        LayerMask layer = enemyLayer != 0 ? enemyLayer : data.enemyLayer;
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, data.hitRadius, layer);

        HashSet<Collider2D> damagedEnemies = new HashSet<Collider2D>();
        bool hasPlayedParticle = false; // Biến kiểm tra để chỉ nổ hạt 1 lần mỗi cú húc

        foreach (var hit in hits)
        {
            if (damagedEnemies.Add(hit))
            {
                NetworkEntity enemyEntity = hit.GetComponent<NetworkEntity>();
                if (enemyEntity != null) 
                {
                    enemyEntity.TakeDamage((int)data.damage);

                    // Đẩy lùi
                    enemyEntity.ApplyKnockback(facingDir * data.knockbackForce, 0.2f);
                }

                // TẠO HIỆU ỨNG TÓE LỬA TỪ OBJECT CÓ SẴN
                if (impactParticle != null && !hasPlayedParticle)
                {
                    // Lấy điểm tiếp xúc gần nhất
                    Vector3 impactPos = hit.ClosestPoint(hitCenter);

                    // Dời object particle đến đúng vị trí chạm
                    impactParticle.transform.position = impactPos;

                    // XOAY PARTICLE THEO HƯỚNG NHÂN VẬT
                    // Nếu nhân vật quay trái (facingDir.x < 0), xoay Particle 180 độ trục Y. Nếu quay phải thì giữ nguyên 0 độ.
                    float yRotation = facingDir.x < 0 ? 180f : 0f;
                    impactParticle.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

                    // Bật nổ tia lửa
                    impactParticle.Play();

                    // Đánh dấu là đã nổ để không gọi lại Play() nếu trúng thêm quái khác cùng lúc
                    hasPlayedParticle = true;
                }
                
                ClientPlayHitEffect(data, hit.transform.position);
            }
        }

        yield return new WaitForSeconds(data.recoveryTime);

        if (movement != null) movement.isMovementLocked = false;
        currentHeadbuttData = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (currentHeadbuttData != null)
        {
            Gizmos.color = Color.red;
            
            // Gizmos cũng dùng biến playerSprite để vẽ hitbox cho chuẩn
            float facingX = 1f;
            if (playerSprite != null && playerSprite.flipX) facingX = -1f;
            
            Vector2 hitCenter = (Vector2)transform.position + new Vector2(currentHeadbuttData.hitboxOffset.x * facingX, currentHeadbuttData.hitboxOffset.y);
            Gizmos.DrawWireSphere(hitCenter, currentHeadbuttData.hitRadius);
        }
    }
}