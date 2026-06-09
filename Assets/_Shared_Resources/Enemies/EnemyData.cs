using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Gameplay/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public EnemyKind enemyKind;
    public string displayName;

    [Header("Stats")]
    public int maxHealth = 150;
    public float moveSpeed = 5f;
    public int attackDamage = 20;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.25f;
}
