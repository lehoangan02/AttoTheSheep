using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Netcode;

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
    Manual,
    Auto   
}

public class FlockManager : NetworkBehaviour
{
    [Header("Level Settings")]
    public NetworkVariable<int> currentLevel = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [SerializeField] private FlockLevelConfig[] levelConfigs;

    [Header("Control Settings")]
    public FlockControlMode currentControlMode = FlockControlMode.Auto;

    [Header("Auto Follow Settings (AI)")]
    [SerializeField] private float playerDeadzoneRadius = 3f;
    [SerializeField] private float randomOffsetRadius = 2f;

    [Header("Flock Settings")]
    [SerializeField] private GameObject lambPrefab;
    [SerializeField] private float radiusMultiplier = 0.5f; 

    [Header("Skill Zone (Radius 2 - Lớn hơn)")]
    [SerializeField] private float skillZoneRadiusMultiplier = 1.5f; 

    [Header("Skill Unlock Milestones")]
    [SerializeField] private int lambsForSkill1 = 3;
    [SerializeField] private int lambsForSkill2 = 6;
    [SerializeField] private int lambsForSkill3 = 10;

    [Header("Auto Spawn Settings")]
    [SerializeField] private bool enableAutoSpawn = true;
    [SerializeField] private float autoSpawnInterval = 10f;
    private float spawnTimer = 0f; 

    [Header("Spawn Validation (Chống kẹt tường/quái)")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float spawnCheckRadius = 0.4f;
    [SerializeField] private int maxSpawnAttempts = 10;

    // ==========================================
    // TÍNH NĂNG MỚI: CẤU HÌNH CHIẾN ĐẤU & BỊ ĐẨY
    // ==========================================
    [Header("Combat & Push Settings")]
    [Tooltip("Thời gian cừu chạy loạn xạ khi bị tấn công")]
    [SerializeField] private float panicDuration = 3f;
    [Tooltip("Bán kính lan truyền hoảng loạn sang các con cừu bên cạnh")]
    [SerializeField] private float panicAlertRadius = 2.5f;
    [Tooltip("Tần suất (giây) kiểm tra cừu bị đẩy ra ngoài bán kính bầy")]
    [SerializeField] private float oobCheckInterval = 0.2f;
    private float oobCheckTimer = 0f;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugRadius = true;

    public List<LambAI> activeLambs { get; private set; } = new List<LambAI>();
    
    public NetworkVariable<Vector2> currentFlockCenter = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); 
    
    public float currentFlockRadius { get; private set; } 
    public float currentSkillZoneRadius { get; private set; } 

    [Header("Flock Buff Settings")]
    [SerializeField] private float healScale = 1f;
    [SerializeField] private float manaScale = 1f;
    public float HealScale => healScale;
    public float ManaScale => manaScale;

    public event Action<int> OnFlockTierChanged;

    private PlayerController currentPlayer; 
    private Vector2 lastPlayerAnchorPos; 
    private Vector2 flockDestination;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (levelConfigs == null || levelConfigs.Length == 0)
            {
                levelConfigs = new FlockLevelConfig[]
                {
                    new FlockLevelConfig { maxLambs = 3, baseRadius = 1.0f },
                    new FlockLevelConfig { maxLambs = 6, baseRadius = 1.5f },
                    new FlockLevelConfig { maxLambs = 10, baseRadius = 2.0f }
                };
            }

