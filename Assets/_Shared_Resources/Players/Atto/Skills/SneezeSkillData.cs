using UnityEngine;

[CreateAssetMenu(fileName = "SneezeSkillData", menuName = "Gameplay/Skills/Sneeze Skill Data")]
public class SneezeSkillData : SkillData
{
    [Header("Cài đặt Đạn Nước Mũi (Projectile)")]
    public GameObject projectilePrefab;     // Prefab đạn lúc bay
    public int projectileCount = 5;         // Số tia bắn ra
    public float spreadAngle = 60f;         // Góc spread
    public float projectileSpeed = 15f;     // Tốc độ bay
    public float projectileMaxDistance = 8f;// Khoảng cách bay tối đa

    [Header("Cài đặt Vùng Làm Chậm (Puddle)")]
    public GameObject puddlePrefab;         // Prefab vũng nước mũi rơi xuống đất
    public float puddleDuration = 5f;       // Thời gian vũng nước tồn tại
    public float slowMultiplier = 0.5f;     // Hệ số làm chậm (VD: 0.5 = giảm 50% tốc độ)
    public LayerMask hitLayer;              // Layer để đạn dừng lại (Nên bao gồm Quái vật và Môi trường/Tường)
}