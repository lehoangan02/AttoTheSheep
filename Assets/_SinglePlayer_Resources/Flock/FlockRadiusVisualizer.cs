using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FlockRadiusVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FlockManager flockManager;

    [Header("Line Settings")]
    [SerializeField] private int segments = 60; // Độ mượt của vòng tròn

    [Header("Skill Zone Settings (Cổ tự)")]
    [SerializeField] private float runeRotationSpeed = 0.2f;

    private LineRenderer lineRenderer;
    private Material runtimeMaterial;
    private float currentOffsetX = 0f;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segments + 1;
        lineRenderer.useWorldSpace = true;
        
        // Tránh ghi đè asset material gốc
        if (lineRenderer.material != null)
        {
            runtimeMaterial = lineRenderer.material;
        }
    }

    void Update()
    {
        if (flockManager == null) return;

        // Vòng Skill chỉ hiện khi bầy cừu có ít nhất 1 chiêu thức (Tier > 0)
        bool hasSkill = flockManager.GetFlockTier() > 0;
        lineRenderer.enabled = hasSkill;
        
        if (!hasSkill) return;

        // Lấy tọa độ và bán kính vòng Skill từ FlockManager
        Vector2 center = flockManager.currentFlockCenter.Value;
        float targetRadius = flockManager.currentSkillZoneRadius;

        // Vẽ vòng tròn ma thuật
        DrawCircle(center, targetRadius);

        // Cuộn Texture để xoay cổ tự
        if (runtimeMaterial != null)
        {
            currentOffsetX -= Time.deltaTime * runeRotationSpeed;
            
            // Giữ nguyên Offset Y trên Inspector, chỉ cập nhật Offset X để chữ chạy vòng quanh
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
            
            // Đặt Z dương nhẹ để đảm bảo vòng tròn nằm dưới chân cừu
            Vector3 pos = new Vector3(x, y, 0.1f) + (Vector3)center; 
            lineRenderer.SetPosition(i, pos);

            theta += deltaTheta;
        }
    }
}