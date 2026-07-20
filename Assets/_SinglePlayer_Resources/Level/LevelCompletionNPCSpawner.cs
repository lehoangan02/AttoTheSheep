using UnityEngine;

public class LevelCompletionNPCSpawner : MonoBehaviour
{
    [Header("NPC Settings")]
    [Tooltip("Prefab của NPC chứa Script Dialogue và WinBannerOnDialogueEnd")]
    [SerializeField] private GameObject npcPrefab;

    [Tooltip("Vị trí mà NPC sẽ spawn ra")]
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelComplete += SpawnNPC;
        }
    }

    private void OnDestroy()
    {

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelComplete -= SpawnNPC;
        }
    }

    private void SpawnNPC()
    {
        if (npcPrefab != null && spawnPoint != null)
        {

            GameObject npc = Instantiate(npcPrefab, spawnPoint.position, spawnPoint.rotation);

            if (npc.TryGetComponent(out Unity.Netcode.NetworkObject netObj))
            {
                netObj.Spawn();
            }
        }
        else
        {

        }
    }
}