            flockDestination = transform.position;
            currentFlockCenter.Value = transform.position;
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
                lastPlayerAnchorPos = currentPlayer.transform.position;
            }
        }

        if (IsServer)
        {
            if (currentControlMode == FlockControlMode.Auto && currentPlayer != null)
            {
                HandleAutoFollow();
            }

            UpdateActualFlockCenter();

            // TÍNH NĂNG MỚI: Quét định kỳ kiểm tra xem có con cừu nào bị đẩy ra rìa không
            oobCheckTimer += Time.deltaTime;
            if (oobCheckTimer >= oobCheckInterval)
            {
                oobCheckTimer = 0f;
                CheckLambsOutOfBounds();
            }

            if (enableAutoSpawn)
            {
                spawnTimer += Time.deltaTime; 
                if (spawnTimer >= autoSpawnInterval)
                {
                    spawnTimer = 0f; 
                    SpawnLamb(currentFlockCenter.Value);
                }
            }
        }
    }

    private void UpdateActualFlockCenter()
    {
        if (activeLambs == null || activeLambs.Count == 0)
        {
            currentFlockCenter.Value = flockDestination;
            return;
        }

        Vector2 sumPosition = Vector2.zero;
        int validCount = 0;

        for (int i = 0; i < activeLambs.Count; i++)
        {
            if (activeLambs[i] != null && activeLambs[i].gameObject.activeInHierarchy)
            {
                sumPosition += (Vector2)activeLambs[i].transform.position;
                validCount++;
            }
        }

        if (validCount > 0)
        {
            currentFlockCenter.Value = sumPosition / validCount;
        }
        else
        {
            currentFlockCenter.Value = flockDestination;
        }
    }

    // ==========================================
    // LOGIC 1: PHÁT HIỆN CỪU BỊ ĐẨY KHỎI BÁN KÍNH
    // ==========================================
    private void CheckLambsOutOfBounds()
    {
        Vector2 center = flockDestination;

        foreach (var lamb in activeLambs)
        {
            if (lamb != null && lamb.gameObject.activeInHierarchy)
            {
                float dist = Vector2.Distance(lamb.transform.position, center);
                
                // Nếu khoảng cách lớn hơn bán kính bầy hiện tại
                if (dist > currentFlockRadius)
                {
                    // Phát lệnh ép buộc cừu quay trở về bầy lập tức
                    lamb.NotifyOutOfBounds(center);
                }
            }
        }
    }

    // ==========================================
    // LOGIC 2: KÍCH HOẠT KHI BẦY CỪU BỊ TẤN CÔNG
    // ==========================================
    /// <summary>
    /// Hàm này được gọi từ script nhận sát thương của con cừu (ví dụ: LambHealth hoặc chính LambAI)
    /// </summary>
    public void ReportLambAttacked(LambAI attackedLamb)
    {
        if (!IsServer) return;

        // 1. Cho con cừu bị dính đòn hoảng loạn trước
        attackedLamb.TriggerPanic(panicDuration);

        // 2. Hiệu ứng đám đông: Lan truyền sự sợ hãi sang các con cừu lân cận
        foreach (var lamb in activeLambs)
        {
            if (lamb != null && lamb != attackedLamb && lamb.gameObject.activeInHierarchy)
            {
                float dist = Vector2.Distance(attackedLamb.transform.position, lamb.transform.position);
                if (dist <= panicAlertRadius)
                {
                    // Cừu xung quanh hoảng loạn ngắn hơn một chút (70% thời gian gốc)
                    lamb.TriggerPanic(panicDuration * 0.7f); 
                }
            }
        }
    }

    private void HandleAutoFollow()
    {
        Vector2 playerPos = currentPlayer.transform.position;
        float distancePlayerMoved = Vector2.Distance(lastPlayerAnchorPos, playerPos);

        if (distancePlayerMoved > playerDeadzoneRadius)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * randomOffsetRadius;
            Vector2 newFlockDestination = playerPos + randomOffset;
            
            CommandFlock(newFlockDestination);
            lastPlayerAnchorPos = playerPos;
        }
    }

    public void SetControlMode(FlockControlMode newMode)
    {
        currentControlMode = newMode;
        if (newMode == FlockControlMode.Auto && currentPlayer != null)
        {
            lastPlayerAnchorPos = currentPlayer.transform.position;
            HandleAutoFollow(); 
        }
    }

    // Thêm Sự kiện để UI có thể đăng ký lắng nghe và thay đổi Text/Màu sắc nút
    public event Action<FlockControlMode> OnControlModeChanged;

    /// <summary>
    /// Hàm dành cho UI Button gọi để yêu cầu đổi chế độ
    /// </summary>
    public void RequestToggleControlMode()
    {
        // Gửi yêu cầu lên Server
        ToggleControlModeServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleControlModeServerRpc()
    {
        // Server tính toán chế độ tiếp theo
        FlockControlMode nextMode = (currentControlMode == FlockControlMode.Auto) ? FlockControlMode.Manual : FlockControlMode.Auto;
        
        // Thực thi thay đổi trên Server
        SetControlMode(nextMode);

        // Phát lệnh đồng bộ trạng thái xuống toàn bộ Client
        SyncControlModeClientRpc(nextMode);
    }

    [ClientRpc]
    private void SyncControlModeClientRpc(FlockControlMode newMode)
    {
        currentControlMode = newMode;
        
        // Kích hoạt sự kiện để giao diện cập nhật theo thay đổi mới
        OnControlModeChanged?.Invoke(newMode);
    }

    public FlockLevelConfig GetCurrentLevelConfig()
    {
        int index = Mathf.Clamp(currentLevel.Value - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index];
    }

    private void SpawnInitialFlock()
    {
        int startingLambs = GetCurrentLevelConfig().maxLambs;
        for (int i = 0; i < startingLambs; i++)
        {
            SpawnLamb(flockDestination);
        }
        UpdateFlockRadius();
    }

    public LambAI SpawnLamb(Vector2 centerPosition)
    {
        if (!IsServer) return null;

        if (activeLambs.Count >= GetCurrentLevelConfig().maxLambs) return null;

        if (!TryGetValidSpawnPosition(centerPosition, currentFlockRadius, out Vector2 spawnPos))
        {
            Debug.LogWarning("⚠️ [FlockManager] Không tìm được vị trí trống để spawn cừu!");
            return null; 
        }

        GameObject lambObj = Instantiate(lambPrefab, spawnPos, Quaternion.identity);
        
        NetworkObject netObj = lambObj.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn(true);

        LambAI lambAI = lambObj.GetComponent<LambAI>();
        if (lambAI != null)
        {
            lambAI.Initialize(this, obstacleLayer);
            activeLambs.Add(lambAI);
            UpdateFlockRadius();
            OnFlockTierChanged?.Invoke(GetFlockTier());
        }

        return lambAI;
    }

    private bool TryGetValidSpawnPosition(Vector2 center, float maxRadius, out Vector2 validPosition)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * maxRadius;
            Vector2 testPosition = center + randomOffset;

            Collider2D hit = Physics2D.OverlapCircle(testPosition, spawnCheckRadius, obstacleLayer);
            if (hit == null)
            {
                validPosition = testPosition;
                return true;
            }
        }
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
        
        CommandFlock(flockDestination);
    }

    public void LevelUpFlock()
    {
        if (!IsServer) return;

        if (currentLevel.Value < levelConfigs.Length)
        {
            currentLevel.Value++;
            UpdateFlockRadius();
        }
    }

    private void HandleMapClicked(Vector2 targetPos)
    {
        CommandFlock(targetPos);

        if (currentControlMode == FlockControlMode.Auto && currentPlayer != null)
        {
            lastPlayerAnchorPos = currentPlayer.transform.position;
        }
    }

    private void CommandFlock(Vector2 targetPos)
    {
        flockDestination = targetPos;

        foreach (var lamb in activeLambs)
        {
            if (lamb != null && lamb.gameObject.activeInHierarchy)
            {
                lamb.SetFlockData(flockDestination, currentFlockRadius);
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
            Gizmos.DrawWireSphere(currentFlockCenter.Value, currentFlockRadius);

            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireSphere(currentFlockCenter.Value, currentSkillZoneRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(flockDestination, 0.2f);

            if (Application.isPlaying && currentPlayer != null && currentControlMode == FlockControlMode.Auto)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f); 
                Gizmos.DrawWireSphere(lastPlayerAnchorPos, playerDeadzoneRadius);
            }
        }
    }

    public bool IsPositionInsideHealZone(Vector2 targetPosition)
    {
        float dist = Vector2.Distance(targetPosition, currentFlockCenter.Value);
        return dist <= currentFlockRadius;
    }

    public bool IsPositionInsideSkillZone(Vector2 targetPosition)
    {
        float dist = Vector2.Distance(targetPosition, currentFlockCenter.Value);
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
