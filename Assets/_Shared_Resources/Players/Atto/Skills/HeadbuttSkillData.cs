using UnityEngine;

[CreateAssetMenu(fileName = "HeadbuttSkillData", menuName = "Gameplay/Skills/Headbutt Skill Data")]
public class HeadbuttSkillData : SkillData
{
    [Header("Cài đặt Húc Đầu")]
    public float hitRadius = 1f;
    public Vector2 hitboxOffset = new Vector2(1f, 0f);
    public float knockbackForce = 5f;
    public float attackDelay = 0.1f;
    public float recoveryTime = 0.2f;
    public LayerMask enemyLayer;
}