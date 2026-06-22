using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class PlayerSneezeSkill : BaseSkillComponent
{
    [Header("Spawn Point")]
    [SerializeField] private Transform spawnPoint; // Vị trí spawn đạn (nên đặt trước mặt Atto)

    private SneezeSkillData currentSneezeData;
    private PlayerController sneezeController;

    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        if (data is SneezeSkillData sneezeData && controller != null)
        {
            currentSneezeData = sneezeData;
            sneezeController = controller;
            StartCoroutine(SneezeRoutine(sneezeData, controller));
        }
    }

    public override void ClientPlayVisual(SkillData data)
    {
        base.ClientPlayVisual(data); 
    }

    private IEnumerator SneezeRoutine(SneezeSkillData data, PlayerController controller)
    {

        Debug.Log($"🤧 [SNEEZE] Atto hắt xì! Bắn {data.projectileCount} tia nước mũi!");

        if (data.projectilePrefab == null)
        {
            Debug.LogError("❌ [SNEEZE] Chưa gán projectilePrefab trong SneezeSkillData!");
            yield break;
        }

        // Chia đều damage cho các tia (tổng damage / số tia)
        int damagePerProjectile = Mathf.Max(1, (int)data.damage / data.projectileCount);
        Debug.Log($"🤧 [SNEEZE] Tổng DMG: {data.damage}, {data.projectileCount} tia → mỗi tia: {damagePerProjectile} DMG.");

        // Xác định hướng bắn (ưu tiên hướng đang di chuyển / hướng mặt)
        Vector2 baseDirection = GetFacingDirection(controller);
        // Xác định flipX đồng bộ cho TẤT CẢ đạn dựa trên baseDirection,
        // tránh mỗi đạn tự flip riêng gây chụm tia
        bool flipX = baseDirection.x < 0;

        // Spawn N tia theo hình spread
        for (int i = 0; i < data.projectileCount; i++)
        {
            // --- SỬA LỖI ĐÈ LAYER THỨ TỰ TRÊN XUỐNG ---
            // Đảo ngược index nếu bắn trái để đạn luộn được sinh ra từ dưới lên trên.
            // Điều này đảm bảo bóng của tia trên luôn đè đúng lên thân tia dưới ở cả 2 hướng.
            int index = flipX ? (data.projectileCount - 1 - i) : i;

            // Tính góc cho tia thứ index
            float halfSpread = data.spreadAngle * 0.5f;
            float angleStep = data.projectileCount > 1 ? data.spreadAngle / (data.projectileCount - 1) : 0f;
            float currentAngle = -halfSpread + (angleStep * index);

            // Xoay hướng baseDirection đi 1 góc currentAngle
            Vector2 projectileDir = RotateVector(baseDirection, currentAngle);

            // Vị trí spawn
            Vector3 spawnPos = controller.transform.position;
            if (spawnPoint != null) spawnPos = spawnPoint.position;

            // Instantiate prefab
            GameObject projectileObj = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);

            // Spawn trên network để đồng bộ giữa các client
            NetworkObject netObj = projectileObj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }

            // Khởi tạo đạn
            SneezeProjectile projectile = projectileObj.GetComponent<SneezeProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(projectileDir, data, damagePerProjectile, flipX);
            }
        }

        yield return null;
    }

    /// <summary>
    /// Lấy hướng mặt của Player (dựa vào SpriteRenderer.flipX hoặc hướng di chuyển)
    /// </summary>
    private Vector2 GetFacingDirection(PlayerController controller)
    {
        // Ưu tiên hướng di chuyển
        Rigidbody2D rb = controller.GetComponent<Rigidbody2D>();
        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            return rb.linearVelocity.normalized;
        }

        // Fallback: dựa vào SpriteRenderer.flipX
        SpriteRenderer sprite = controller.GetComponentInChildren<SpriteRenderer>();
        if (sprite != null && sprite.flipX)
        {
            return Vector2.left;
        }
        return Vector2.right;
    }

    /// <summary>
    /// Xoay vector direction đi 1 góc angle (degree)
    /// </summary>
    private Vector2 RotateVector(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // Vẽ Gizmos để visualize spread của đạn trong Editor
    private void OnDrawGizmosSelected()
    {
        if (currentSneezeData == null) return;

        Gizmos.color = Color.cyan;

        // Xác định vị trí
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector2 baseDir = Vector2.right; // Mặc định nhìn phải

        // Vẽ các tia spread
        float halfSpread = currentSneezeData.spreadAngle * 0.5f;
        float angleStep = currentSneezeData.projectileCount > 1 
            ? currentSneezeData.spreadAngle / (currentSneezeData.projectileCount - 1) 
            : 0f;

        for (int i = 0; i < currentSneezeData.projectileCount; i++)
        {
            float currentAngle = -halfSpread + (angleStep * i);
            Vector2 dir = RotateVector(baseDir, currentAngle);
            Gizmos.DrawRay(pos, dir * currentSneezeData.projectileMaxDistance);
        }
    }
}