using UnityEngine;

[CreateAssetMenu(fileName = "FartSkillData", menuName = "Gameplay/Skills/Fart Skill Data")]
public class FartSkillData : SkillData
{
    [Header("Cài đặt Lướt (Dash)")]
    public float dashForce = 25f;       // Lực lướt tới
    public float dashDuration = 0.2f;   // Thời gian lướt

    [Header("Cài đặt Chiến đấu")]
    public float knockupForce = 15f;    // Lực hất tung
    public float hitRadius = 2f;        // Bán kính vùng sát thương
    public LayerMask enemyLayer;        // Layer của Quái vật
}