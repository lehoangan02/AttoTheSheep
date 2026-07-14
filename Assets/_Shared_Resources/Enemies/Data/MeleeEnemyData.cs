using UnityEngine;

[CreateAssetMenu(fileName = "MeleeEnemyData", menuName = "Gameplay/Enemies/Melee Enemy Data")]
public class MeleeEnemyData : EnemyData
{
    [Header("Melee Attacks")]
    public int attackDamage = 20;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.25f;
}
