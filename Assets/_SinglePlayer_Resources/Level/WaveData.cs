using UnityEngine;

/// <summary>
/// ScriptableObject holding authored config for one wave zone:
/// enemy prefab pool, spawn point transforms, spawn count/timing.
/// </summary>
[CreateAssetMenu(menuName = "AttoTheSheep/Wave Data", fileName = "WaveData")]
public class WaveData : ScriptableObject
{
    [Header("Enemy Pool")]
    [Tooltip("Prefabs randomly chosen per spawn.")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Spawn Points")]
    [Tooltip("Transforms in the scene marking valid spawn positions.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Timing")]
    [Tooltip("Total enemies to spawn for this wave.")]
    [SerializeField] private int totalEnemyCount = 5;

    [Tooltip("Seconds between each individual spawn.")]
    [SerializeField] private float spawnInterval = 1.5f;

    [Tooltip("0 = no cap. Otherwise pauses spawning when this many are alive.")]
    [SerializeField] private int maxAliveAtOnce;

    // --- Public accessors ---
    public int TotalEnemyCount => totalEnemyCount;
    public float SpawnInterval => spawnInterval;
    public int MaxAliveAtOnce => maxAliveAtOnce;

    public GameObject GetRandomEnemyPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;
        return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
    }

    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    void OnValidate()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            Debug.LogWarning($"WaveData '{name}': no enemy prefabs assigned.", this);
        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogWarning($"WaveData '{name}': no spawn points assigned.", this);
    }
}
