using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Per-zone controller. Reads a WaveData SO, spawns enemies at intervals,
/// tracks living enemies, and fires OnWaveCleared when done.
/// </summary>
public class WaveController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private WaveData waveData;
    public WaveData Data => waveData;
    [Tooltip("Scene Transforms where enemies spawn. ScriptableObjects can't hold scene refs.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Debug")]
    [SerializeField] private bool logEvents;

    /// <summary>Fired when all enemies from this wave have been spawned and killed.</summary>
    public event Action<WaveController> OnWaveCleared;

    private int _spawnedCount;
    private int _aliveCount;
    private bool _isCleared;
    private Coroutine _spawnRoutine;

    // Spawned enemies tracked for cleanup on reset.
    private readonly List<NetworkObject> _livingEnemies = new List<NetworkObject>();

    public bool IsCleared => _isCleared;

    // --- Public API ---

    /// <summary>Called by LevelManager to start this wave.</summary>
    public void BeginWave()
    {
        if (_spawnRoutine != null)
        {

            return;
        }

        if (waveData == null)
        {

            return;
        }

        if (logEvents)
        _spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>Called by LevelManager on level restart.</summary>
    public void ResetWave()
    {
        if (logEvents)

        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        // Despawn all living enemies.
        for (int i = _livingEnemies.Count - 1; i >= 0; i--)
        {
            NetworkObject obj = _livingEnemies[i];
            if (obj != null && obj.IsSpawned)
            {
                obj.Despawn(true);
            }
        }
        _livingEnemies.Clear();

        _spawnedCount = 0;
        _aliveCount = 0;
        _isCleared = false;
    }

    private int _difficultyMultiplier = 1;
    private List<GameObject> _allEnemyTypes;

    public void SetScaling(int roundCount, List<GameObject> allTypes)
    {
        _difficultyMultiplier = 1 + roundCount; // Round 0 = 1x, Round 1 = 2x, etc.
        if (roundCount > 0 && allTypes != null && allTypes.Count > 0)
        {
            _allEnemyTypes = allTypes;
        }
        else
        {
            _allEnemyTypes = null;
        }
    }

    // --- Spawn Loop ---

    private IEnumerator SpawnRoutine()
    {
        int totalToSpawn = waveData.TotalEnemyCount * _difficultyMultiplier;

        while (_spawnedCount < totalToSpawn)
        {
            SpawnOneEnemy();

            // Speed up spawn interval as rounds increase so it doesn't take forever
            float currentInterval = Mathf.Max(0.5f, waveData.SpawnInterval / (1f + (_difficultyMultiplier - 1) * 0.25f));
            yield return new WaitForSeconds(currentInterval);
        }

        if (logEvents)
    }

    private void SpawnOneEnemy()
    {
        GameObject prefab = null;
        if (_allEnemyTypes != null && _allEnemyTypes.Count > 0)
        {
            prefab = _allEnemyTypes[UnityEngine.Random.Range(0, _allEnemyTypes.Count)];
        }
        else
        {
            prefab = waveData.GetRandomEnemyPrefab();
        }

        Transform spawnPoint = GetRandomSpawnPoint();

        if (prefab == null)
        {

            return;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);

        NetworkObject netObj = instance.GetComponent<NetworkObject>();
        if (netObj == null)
        {

            Destroy(instance);
            return;
        }

        // Subscribe to death event BEFORE spawn (OnNetworkSpawn may fire immediately).
        NetworkEntity entity = instance.GetComponent<NetworkEntity>();
        if (entity != null)
        {
            if (entity is EnemyEntity enemyEntity && enemyEntity.Data != null)
            {
                enemyEntity.Configure(enemyEntity.Data);
                // Force initialization since IsServer on NetworkBehaviour is false before Spawn
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    // Scale enemy health based on round multiplier (optional, but requested increasing difficulty)
                    int scaledHealth = enemyEntity.Data.maxHealth;
                    if (_difficultyMultiplier > 1)
                    {
                        scaledHealth = Mathf.CeilToInt(scaledHealth * (1f + (_difficultyMultiplier - 1) * 0.5f));
                    }
                    entity.currentHealth.Value = scaledHealth;
                    entity.currentMoveSpeed.Value = enemyEntity.Data.moveSpeed;
                }
            }
            entity.OnDied += OnEnemyDied;
        }

        _livingEnemies.Add(netObj);
        _spawnedCount++;
        _aliveCount++;

        netObj.Spawn(true);

        if (logEvents)
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;
        return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
    }

    // --- Death Callback ---

    private void OnEnemyDied()
    {
        _aliveCount--;

        // Clean up dead enemies from the tracker next frame (or lazily in ResetWave).
        if (logEvents)

        CheckCleared();
    }

    private void CheckCleared()
    {
        if (_isCleared) return;

        if (_spawnedCount >= waveData.TotalEnemyCount && _aliveCount <= 0)
        {
            _isCleared = true;
            _spawnRoutine = null;

            if (logEvents)
            OnWaveCleared?.Invoke(this);
        }
    }

    // --- Cleanup ---

    void OnDestroy()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        // Unsubscribe from remaining living enemies.
        foreach (NetworkObject netObj in _livingEnemies)
        {
            if (netObj == null) continue;
            NetworkEntity entity = netObj.GetComponent<NetworkEntity>();
            if (entity != null) entity.OnDied -= OnEnemyDied;
        }
        _livingEnemies.Clear();
    }
}
