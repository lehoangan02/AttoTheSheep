using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Netcode;

// --- CẤU TRÚC LƯU TRỮ THÔNG SỐ THEO LEVEL ---
[System.Serializable]
public struct FlockLevelConfig
{
    [Tooltip("Số lượng cừu tối đa ở Level này")]
    public int maxLambs;
    [Tooltip("Bán kính cơ bản (Base Radius) ở Level này")]
    public float baseRadius;
}

public class FlockManager : NetworkBehaviour
{
    [Header("Level Settings")]
    public NetworkVariable<int> currentLevel = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [Tooltip("Cấu hình số cừu và bán kính cho từng Level. Phần tử 0 = Level 1, Phần tử 1 = Level 2...")]
    [SerializeField] private FlockLevelConfig[] levelConfigs;

    [Header("Flock Settings")]
    [SerializeField] private GameObject lambPrefab;
    [SerializeField] private float radiusMultiplier = 0.5f; 

    [Header("Skill Zone (Radius 2 - Lớn hơn)")]
    [SerializeField] private float skillZoneRadiusMultiplier = 1.5f; // Bán kính kỹ năng = innerRadius * multiplier

    [Header("Skill Unlock Milestones")]
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
    public float currentFlockRadius { get; private set; } // Bán kính 1: Giới hạn di chuyển của cừu + hồi máu
    public float currentSkillZoneRadius { get; private set; } // Bán kính 2: Cho phép dùng skill

    [Header("Flock Buff Settings")]
    [SerializeField] private float healScale = 1f;
    [SerializeField] private float manaScale = 1f;
    public float HealScale => healScale;
    public float ManaScale => manaScale;

    public event Action<int> OnFlockTierChanged;

    private PlayerController currentPlayer; 

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Thiết lập giá trị mặc định cho mảng Level nếu bạn quên kéo trong Inspector
            if (levelConfigs == null || levelConfigs.Length == 0)
            {
                levelConfigs = new FlockLevelConfig[]
                {
                    new FlockLevelConfig { maxLambs = 3, baseRadius = 1.0f }, // Level 1
                    new FlockLevelConfig { maxLambs = 6, baseRadius = 1.5f }, // Level 2
                    new FlockLevelConfig { maxLambs = 10, baseRadius = 2.0f } // Level 3
                };
            }

            currentFlockCenter = transform.position;
            SpawnInitialFlock();
        }
    }

    void Update()
    {
        if (currentPlayer == null)
        {
            currentPlayer = FindFirstObjectByType<PlayerController>();
            if (currentPlayer != null)
            {
                currentPlayer.OnMapClicked += HandleMapClicked;
            }
        }

        if (enableAutoSpawn && IsServer)
        {
            spawnTimer += Time.deltaTime; 
            if (spawnTimer >= autoSpawnInterval)
            {
                spawnTimer = 0f; 
                SpawnLamb(currentFlockCenter);
            }
        }
    }

    // Lấy thông số (max cừu, base radius) của Level hiện tại
    private FlockLevelConfig GetCurrentLevelConfig()
    {
        // Trừ 1 vì Level 1 tương ứng với Index 0 trong mảng
        int index = Mathf.Clamp(currentLevel.Value - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index];
    }

    private void SpawnInitialFlock()
    {
        int startingLambs = GetCurrentLevelConfig().maxLambs;
        for (int i = 0; i < startingLambs; i++)
        {
            SpawnLamb(currentFlockCenter);
        }
        UpdateFlockRadius();
    }

    public void SpawnLamb(Vector2 position)
    {
        if (!IsServer) return;

        // --- ĐIỀU KIỆN CHẶN: Nếu số cừu đã bằng mức tối đa của Level thì không sinh thêm ---
        if (activeLambs.Count >= GetCurrentLevelConfig().maxLambs)
        {
            return;
        }

        GameObject lambObj = Instantiate(lambPrefab, position, Quaternion.identity);
        
        NetworkObject netObj = lambObj.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn(true);

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
        // Lấy Base Radius mở rộng tùy theo Level hiện tại
        float dynamicBaseRadius = GetCurrentLevelConfig().baseRadius;

        // Bán kính 1 = Base Radius (phụ thuộc Level) + Multiplier * Số lượng cừu
        currentFlockRadius = dynamicBaseRadius + (radiusMultiplier * Mathf.Sqrt(activeLambs.Count));
        
        // Bán kính 2: Cho phép dùng skill (rộng hơn)
        currentSkillZoneRadius = currentFlockRadius * skillZoneRadiusMultiplier;
        
        CommandFlock(currentFlockCenter);
    }

    // Gọi hàm này khi bạn muốn bầy cừu lên Level (từ Item, EXP, v.v...)
    public void LevelUpFlock()
    {
        if (!IsServer) return;

        if (currentLevel.Value < levelConfigs.Length)
        {
            currentLevel.Value++;
            UpdateFlockRadius();
            Debug.Log($"[FLOCK] Bầy cừu đã lên Level {currentLevel.Value}! Tối đa: {GetCurrentLevelConfig().maxLambs} cừu.");
        }
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
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(currentFlockCenter, currentFlockRadius);

            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireSphere(currentFlockCenter, currentSkillZoneRadius);
        }
    }

    public bool IsPositionInsideHealZone(Vector2 targetPosition)
    {
        float dist = Vector2.Distance(targetPosition, currentFlockCenter);
        return dist <= currentFlockRadius;
    }

    public bool IsPositionInsideSkillZone(Vector2 targetPosition)
    {
        float dist = Vector2.Distance(targetPosition, currentFlockCenter);
        return dist <= currentSkillZoneRadius;
    }

    public override void OnNetworkDespawn()
    {
        if (currentPlayer != null)
        {
            currentPlayer.OnMapClicked -= HandleMapClicked;
        }
    }
}