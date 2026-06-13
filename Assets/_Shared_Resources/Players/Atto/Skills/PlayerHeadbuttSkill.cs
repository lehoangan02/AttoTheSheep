using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerHeadbuttSkill : BaseSkillComponent
{
    [Header("References - Set in Inspector")]
    [SerializeField] private Animator animator;          // Gán Animator của Atto ở đây
    [SerializeField] private LayerMask enemyLayer;       // Gán layer Enemy ở đây

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
        // tự tìm Animator nếu chưa gán trong Inspector
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
        // 1. Khóa di chuyển
        PlayerMovement movement = controller.GetComponentInChildren<PlayerMovement>();
        if (movement == null) movement = controller.GetComponentInParent<PlayerMovement>();
        if (movement != null) movement.isMovementLocked = true;

        // 2. Chờ attackDelay để khớp animation
        yield return new WaitForSeconds(data.attackDelay);

        // 3. Xác định hướng mặt
        Vector2 facingDir = Vector2.right;
        SpriteRenderer sprite = controller.GetComponentInChildren<SpriteRenderer>();
        if (sprite != null && sprite.flipX) facingDir = Vector2.left;

        // 4. Tính vị trí hitbox
        Vector2 hitCenter = (Vector2)controller.transform.position + new Vector2(data.hitboxOffset.x * facingDir.x, data.hitboxOffset.y);

        // 5. Quét enemy (ưu tiên layer từ component, fallback sang data)
        LayerMask layer = enemyLayer != 0 ? enemyLayer : data.enemyLayer;
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, data.hitRadius, layer);

        HashSet<Collider2D> damagedEnemies = new HashSet<Collider2D>();
        foreach (var hit in hits)
        {
            if (damagedEnemies.Add(hit))
            {
                NetworkEntity enemyEntity = hit.GetComponent<NetworkEntity>();
                if (enemyEntity != null) enemyEntity.TakeDamage((int)data.damage);

                // Đẩy lùi nhẹ
                Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                if (enemyRb == null) enemyRb = hit.GetComponentInParent<Rigidbody2D>();
                if (enemyRb != null && data.knockbackForce > 0)
                {
                    enemyRb.linearVelocity = facingDir * data.knockbackForce;
                }
            }
        }

        // 6. Chờ hồi đòn
        yield return new WaitForSeconds(data.recoveryTime);

        // 7. Mở khóa di chuyển
        if (movement != null) movement.isMovementLocked = false;
        currentHeadbuttData = null;
    }

    // Vẽ hitbox trong Editor
    private void OnDrawGizmosSelected()
    {
        if (currentHeadbuttData != null)
        {
            Gizmos.color = Color.red;
            Vector2 hitCenter = (Vector2)transform.position + currentHeadbuttData.hitboxOffset;
            Gizmos.DrawWireSphere(hitCenter, currentHeadbuttData.hitRadius);
        }
    }
}