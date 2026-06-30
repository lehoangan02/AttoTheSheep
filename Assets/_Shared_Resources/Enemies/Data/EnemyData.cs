using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Gameplay/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Base Stats")]
    public int maxHealth = 150;
    
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Spawn / Death VFX")]
    public GameObject spawnVFXPrefab;
    public GameObject deathVFXPrefab;
    public float spawnFadeDuration = 0.5f;
    public float deathFadeDuration = 0.5f;
}
