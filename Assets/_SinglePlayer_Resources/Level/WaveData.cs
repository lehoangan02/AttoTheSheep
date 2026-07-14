using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AttoTheSheep/Wave Data", fileName = "WaveData")]
public class WaveData : ScriptableObject
{
    [Header("Enemy Pool")]
    [Tooltip("Prefabs randomly chosen per spawn.")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Timing")]
    [Tooltip("Total enemies to spawn for this wave.")]
    [SerializeField] private int totalEnemyCount = 5;

    [Tooltip("Seconds between each individual spawn.")]
    [SerializeField] private float spawnInterval = 1.5f;

    private List<GameObject> _shuffledPool;

    // --- Public accessors ---
    public int TotalEnemyCount => totalEnemyCount;
    public float SpawnInterval => spawnInterval;
    public GameObject[] EnemyPrefabs => enemyPrefabs;

    public GameObject GetRandomEnemyPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;

        if (_shuffledPool == null || _shuffledPool.Count == 0)
        {
            _shuffledPool = new List<GameObject>(enemyPrefabs);
            for (int i = _shuffledPool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                GameObject temp = _shuffledPool[i];
                _shuffledPool[i] = _shuffledPool[j];
                _shuffledPool[j] = temp;
            }
        }

        int last = _shuffledPool.Count - 1;
        GameObject result = _shuffledPool[last];
        _shuffledPool.RemoveAt(last);
        return result;
    }

    void OnValidate()
    {
        if (enemyPrefabs != null && enemyPrefabs.Length == 0)
            Debug.LogWarning($"WaveData '{name}': no enemy prefabs assigned.", this);
    }
}
