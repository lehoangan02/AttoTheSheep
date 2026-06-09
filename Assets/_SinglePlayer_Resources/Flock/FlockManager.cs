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
    [SerializeField] private float skillRadiusMultiplier = 2f;

    [Header("Mốc Unlock Skill")]
    [SerializeField] private int lambsForSkill1 = 3;
    [SerializeField] private int lambsForSkill2 = 6;
    [SerializeField] private int lambsForSkill3 = 10;

    [Header("Auto Spawn Settings")]
    [SerializeField] private bool enableAutoSpawn = true;
    [SerializeField] private float autoSpawnInterval = 10f;
    private float spawnTimer = 0f;

    [Header("Regeneration Settings")]
    [SerializeField] private float healScale = 5f;
    [SerializeField] private float manaScale = 5f;

    public float HealScale => healScale;
    public float ManaScale => manaScale;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugRadius = true;

    public List<LambAI> activeLambs { get; private set; } = new List<LambAI>();
    public Vector2 currentFlockCenter { get; private set; }
    public float currentFlockRadius { get; private set; }
    public float currentSkillRadius { get; private set; }

    public event Action<int> OnFlockTierChanged;

    private PlayerController currentPlayer;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        currentFlockCenter = transform.position;
        SpawnInitialFlock();
    }

    void Update()
    {
        if (!IsServer) return;

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

        NetworkObject netObj = lambObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true);
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
        // Vùng hồi máu + giới hạn di chuyển của cừu
        currentFlockRadius = baseRadius + (radiusMultiplier * Mathf.Sqrt(activeLambs.Count));
        // Vùng cho phép dùng skill (rộng hơn)
        currentSkillRadius = currentFlockRadius * skillRadiusMultiplier;
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
            // Vùng hồi máu (màu vàng)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentFlockCenter, currentFlockRadius);
            // Vùng skill (màu xanh cyan, rộng hơn)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentFlockCenter, currentSkillRadius);
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
        return dist <= currentSkillRadius;
    }

    public override void OnNetworkDespawn()
    {
        if (currentPlayer != null)
        {
            currentPlayer.OnMapClicked -= HandleMapClicked;
        }
    }
}