using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Netcode;

public class FlockManager : NetworkBehaviour
{
    [Header("Flock Settings")]
    [SerializeField] private GameObject lambPrefab;
    [SerializeField] private int initialLambCount = 5;
    [SerializeField] private float baseRadius = 1f;
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

    private PlayerController currentPlayer; // Store player reference to avoid redundant searches

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
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
        if (!IsServer) return;

        GameObject lambObj = Instantiate(lambPrefab, position, Quaternion.identity);
        
        // Register the lamb with the network so it gets a unique ID and doesn't crash the scene sweep
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
        // Bán kính 1: Giới hạn di chuyển của bầy cừu + hồi máu
        currentFlockRadius = baseRadius + (radiusMultiplier * Mathf.Sqrt(activeLambs.Count));
        // Bán kính 2: Cho phép dùng skill (rộng hơn)
        currentSkillZoneRadius = currentFlockRadius * skillZoneRadiusMultiplier;
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
                // Cừu di chuyển theo bán kính 1 (inner radius)
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
            // Bán kính 1 - Vùng cừu di chuyển + hồi máu (màu xanh lá)
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(currentFlockCenter, currentFlockRadius);

            // Bán kính 2 - Vùng dùng skill (màu xanh dương)
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireSphere(currentFlockCenter, currentSkillZoneRadius);
        }
    }

    /// <summary>
    /// Bán kính 1: Kiểm tra player có ở trong vùng hồi máu + mana không
    /// </summary>
    public bool IsPositionInsideHealZone(Vector2 targetPosition)
    {
        float dist = Vector2.Distance(targetPosition, currentFlockCenter);
        return dist <= currentFlockRadius;
    }

    /// <summary>
    /// Bán kính 2: Kiểm tra player có ở trong vùng cho phép dùng skill không
    /// </summary>
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