using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FlockRadiusVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FlockManager flockManager;
    
    [Tooltip("Kéo LineRenderer làm NỀN vào đây (Nếu muốn có vành đai nền nổi bật chữ)")]
    [SerializeField] private LineRenderer backgroundLineRenderer;

    [Header("Line Settings")]
    [SerializeField] private int segments = 60; 

    [Header("Skill Zone Settings (Cổ tự)")]
    [SerializeField] private float runeRotationSpeed = 0.2f;

    private LineRenderer runeLineRenderer;
    private Material runtimeMaterial;
    private float currentOffsetX = 0f;

    void Awake()
    {
        runeLineRenderer = GetComponent<LineRenderer>();
        runeLineRenderer.positionCount = segments; // Đã sửa: bỏ "+ 1"
        runeLineRenderer.useWorldSpace = true;
        runeLineRenderer.loop = true; // Đã thêm: Bật vòng lặp khép kín xóa vết gợn
        
        if (runeLineRenderer.material != null)
        {
            runtimeMaterial = runeLineRenderer.material;
        }

        // Tự động cấu hình cho Line nền nếu có
        if (backgroundLineRenderer != null)
        {
            backgroundLineRenderer.positionCount = segments; // Đã sửa: bỏ "+ 1"
            backgroundLineRenderer.useWorldSpace = true;
            backgroundLineRenderer.loop = true; // Đã thêm: Bật vòng lặp khép kín
        }
    }

    void Update()
    {
        if (flockManager == null) return;

        bool hasSkill = flockManager.GetFlockTier() > 0;
        runeLineRenderer.enabled = hasSkill;
        if (backgroundLineRenderer != null) backgroundLineRenderer.enabled = hasSkill;
        
        if (!hasSkill) return;

        Vector2 center = flockManager.currentFlockCenter.Value;
        float targetRadius = flockManager.currentSkillZoneRadius;

        // Vẽ cả 2 vòng cùng lúc để đồng bộ tuyệt đối
        DrawCircle(center, targetRadius);

        // Cuộn Texture để xoay cổ tự
        if (runtimeMaterial != null)
        {
            currentOffsetX -= Time.deltaTime * runeRotationSpeed;
            float currentOffsetY = runtimeMaterial.mainTextureOffset.y;
            runtimeMaterial.mainTextureOffset = new Vector2(currentOffsetX, currentOffsetY);
        }
    }

    private void DrawCircle(Vector2 center, float radius)
    {
        float deltaTheta = (2f * Mathf.PI) / segments;
        float theta = 0f;

        // HIỆU CHỈNH ĐỘ DÀY: Trừ đi một nửa chiều rộng (startWidth) của LineRenderer
        // Giúp mép ngoài cùng co lại, nằm ĐÚNG vào bán kính Logic
        float adjustedRuneRadius = radius - (runeLineRenderer.startWidth / 2f);
        
        float adjustedBgRadius = radius;
        if (backgroundLineRenderer != null)
        {
            adjustedBgRadius = radius - (backgroundLineRenderer.startWidth / 2f);
        }

        for (int i = 0; i < segments; i++) // Đã sửa: chạy đến < segments
        {
            // 1. Tính toán cho Vòng chữ cổ tự
            float xRune = adjustedRuneRadius * Mathf.Cos(theta);
            float yRune = adjustedRuneRadius * Mathf.Sin(theta);
            Vector3 runePos = new Vector3(xRune, yRune, 0.1f) + (Vector3)center; 
            runeLineRenderer.SetPosition(i, runePos);

            // 2. Tính toán cho Vòng nền (nếu có)
            if (backgroundLineRenderer != null)
            {
                float xBg = adjustedBgRadius * Mathf.Cos(theta);
                float yBg = adjustedBgRadius * Mathf.Sin(theta);
                Vector3 bgPos = new Vector3(xBg, yBg, 0.12f) + (Vector3)center;
                backgroundLineRenderer.SetPosition(i, bgPos);
            }

            theta += deltaTheta;
        }
    }
}