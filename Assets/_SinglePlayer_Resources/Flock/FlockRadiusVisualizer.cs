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
        runeLineRenderer.positionCount = segments + 1;
        runeLineRenderer.useWorldSpace = true;
        
        if (runeLineRenderer.material != null)
        {
            runtimeMaterial = runeLineRenderer.material;
        }

        // Tự động cấu hình cho Line nền nếu có
        if (backgroundLineRenderer != null)
        {
            backgroundLineRenderer.positionCount = segments + 1;
            backgroundLineRenderer.useWorldSpace = true;
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

        for (int i = 0; i < segments + 1; i++)
        {
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            
            // 1. Vòng chữ nằm ở Z = 0.1f
            Vector3 runePos = new Vector3(x, y, 0.1f) + (Vector3)center; 
            runeLineRenderer.SetPosition(i, runePos);

            // 2. Vòng nền nằm ở Z = 0.12f (Hơi lùi về sau một chút để nằm DƯỚI chữ)
            if (backgroundLineRenderer != null)
            {
                Vector3 bgPos = new Vector3(x, y, 0.12f) + (Vector3)center;
                backgroundLineRenderer.SetPosition(i, bgPos);
            }

            theta += deltaTheta;
        }
    }
}