using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Gameplay/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public EnemyKind enemyKind;
    public string displayName;

    [Header("Base Stats")]
    public int maxHealth = 150;
    
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Attacks")]
    public int attackDamage = 20;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.25f;

    [Header("Audio")]
    public EnemyAudioSet audio = new EnemyAudioSet();

    public float MoveSpeed => moveSpeed;
}
