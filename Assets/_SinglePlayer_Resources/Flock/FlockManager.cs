using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Netcode;

[System.Serializable]
public struct FlockLevelConfig
{
    public int maxLambs;
    public float baseRadius;
}

public enum FlockControlMode
{
    Manual,
    Auto   
}

public class FlockManager : NetworkBehaviour
{
    [Header("=== HARDCODED SETTINGS (CHỈNH SỬA TẠI ĐÂY) ===")]
    [Tooltip("SỬA SỐ NÀY TRONG CODE ĐỂ ĐỔI LEVEL KHỞI ĐẦU KHI RUN: 1, 2 hoặc 3")]
    private const int HARDCODED_STARTING_LEVEL = 3; 
    
    // Hardcode các mốc kích hoạt Kỹ năng (Skill Milestones)
    private const int lambsForSkill1 = 3;
    private const int lambsForSkill2 = 6;
    private const int lambsForSkill3 = 10;
    private const int MAX_LEVEL = 3;

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

    [Header("Auto Spawn Settings")]
    [SerializeField] private bool enableAutoSpawn = true;
    [SerializeField] private float autoSpawnInterval = 10f;
    private float spawnTimer = 0f; 

    [Header("Spawn Validation (Chống kẹt tường/quái)")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float spawnCheckRadius = 0.4f;
    [SerializeField] private int maxSpawnAttempts = 10;

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

    // Các biến đồng bộ Network và dữ liệu Runtime
    [HideInInspector]
    public NetworkVariable<int> currentLevel = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
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
    private PlayerSkills currentPlayerSkills; 

    // ==========================================
    // LOGIC HARDCODE CẤU HÌNH LEVEL
    // ==========================================
    public FlockLevelConfig GetCurrentLevelConfig()
    {
        switch (currentLevel.Value)
        {
            case 1:
                return new FlockLevelConfig { maxLambs = 3, baseRadius = 1.0f };
            case 2:
                return new FlockLevelConfig { maxLambs = 6, baseRadius = 1.5f };
            case 3:
            default:
                return new FlockLevelConfig { maxLambs = 10, baseRadius = 2.0f };
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentLevel.Value = HARDCODED_STARTING_LEVEL;

            flockDestination = transform.position;
            currentFlockCenter.Value = transform.position;
            SpawnInitialFlock();
        }
    }

    void Update()
    {
        if (currentPlayer == null)
        {
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            if (players.Length == 1)
            {
                currentPlayer = players[0];
            }
            else
            {
                foreach (var p in players)
                {
                    if (p.OwnerClientId == OwnerClientId)
                    {
                        currentPlayer = p;
                        break;
                    }
                }
            }

            if (currentPlayer != null)
            {
                currentPlayer.OnMapClicked += HandleMapClicked;
                lastPlayerAnchorPos = currentPlayer.transform.position;
                
                currentPlayerSkills = currentPlayer.GetComponent<PlayerSkills>();
                if (currentPlayerSkills == null) 
                    currentPlayerSkills = currentPlayer.GetComponentInChildren<PlayerSkills>();
            }
        }

        if (IsServer)
        {
            if (currentControlMode == FlockControlMode.Auto && currentPlayer != null)
            {
                HandleAutoFollow();
            }

            UpdateActualFlockCenter();

            // CHỖ THAY ĐỔI: Đồng bộ số lượng cừu thực tế sang PlayerSkills mới
            if (currentPlayerSkills != null)
            {
                int currentLambsCount = activeLambs.Count; // Lấy tổng số cừu thực tế hiện tại
                if (currentPlayerSkills.currentLambCount.Value != currentLambsCount)
                {
                    currentPlayerSkills.currentLambCount.Value = currentLambsCount; // Gán vào biến mới
                }

                bool isInside = IsPositionInsideSkillZone(currentPlayer.transform.position);
                if (currentPlayerSkills.isInsideFlock.Value != isInside)
                {
                    currentPlayerSkills.isInsideFlock.Value = isInside;
                }
            }

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

    private void CheckLambsOutOfBounds()
    {
        Vector2 center = flockDestination;

        foreach (var lamb in activeLambs)
        {
            if (lamb != null && lamb.gameObject.activeInHierarchy)
            {
                float dist = Vector2.Distance(lamb.transform.position, center);
                if (dist > currentFlockRadius)
                {
                    lamb.NotifyOutOfBounds(center);
                }
            }
        }
    }

    public void ReportLambAttacked(LambAI attackedLamb)
    {
        if (!IsServer) return;

        attackedLamb.TriggerPanic(panicDuration);

        foreach (var lamb in activeLambs)
        {
            if (lamb != null && lamb != attackedLamb && lamb.gameObject.activeInHierarchy)
            {
                float dist = Vector2.Distance(attackedLamb.transform.position, lamb.transform.position);
                if (dist <= panicAlertRadius)
                {
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

    public event Action<FlockControlMode> OnControlModeChanged;

    public void RequestToggleControlMode()
    {
        ToggleControlModeServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleControlModeServerRpc()
    {
        FlockControlMode nextMode = (currentControlMode == FlockControlMode.Auto) ? FlockControlMode.Manual : FlockControlMode.Auto;
        SetControlMode(nextMode);
        SyncControlModeClientRpc(nextMode);
    }

    [ClientRpc]
    private void SyncControlModeClientRpc(FlockControlMode newMode)
    {
        currentControlMode = newMode;
        OnControlModeChanged?.Invoke(newMode);
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

        if (currentLevel.Value < MAX_LEVEL)
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