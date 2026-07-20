using UnityEngine;

[CreateAssetMenu(fileName = "FartSkillData", menuName = "Gameplay/Skills/Fart Skill Data")]
public class FartSkillData : SkillData
{
    [Header("Cài đặt Lướt (Dash)")]
    public float dashForce = 25f;
    public float dashDuration = 0.2f;

    [Header("Cài đặt Chiến đấu")]
    public float knockupForce = 15f;
    public float hitRadius = 2f;
    public LayerMask enemyLayer;
}