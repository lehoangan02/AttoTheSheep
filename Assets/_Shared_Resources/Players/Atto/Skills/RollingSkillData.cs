using UnityEngine;

[CreateAssetMenu(fileName = "RollingSkillData", menuName = "Gameplay/Skills/Rolling Skill Data")]
public class RollingSkillData : SkillData
{
    [Header("Mechanic: Hút & Nuốt")]
    public float pullRadius = 6f;       // Bán kính lốc xoáy quét từ xa
    public float pullSpeed = 5f;        // Tốc độ kéo quái vào tâm
    public float hitRadius = 1.5f;      // Bán kính để nuốt chửng quái vào bụng
    public LayerMask enemyLayer;        // Layer để quét quái

    [Header("Mechanic: Di chuyển & Quay")]
    public float maxSpeedMultiplier = 2.0f;
    public float acceleration = 1.5f;
    public float duration = 4f;
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