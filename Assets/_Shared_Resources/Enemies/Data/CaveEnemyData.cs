using UnityEngine;

[CreateAssetMenu(fileName = "CaveEnemyData", menuName = "Gameplay/Enemies/Cave Enemy Data")]
public class CaveEnemyData : MeleeEnemyData
{
    [Header("Status Effects")]
    [Tooltip("Status effects applied on a successful melee hit.")]
    public StatusEffectData[] onHitEffects;
}
