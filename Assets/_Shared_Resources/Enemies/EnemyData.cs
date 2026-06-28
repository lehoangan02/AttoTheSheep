using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Gameplay/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
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

    [Header("Spawn / Death VFX")]
    public GameObject spawnVFXPrefab;
    public GameObject deathVFXPrefab;
    public float spawnFadeDuration = 0.5f;
    public float deathFadeDuration = 0.5f;

    public float MoveSpeed => moveSpeed;
}
