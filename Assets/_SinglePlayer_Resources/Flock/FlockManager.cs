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

public enum FlockControlMode
{
    Manual, // Click đâu đi đó
    Auto    // Tự động bám theo player với Deadzone
}

public class FlockManager : NetworkBehaviour
{
    [Header("Level Settings")]
    public NetworkVariable<int> currentLevel = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [Tooltip("Cấu hình số cừu và bán kính cho từng Level. Phần tử 0 = Level 1, Phần tử 1 = Level 2...")]
    [SerializeField] private FlockLevelConfig[] levelConfigs;

    [Header("Control Settings")]
    public FlockControlMode currentControlMode = FlockControlMode.Auto;

    [Header("Auto Follow Settings (AI)")]
    [Tooltip("Khoảng cách tối đa Player có thể di chuyển trước khi bầy cừu đi theo")]
    [SerializeField] private float playerDeadzoneRadius = 3f;
    [Tooltip("Bán kính random tâm mới xung quanh Player (Tạo sự tự nhiên)")]
    [SerializeField] private float randomOffsetRadius = 2f;

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

    // ==========================================
    // TÍNH NĂNG MỚI: BỘ LỌC SPAWN
    // ==========================================
    [Header("Spawn Validation (Chống kẹt tường/quái)")]
    [Tooltip("Các Layer không được phép spawn đè lên (VD: Wall, Obstacle, Enemy)")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("Kích thước vùng kiểm tra (Thường bằng bán kính của collider cừu)")]
    [SerializeField] private float spawnCheckRadius = 0.4f;
    [Tooltip("Số lần thử tìm vị trí trống tối đa trước khi hủy lệnh spawn lần đó")]
    [SerializeField] private int maxSpawnAttempts = 10;

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
        // Liên tục kiểm tra và lấy tham chiếu đến Player (cần chỉnh sửa nếu có nhiều Player)
        if (currentPlayer == null)
        {
            currentPlayer = FindFirstObjectByType<PlayerController>();
            if (currentPlayer != null)
            {
                currentPlayer.OnMapClicked += HandleMapClicked;
            }
        }

        if (IsServer)
        {
            // Xử lý logic AI đi theo
            if (currentControlMode == FlockControlMode.Auto && currentPlayer != null)
            {
                HandleAutoFollow();
            }

            // Xử lý sinh cừu tự động
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
    }

    // ==========================================
    // AUTO FOLLOW (AI) LOGIC
    // ==========================================
    private void HandleAutoFollow()
    {
        Vector2 playerPos = currentPlayer.transform.position;
        float distanceToPlayer = Vector2.Distance(currentFlockCenter, playerPos);

        // Nếu người chơi vượt ra khỏi Deadzone
        if (distanceToPlayer > playerDeadzoneRadius)
        {
            // Tạo một vị trí random xung quanh player
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * randomOffsetRadius;
            Vector2 newFlockCenter = playerPos + randomOffset;
            
            CommandFlock(newFlockCenter);
        }
    }

    // Bạn có thể gọi hàm này từ UI/Input để đổi trạng thái
    public void SetControlMode(FlockControlMode newMode)
    {
        currentControlMode = newMode;
        if (newMode == FlockControlMode.Auto && currentPlayer != null)
        {
            // Buộc cập nhật ngay lập tức nếu chuyển sang Auto
            HandleAutoFollow(); 
        }
    }

    // ==========================================
    // CORE FLOCK LOGIC
    // ==========================================
    private FlockLevelConfig GetCurrentLevelConfig()
    {
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

    public void SpawnLamb(Vector2 centerPosition)
    {
        if (!IsServer) return;

        if (activeLambs.Count >= GetCurrentLevelConfig().maxLambs)
        {
            return;
        }

        // TÌM VỊ TRÍ HỢP LỆ TRƯỚC KHI SPAWN
        if (!TryGetValidSpawnPosition(centerPosition, currentFlockRadius, out Vector2 spawnPos))
        {
            Debug.LogWarning("⚠️ [FlockManager] Không tìm được vị trí trống để spawn cừu! Hủy spawn lần này để tránh kẹt tường.");
            return; // Hủy spawn nếu không có chỗ trống
        }

        GameObject lambObj = Instantiate(lambPrefab, spawnPos, Quaternion.identity);
        
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

    /// <summary>
    /// Thử tìm vị trí ngẫu nhiên không đè lên chướng ngại vật.
    /// </summary>
    private bool TryGetValidSpawnPosition(Vector2 center, float maxRadius, out Vector2 validPosition)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            // Lấy 1 điểm random xung quanh tâm bầy
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * maxRadius;
            Vector2 testPosition = center + randomOffset;

            // Kiểm tra xem vị trí này có bị đụng tường/quái (obstacleLayer) không
            Collider2D hit = Physics2D.OverlapCircle(testPosition, spawnCheckRadius, obstacleLayer);
            
            if (hit == null)
            {
                // Vị trí an toàn, trả về true
                validPosition = testPosition;
                return true;
            }
        }
        
        // Đã thử quá giới hạn số lần mà vẫn toàn vướng tường -> Trả về false
        validPosition = center;
        return false;
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
        float dynamicBaseRadius = GetCurrentLevelConfig().baseRadius;
        currentFlockRadius = dynamicBaseRadius + (radiusMultiplier * Mathf.Sqrt(activeLambs.Count));
        currentSkillZoneRadius = currentFlockRadius * skillZoneRadiusMultiplier;
        
        CommandFlock(currentFlockCenter);
    }

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
        // Tự động chuyển qua chế độ Manual khi người chơi ra lệnh
        currentControlMode = FlockControlMode.Manual;
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
            // Vẽ tâm của bầy
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(currentFlockCenter, currentFlockRadius);

            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireSphere(currentFlockCenter, currentSkillZoneRadius);

            // Vẽ Deadzone của Player (nếu ở chế độ Auto)
            if (Application.isPlaying && currentPlayer != null && currentControlMode == FlockControlMode.Auto)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f); // Màu đỏ nhạt cho Deadzone
                Gizmos.DrawWireSphere(currentPlayer.transform.position, playerDeadzoneRadius);
            }
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