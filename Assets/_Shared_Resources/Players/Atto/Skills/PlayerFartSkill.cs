using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerFartSkill : BaseSkillComponent
{
    [Header("References")]
    [SerializeField] private List<TrailRenderer> dashTrails;
    [SerializeField] private Collider2D parentCollider; 
    [SerializeField] private Rigidbody2D parentRb;
    [SerializeField] private SpriteRenderer playerSprite; 

    private FartSkillData currentFartData;
    private PlayerController fartController;
    private bool isDashing = false; // Biến cờ để kiểm tra trạng thái đang lướt
    private List<Vector3> originalTrailLocalPositions;

    private void Start()
    {
        if (dashTrails == null || dashTrails.Count == 0)
        {
            dashTrails = new List<TrailRenderer>(GetComponentsInChildren<TrailRenderer>(true));
        }

        originalTrailLocalPositions = new List<Vector3>();
        foreach (var trail in dashTrails)
        {
            if (trail != null)
            {
                originalTrailLocalPositions.Add(trail.transform.localPosition);
                trail.emitting = false; // Disable emitting by default
            }
            else
            {
                originalTrailLocalPositions.Add(Vector3.zero);
            }
        }
    }

    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        if (data is FartSkillData fartData && controller != null)
        {
            currentFartData = fartData;
            fartController = controller;
            StartCoroutine(DashAndDamageRoutine(fartData, controller));
        }
    }

    public override void ClientPlayVisual(SkillData data)
    {        
        base.ClientPlayVisual(data);
        if (data is FartSkillData fartData)
        {
            StartCoroutine(PlayTrailRoutine(fartData.dashDuration));
        }
    }

    private IEnumerator PlayTrailRoutine(float duration)
    {
        if (dashTrails == null || dashTrails.Count == 0) yield break;

        bool flip = playerSprite != null && playerSprite.flipX;

        for (int i = 0; i < dashTrails.Count; i++)
        {
            if (dashTrails[i] == null) continue;

            Vector3 originalPos = (originalTrailLocalPositions != null && originalTrailLocalPositions.Count > i)
                ? originalTrailLocalPositions[i]
                : dashTrails[i].transform.localPosition;

            float targetX = flip ? -originalPos.x : originalPos.x;
            dashTrails[i].transform.localPosition = new Vector3(targetX, originalPos.y, originalPos.z);
            dashTrails[i].emitting = true;
        }

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < dashTrails.Count; i++)
        {
            if (dashTrails[i] != null)
            {
                dashTrails[i].emitting = false;
            }
        }
    }

    private IEnumerator DashAndDamageRoutine(FartSkillData data, PlayerController controller)
    {
        if (parentRb == null || parentCollider == null)
        {
            Debug.LogError("❌ [LOGIC LỖI] Bạn chưa kéo thả Rigidbody hoặc Collider của Object Cha vào Inspector!");
            yield break;
        }

        PlayerMovement movement = controller.GetComponentInChildren<PlayerMovement>();
        if (movement == null) movement = controller.GetComponentInParent<PlayerMovement>();

        // Get damage multiplier from PlayerSkills
        float damageMultiplier = 1f;
        PlayerSkills skills = controller.GetComponentInChildren<PlayerSkills>();
        if (skills == null) skills = controller.GetComponentInParent<PlayerSkills>();
        if (skills != null) damageMultiplier = skills.damageMultiplier.Value;

        Vector2 dashDir = parentRb.linearVelocity.normalized;
        if (dashDir == Vector2.zero)
        {
            dashDir = (playerSprite != null && playerSprite.flipX) ? Vector2.left : Vector2.right;
        }

        if (movement != null) movement.isMovementLocked = true;

        // Đảm bảo không bật Trigger để không bị xuyên tường
        parentCollider.isTrigger = false;

        float elapsed = 0f;
        bool hasCollided = false;

        // --- CHUẨN BỊ BỘ LỌC CHO HÀM CAST ---
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(data.enemyLayer | LayerMask.GetMask("Ground", "Wall"));
        filter.useLayerMask = true;

        // Mảng để chứa kết quả quét (chỉ cần lấy 1 vật cản đầu tiên)
        RaycastHit2D[] hits = new RaycastHit2D[1];

        // Vòng lặp lướt
        while (elapsed < data.dashDuration && !hasCollided)
        {
            // 1. Ép vận tốc đẩy tới
            parentRb.linearVelocity = dashDir * data.dashForce;

            // 2. CHỦ ĐỘNG QUÉT BẰNG CHÍNH COLLIDER CỦA NHÂN VẬT
            float moveDistance = data.dashForce * Time.fixedDeltaTime;

            // Dùng parentCollider.Cast quét tới trước một khoảng bằng moveDistance + 0.1f (cộng thêm 1 chút xíu để bù trừ sai số vật lý)
            int hitCount = parentCollider.Cast(dashDir, filter, hits, moveDistance + 0.1f);

            if (hitCount > 0)
            {
                RaycastHit2D hit = hits[0]; // Lấy vật đầu tiên tông trúng
                Debug.Log($"🛑 [PHYSICS CAST] Tông trúng: {hit.collider.gameObject.name}. Dừng lướt!");

                hasCollided = true; // Kích hoạt cờ dừng lướt

                // Nếu vật trúng là Enemy thì xử lý sát thương
                if (((1 << hit.collider.gameObject.layer) & data.enemyLayer) != 0)
                {
                    NetworkEntity enemyEntity = hit.collider.GetComponent<NetworkEntity>();
                    if (enemyEntity != null)
                    {
                        int finalDamage = Mathf.RoundToInt(data.damage * damageMultiplier);
                        enemyEntity.TakeDamage(finalDamage);
                        Vector2 knockbackDir = ((Vector2)hit.collider.transform.position - (Vector2)parentCollider.bounds.center).normalized;
                        enemyEntity.ApplyKnockback(knockbackDir * data.knockupForce, 0.3f);
                    }
                    ClientPlayHitEffect(data, hit.point);
                }
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Kết thúc lướt
        parentRb.linearVelocity = Vector2.zero;
        if (movement != null) movement.isMovementLocked = false;

        currentFartData = null;
    }
    
    // XỬ LÝ VA CHẠM VẬT LÝ THÔNG THƯỜNG TẠI ĐÂY
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Chỉ xử lý nếu nhân vật đang trong trạng thái lướt chiêu thức
        if (!isDashing || currentFartData == null || fartController == null) return;

        Debug.Log($"🛑 [PHYSICS] Tông trúng Collider: {collision.gameObject.name}. Dừng lướt ngay lập tức!");
        
        // 1. ÉP PLAYER DỪNG LẠI NGAY LẬP TỨC khi chạm vào BẤT KỲ ĐỊA HÌNH HAY COLLIDER NÀO
        isDashing = false; 
        Rigidbody2D rb = fartController.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 2. KIỂM TRA NẾU VẬT CHẠM THUỘC LAYER ENEMY THÌ MỚI GÂY SÁT THƯƠNG & KNOCKBACK
        if (((1 << collision.gameObject.layer) & currentFartData.enemyLayer) != 0)
        {
            Debug.Log($"💥 [PHYSICS] Xác nhận mục tiêu là Enemy: {collision.gameObject.name}. Kích hoạt Knockback!");
            
            NetworkEntity enemyEntity = collision.gameObject.GetComponent<NetworkEntity>();
            if (enemyEntity != null)
            {
                // Get damage multiplier from PlayerSkills
                float damageMultiplier = 1f;
                if (fartController != null)
                {
                    PlayerSkills skills = fartController.GetComponentInChildren<PlayerSkills>();
                    if (skills == null) skills = fartController.GetComponentInParent<PlayerSkills>();
                    if (skills != null) damageMultiplier = skills.damageMultiplier.Value;
                }
                int finalDamage = Mathf.RoundToInt(currentFartData.damage * damageMultiplier);
                // Gây sát thương
                enemyEntity.TakeDamage(finalDamage);

                // Tính toán hướng hất văng AN TOÀN: Chỉ lấy hướng Trái hoặc Phải dựa trên trục X
                float dirX = collision.transform.position.x > fartController.transform.position.x ? 1f : -1f;
                
                // Tạo vector hất văng (bạn có thể thay số 0 thành 0.2f nếu muốn quái hơi nảy lên nhẹ khi bị tông)
                Vector2 knockbackDir = new Vector2(dirX, 0f).normalized; 
                
                // Áp dụng lực Knockback cho Enemy
                enemyEntity.ApplyKnockback(knockbackDir * currentFartData.knockupForce, 0.3f);
            }
                    
            // Phát hiệu ứng trúng đòn ngay tại điểm va chạm đầu tiên
            if (collision.contactCount > 0)
            {
                ClientPlayHitEffect(currentFartData, collision.GetContact(0).point);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Để tránh bị đánh lừa, bạn nên vẽ Gizmos bằng hitRadius thực tế, hoặc giữ nguyên để tham khảo
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f); 
    }
}