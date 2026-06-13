using UnityEngine;

[CreateAssetMenu(fileName = "HeadbuttSkillData", menuName = "Gameplay/Skills/Headbutt Skill Data")]
public class HeadbuttSkillData : SkillData
{
    [Header("Cài đặt Húc Đầu")]
    public float hitRadius = 1f;                    // Bán kính vùng sát thương
    public Vector2 hitboxOffset = new Vector2(1f, 0f); // Vị trí hitbox so với tâm nhân vật (x sẽ tự đảo theo hướng quay mặt)
    public float knockbackForce = 5f;               // Lực đẩy lùi nhẹ quái vật
    public float attackDelay = 0.1f;                // Thời gian chờ từ lúc bấm đến lúc sát thương nổ (khớp với frame đầu đưa lên của animation)
    public float recoveryTime = 0.2f;               // Thời gian đứng im sau khi húc
    public LayerMask enemyLayer;                    // Layer của Quái vật
}