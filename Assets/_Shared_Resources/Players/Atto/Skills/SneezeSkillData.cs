using UnityEngine;

[CreateAssetMenu(fileName = "SneezeSkillData", menuName = "Gameplay/Skills/Sneeze Skill Data")]
public class SneezeSkillData : SkillData
{
    [Header("Cài đặt Đạn Nước Mũi (Projectile)")]
    public GameObject projectilePrefab;     // Prefab đạn nước mũi
    public int projectileCount = 5;         // Số tia bắn ra
    public float spreadAngle = 60f;         // Góc spread (VD: 60 độ -> ±30 độ)
    public float projectileSpeed = 15f;     // Tốc độ bay
    public float projectileMaxDistance = 8f;// Khoảng cách bay tối đa

    [Header("Cài đặt Hiệu ứng")]
    public float stunDuration = 1.5f;       // Thời gian choáng
    public LayerMask enemyLayer;            // Layer của quái vật
}