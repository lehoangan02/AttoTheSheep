using UnityEngine;

[CreateAssetMenu(fileName = "RollingSkillData", menuName = "Gameplay/Skills/Rolling Skill Data")]
public class RollingSkillData : SkillData
{
    [Header("Mechanic: Hút & Nuốt")]
    public float pullRadius = 6f;
    public float pullSpeed = 5f;
    public float hitRadius = 1.5f;
    public LayerMask enemyLayer;

    [Header("Mechanic: Di chuyển & Quay")]
    public float maxSpeedMultiplier = 2.0f;
    public float acceleration = 1.5f;
    public float duration = 4f;
    public float baseRotationSpeed = 360f;

    public float AccelerationDuration
    {
        get
        {
            if (acceleration <= 0) return 0f;
            return (maxSpeedMultiplier - 1f) / acceleration;
        }
    }
}