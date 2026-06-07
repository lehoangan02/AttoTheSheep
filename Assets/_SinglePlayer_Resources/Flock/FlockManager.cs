using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Netcode; // BẮT BUỘC THÊM THƯ VIỆN NÀY

// 1. ĐỔI THÀNH NetworkBehaviour
public class FlockManager : NetworkBehaviour
{
    [Header("Flock Settings")]
    [SerializeField] private GameObject lambPrefab;
    [SerializeField] private int initialLambCount = 5;
    [SerializeField] private float baseRadius = 1f;
    [SerializeField] private float radiusMultiplier = 0.5f; 

    [Header("Mốc Unlock Skill")]
    [SerializeField] private int lambsForSkill1 = 3;
    [SerializeField] private int lambsForSkill2 = 6;
    [SerializeField] private int lambsForSkill3 = 10;

    [Header("Auto Spawn Settings")]
    [SerializeField] private bool enableAutoSpawn = true;
    [SerializeField] private float autoSpawnInterval = 10f;
    private float spawnTimer = 0f; 

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugRadius = true;

    public List<LambAI> activeLambs { get; private set; } = new List<LambAI>();
    public Vector2 currentFlockCenter { get; private set; } 
    public float currentFlockRadius { get; private set; }

    public event Action<int> OnFlockTierChanged;

    private PlayerController currentPlayer; // Lưu trữ player để tránh tìm lại nhiều lần

    // 2. THAY HÀM Start() BẰNG OnNetworkSpawn()
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return; // Chỉ máy chủ (Host) mới được quản lý và đẻ cừu

        currentFlockCenter = transform.position;
        SpawnInitialFlock();
    }

    void Update()
    {
        if (!IsServer) return; // Client không chạy logic đẻ cừu

        // 3. TÌM PLAYER AN TOÀN TRONG UPDATE: Vì Player sinh ra sau FlockManager
        if (currentPlayer == null)
        {
            currentPlayer = FindFirstObjectByType<PlayerController>();
            if (currentPlayer != null)
            {
                currentPlayer.OnMapClicked += HandleMapClicked;
            }
        }

        if (enableAutoSpawn)
        {
            spawnTimer += Time.deltaTime; 
            if (spawnTimer >= autoSpawnInterval)
            {
                spawnTimer = 0f; 
                SpawnLamb(currentFlockCenter);
            }
        }
    }

    private void SpawnInitialFlock()
    {
        for (int i = 0; i < initialLambCount; i++)
        {
            SpawnLamb(currentFlockCenter);
        }
        UpdateFlockRadius();
    }

    public void SpawnLamb(Vector2 position)
    {
        GameObject lambObj = Instantiate(lambPrefab, position, Quaternion.identity);
        
        // 4. LỆNH CHÚ CHỐT: Bắt buộc phải Spawn Object này lên Network!
        NetworkObject netObj = lambObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true); // Đẻ ra trên mạng
        }
        else
        {
            Debug.LogError("LambPrefab của bạn chưa được gắn Component NetworkObject!");
            return;
        }

        LambAI lambAI = lambObj.GetComponent<LambAI>();
        if (lambAI != null)
        {
            lambAI.Initialize(this); 
            activeLambs.Add(lambAI);
            UpdateFlockRadius();
            OnFlockTierChanged?.Invoke(GetFlockTier());
        }
    }

    public void RemoveLamb(LambAI lamb)
    {
        if (activeLambs.Contains(lamb))
        {
            activeLambs.Remove(lamb);
            UpdateFlockRadius();
            OnFlockTierChanged?.Invoke(GetFlockTier());
        }
    }

    private void UpdateFlockRadius()
    {
        currentFlockRadius = baseRadius + (radiusMultiplier * Mathf.Sqrt(activeLambs.Count));
        CommandFlock(currentFlockCenter);
    }

    private void HandleMapClicked(Vector2 targetPos)
    {
        CommandFlock(targetPos);
    }

    private void CommandFlock(Vector2 targetPos)
    {
        currentFlockCenter = targetPos;

        foreach (var lamb in activeLambs)
        {
            if (lamb != null && lamb.gameObject.activeInHierarchy)
            {
                lamb.SetFlockData(currentFlockCenter, currentFlockRadius);
            }
        }
    }

    public int GetFlockTier()
    {
        if (activeLambs.Count >= lambsForSkill3) return 3;
        if (activeLambs.Count >= lambsForSkill2) return 2;
        if (activeLambs.Count >= lambsForSkill1) return 1;
        return 0; 
    }

    private void OnDrawGizmos()
    {
        if (showDebugRadius)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentFlockCenter, currentFlockRadius);
        }
    }

    public bool IsPositionInsideFlock(Vector2 targetPosition)
    {
        float dist = Vector2.Distance(targetPosition, currentFlockCenter);
        return dist <= currentFlockRadius;
    }

    public override void OnNetworkDespawn()
    {
        if (currentPlayer != null)
        {
            currentPlayer.OnMapClicked -= HandleMapClicked;
        }
    }
}