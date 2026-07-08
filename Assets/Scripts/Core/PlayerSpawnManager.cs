using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerSpawnManager : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float minDistanceBetweenPlayers = 1.5f;
    [SerializeField] private int maxSpawnAttempts = 30;

    private readonly HashSet<ulong> _spawnedClientIds = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // Host is the only client connected at the moment SampleScene loads on the host.
        // StartHost() fires OnClientConnectedCallback for the host BEFORE this scene's
        // OnNetworkSpawn runs, so we must spawn the host explicitly here.
        SpawnPlayer(NetworkManager.ServerClientId);
        _spawnedClientIds.Add(NetworkManager.ServerClientId);

        // Defensive sweep: any client that connected between scene-load-start and this
        // OnNetworkSpawn would otherwise be missed. Iterate the read-only list and
        // spawn anyone we have not already spawned.
        var connectedIds = NetworkManager.Singleton.ConnectedClientsIds;
        for (int i = 0; i < connectedIds.Count; i++)
        {
            ulong id = connectedIds[i];
            if (_spawnedClientIds.Contains(id)) continue;
            SpawnPlayer(id);
            _spawnedClientIds.Add(id);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        if (_spawnedClientIds.Contains(clientId)) return;
        SpawnPlayer(clientId);
        _spawnedClientIds.Add(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;

        if (!GetValidSpawnPosition(out Vector3 pos))
        {
            pos = Vector3.zero;
        }

        GameObject instance = Instantiate(playerPrefab, pos, Quaternion.identity);
        NetworkObject netObj = instance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"[PlayerSpawnManager] playerPrefab has no NetworkObject component!");
            return;
        }

        netObj.SpawnAsPlayerObject(clientId, destroyWithScene: true);
        Debug.Log($"[PlayerSpawnManager] Spawned player for clientId={clientId} at {pos}");

        // Spawn a FlockManager for this player!
        GameObject flockPrefab = GetFlockManagerPrefab();
        if (flockPrefab != null)
        {
            GameObject fmInstance = Instantiate(flockPrefab, pos, Quaternion.identity);
            NetworkObject fmNetObj = fmInstance.GetComponent<NetworkObject>();
            if (fmNetObj != null)
            {
                fmNetObj.SpawnWithOwnership(clientId, destroyWithScene: true);
                Debug.Log($"[PlayerSpawnManager] Spawned FlockManager for clientId={clientId}");
            }
        }
    }

    private GameObject GetFlockManagerPrefab()
    {
        foreach (var prefabInfo in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
        {
            if (prefabInfo.Prefab != null && prefabInfo.Prefab.GetComponent<FlockManager>() != null)
            {
                return prefabInfo.Prefab;
            }
        }
        return null;
    }

    private bool GetValidSpawnPosition(out Vector3 position)
    {
        if (!IsServer)
        {
            position = Vector3.zero;
            return false;
        }

        // Pre-initialized so the `out` parameter is definitely assigned on every return path.
        Vector3 lastCandidate = Vector3.zero;
        var connectedIds = NetworkManager.Singleton.ConnectedClientsIds;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = new Vector3(offset.x, offset.y, 0f);
            lastCandidate = candidate;

            bool rejected = false;
            for (int i = 0; i < connectedIds.Count; i++)
            {
                NetworkObject existing = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(connectedIds[i]);
                if (existing != null && existing.IsSpawned)
                {
                    float dist = Vector2.Distance((Vector2)candidate, (Vector2)existing.transform.position);
                    if (dist < minDistanceBetweenPlayers)
                    {
                        rejected = true;
                        break;
                    }
                }
            }

            if (!rejected)
            {
                position = candidate;
                return true;
            }
        }

        Debug.LogWarning($"[PlayerSpawnManager] Could not find non-overlapping position after {maxSpawnAttempts} attempts. Using last candidate.");
        position = lastCandidate;
        return true;
    }
}
