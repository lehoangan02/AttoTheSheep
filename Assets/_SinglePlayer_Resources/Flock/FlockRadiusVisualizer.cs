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
        runeLineRenderer.positionCount = segments;
        runeLineRenderer.useWorldSpace = true;
        runeLineRenderer.loop = true;

        if (runeLineRenderer.material != null)
        {
            runtimeMaterial = runeLineRenderer.material;
        }

        if (backgroundLineRenderer != null)
        {
            backgroundLineRenderer.positionCount = segments;
            backgroundLineRenderer.useWorldSpace = true;
            backgroundLineRenderer.loop = true;
        }
    }

    void Update()
    {
        if (flockManager == null) return;

        bool hasSkill = flockManager.activeLambs != null && flockManager.activeLambs.Count > 0;

        runeLineRenderer.enabled = hasSkill;
        if (backgroundLineRenderer != null) backgroundLineRenderer.enabled = hasSkill;

        if (!hasSkill) return;

        Vector2 center = flockManager.currentFlockCenter.Value;
        float targetRadius = flockManager.currentSkillZoneRadius;

        DrawCircle(center, targetRadius);

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

        float adjustedRuneRadius = radius - (runeLineRenderer.startWidth / 2f);

        float adjustedBgRadius = radius;
        if (backgroundLineRenderer != null)
        {
            adjustedBgRadius = radius - (backgroundLineRenderer.startWidth / 2f);
        }

        for (int i = 0; i < segments; i++)
        {
            float xRune = adjustedRuneRadius * Mathf.Cos(theta);
            float yRune = adjustedRuneRadius * Mathf.Sin(theta);
            Vector3 runePos = new Vector3(xRune, yRune, 0.1f) + (Vector3)center;
            runeLineRenderer.SetPosition(i, runePos);

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