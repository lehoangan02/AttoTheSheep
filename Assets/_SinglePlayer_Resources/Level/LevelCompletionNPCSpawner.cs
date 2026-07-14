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
        // Đăng ký sự kiện khi LevelManager hoàn thành tất cả các wave
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelComplete += SpawnNPC;
        }
    }

    private void OnDestroy()
    {
        // Hủy đăng ký sự kiện để tránh lỗi
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelComplete -= SpawnNPC;
        }
    }

    private void SpawnNPC()
    {
        if (npcPrefab != null && spawnPoint != null)
        {
            Debug.Log("[LevelCompletionNPCSpawner] Tất cả wave đã xong. Spawn NPC!");
            GameObject npc = Instantiate(npcPrefab, spawnPoint.position, spawnPoint.rotation);
            
            if (npc.TryGetComponent(out Unity.Netcode.NetworkObject netObj))
            {
                netObj.Spawn();
            }
        }
        else
        {
            Debug.LogWarning("[LevelCompletionNPCSpawner] Chưa gán npcPrefab hoặc spawnPoint!");
        }
    }
}