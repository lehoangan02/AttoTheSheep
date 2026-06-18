using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class EnemyPrefabEntry
{
    public EnemyKind enemyKind;
    public EnemyData data;
    public GameObject prefab;
}

public class EnemyWaveSpawner : NetworkBehaviour
{
    [SerializeField] private StageId currentStage = StageId.Farm;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private List<EnemyPrefabEntry> enemyPrefabs = new List<EnemyPrefabEntry>();
    [SerializeField] private List<EnemySpawnRule> spawnRules = new List<EnemySpawnRule>();

    private readonly HashSet<EnemyKind> spawnedOnceKinds = new HashSet<EnemyKind>();
    private int currentWave = 1;
    private int nextSpawnPointIndex;

    public int CurrentWave => currentWave;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            EnsureSpawnRules();
            SpawnCurrentWave();
        }
    }

    public void SetStage(StageId stage)
    {
        currentStage = stage;
    }

    public void StartWave(int wave)
    {
        if (!IsServer) return;

        currentWave = Mathf.Max(1, wave);
        EnsureSpawnRules();
        SpawnCurrentWave();
    }

    public void AdvanceWave()
    {
        if (!IsServer) return;

        currentWave++;
        EnsureSpawnRules();
        SpawnCurrentWave();
    }

    private void Reset()
    {
        spawnRules = EnemySpawnRuleCatalog.CreateDefaultRules();
    }

    private void EnsureSpawnRules()
    {
        if (spawnRules == null || spawnRules.Count == 0)
        {
            spawnRules = EnemySpawnRuleCatalog.CreateDefaultRules();
        }
    }

    private void SpawnCurrentWave()
    {
        foreach (EnemySpawnRule rule in spawnRules)
        {
            if (!rule.Matches(currentStage, currentWave)) continue;
            if (rule.spawnOnce && spawnedOnceKinds.Contains(rule.enemyKind)) continue;

            SpawnRule(rule);

            if (rule.spawnOnce)
            {
                spawnedOnceKinds.Add(rule.enemyKind);
            }
        }
    }

    private void SpawnRule(EnemySpawnRule rule)
    {
        EnemyPrefabEntry entry = enemyPrefabs.Find(item => item.enemyKind == rule.enemyKind);
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning($"EnemyWaveSpawner missing prefab for {rule.enemyKind}");
            return;
        }

        int count = Mathf.Max(0, rule.countPerWave);
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy(entry);
        }
    }

    private void SpawnEnemy(EnemyPrefabEntry entry)
    {
        Transform spawnPoint = GetNextSpawnPoint();
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;

        GameObject enemy = Instantiate(entry.prefab, position, Quaternion.identity);
        EnemyEntity entity = enemy.GetComponent<EnemyEntity>();
        if (entity != null && entry.data != null)
        {
            entity.Configure(entry.data);
        }

        EnemyBrain brain = enemy.GetComponent<EnemyBrain>();
        if (brain != null && entry.data != null && entry.data.behavior != null)
        {
            brain.Behavior = entry.data.behavior;
        }

        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"{entry.prefab.name} is missing NetworkObject and cannot be spawned.");
            Destroy(enemy);
            return;
        }

        networkObject.Spawn(true);
    }

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        Transform point = spawnPoints[nextSpawnPointIndex % spawnPoints.Length];
        nextSpawnPointIndex++;
        return point;
    }
}
