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
            Debug.LogWarning($"[WaveController] {name} already spawning.");
            return;
        }

        if (waveData == null)
        {
            Debug.LogError($"[WaveController] {name} has no WaveData assigned.");
            return;
        }

        if (logEvents) Debug.Log($"[WaveController] {name} wave started.");
        _spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>Called by LevelManager on level restart.</summary>
    public void ResetWave()
    {
        if (logEvents) Debug.Log($"[WaveController] {name} resetting.");

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

    // --- Spawn Loop ---

    private IEnumerator SpawnRoutine()
    {
        while (_spawnedCount < waveData.TotalEnemyCount)
        {
            // Respect max-alive cap.
            if (waveData.MaxAliveAtOnce > 0 && _aliveCount >= waveData.MaxAliveAtOnce)
            {
                yield return null;
                continue;
            }

            SpawnOneEnemy();
            yield return new WaitForSeconds(waveData.SpawnInterval);
        }

        if (logEvents) Debug.Log($"[WaveController] {name}: all {_spawnedCount} enemies spawned.");
    }

    private void SpawnOneEnemy()
    {
        GameObject prefab = waveData.GetRandomEnemyPrefab();
        Transform spawnPoint = waveData.GetRandomSpawnPoint();

        if (prefab == null)
        {
            Debug.LogWarning($"[WaveController] {name}: no enemy prefab available.");
            return;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);

        NetworkObject netObj = instance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"[WaveController] {prefab.name} has no NetworkObject component.");
            Destroy(instance);
            return;
        }

        // Subscribe to death event BEFORE spawn (OnNetworkSpawn may fire immediately).
        NetworkEntity entity = instance.GetComponent<NetworkEntity>();
        if (entity != null)
        {
            entity.OnDied += OnEnemyDied;
        }

        _livingEnemies.Add(netObj);
        _spawnedCount++;
        _aliveCount++;

        netObj.Spawn(true);

        if (logEvents) Debug.Log($"[WaveController] Spawned {prefab.name} at {position}. alive={_aliveCount}");
    }

    // --- Death Callback ---

    private void OnEnemyDied()
    {
        _aliveCount--;

        // Clean up dead enemies from the tracker next frame (or lazily in ResetWave).
        if (logEvents) Debug.Log($"[WaveController] Enemy died. alive={_aliveCount}");

        CheckCleared();
    }

    private void CheckCleared()
    {
        if (_isCleared) return;

        if (_spawnedCount >= waveData.TotalEnemyCount && _aliveCount <= 0)
        {
            _isCleared = true;
            _spawnRoutine = null;

            if (logEvents) Debug.Log($"[WaveController] {name} wave cleared!");
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
