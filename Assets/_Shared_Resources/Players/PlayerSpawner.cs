using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour
{
    public GameObject attoPrefab;
    public GameObject pawnPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
    }

    void SpawnPlayer(ulong clientId)
    {
        GameObject prefabToSpawn;

        // Host = Atto
        // Client = Pawn
        if (clientId == 0)
        {
            prefabToSpawn = attoPrefab;
        }
        else
        {
            prefabToSpawn = pawnPrefab;
        }

        GameObject player = Instantiate(
            prefabToSpawn,
            Vector3.zero,
            Quaternion.identity
        );

        player.GetComponent<NetworkObject>()
            .SpawnAsPlayerObject(clientId);
    }
}