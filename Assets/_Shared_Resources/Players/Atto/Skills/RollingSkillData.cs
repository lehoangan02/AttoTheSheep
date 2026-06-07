using UnityEngine;

[CreateAssetMenu(fileName = "RollingSkillData", menuName = "Gameplay/Skills/Rolling Skill Data")]
public class RollingSkillData : SkillData
{
    [Header("Chỉ số đặc trưng của Lăn/Hóa Bụi")]
    public float maxSpeedMultiplier = 2.0f;
    public float acceleration = 1.5f;
    public float duration = 5f;
    public float disappearDuration = 0.2f;
    public float baseRotationSpeed = 360f;

    // Tự động tính thời gian tăng tốc
    public float AccelerationDuration 
    {
        get 
        {
            if (acceleration <= 0) return 0f;
            return (maxSpeedMultiplier - 1f) / acceleration;
        }
    }
}