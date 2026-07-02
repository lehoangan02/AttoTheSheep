using UnityEngine;

[CreateAssetMenu(fileName = "KnightEnemyData", menuName = "Gameplay/Enemies/Knight Enemy Data")]
public class KnightEnemyData : MeleeEnemyData
{
    [Header("Guard")]
    [Range(0f, 1f)]
    public float guardChance = 0.2f;
}
