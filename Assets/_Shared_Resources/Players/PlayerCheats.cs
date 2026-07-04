using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public enum CheatBuffType
{
    ShieldAllLambs,
    SpawnMaxLambs,
    SkillDamageMultiplier,
    SpeedBoostPlayerAndFlock
}

[Serializable]
public class CheatBuffConfig
{
    public CheatBuffType buffType = CheatBuffType.ShieldAllLambs;

    [Tooltip("Multiplier value for damage/speed buffs. Ignored by spawn and shield.")]
    public float value = 1.25f;

    [Min(0f)]
    [Tooltip("Duration in seconds. Ignored by SpawnMaxLambs.")]
    public float duration = 8f;

    [Min(0f)]
    [Tooltip("Extra value for specific buffs (for speed boost = acceleration duration).")]
    public float extraValue = 0.5f;
}

[Serializable]
public class CheatSlotConfig
{
    [Range(1, 9)] public int cheatId = 1;
    public string cheatName = "Cheat";
    public List<CheatBuffConfig> buffs = new List<CheatBuffConfig>();
}

/// <summary>
/// Applies cheat effects by slot id. Each slot can contain multiple buffs
/// and each buff can define its own value and duration.
/// </summary>
public class PlayerCheats : NetworkBehaviour
{
    private PlayerController controller;
    private PlayerMovement playerMovement;
    private FlockManager flockManager;
    private PlayerSkills playerSkills;

    [Header("Cheat Slots")]
    [SerializeField] private List<CheatSlotConfig> cheatSlots = new List<CheatSlotConfig>
    {
        new CheatSlotConfig
        {
            cheatId = 1,
            cheatName = "Flock Shield",
            buffs = new List<CheatBuffConfig>
            {
                new CheatBuffConfig
                {
                    buffType = CheatBuffType.ShieldAllLambs,
                    duration = 10f
                }
            }
        },
        new CheatSlotConfig
        {
            cheatId = 2,
            cheatName = "Spawn Max Lambs",
            buffs = new List<CheatBuffConfig>
            {
                new CheatBuffConfig
                {
                    buffType = CheatBuffType.SpawnMaxLambs
                }
            }
        },
        new CheatSlotConfig
        {
            cheatId = 3,
            cheatName = "Skill Damage Boost",
            buffs = new List<CheatBuffConfig>
            {
                new CheatBuffConfig
                {
                    buffType = CheatBuffType.SkillDamageMultiplier,
                    value = 1.25f
                }
            }
        },
        new CheatSlotConfig
        {
            cheatId = 4,
            cheatName = "Speed Boost",
            buffs = new List<CheatBuffConfig>
            {
                new CheatBuffConfig
                {
                    buffType = CheatBuffType.SpeedBoostPlayerAndFlock,
                    value = 1.5f,
                    duration = 8f,
                    extraValue = 0.5f
                }
            }
        }
    };

    void Awake()
    {
        ResolveReferences();
    }

    public override void OnNetworkSpawn()
    {
        ResolveReferences();
        if (IsOwner && controller != null)
        {
            controller.OnCheatActivated += HandleCheatActivated;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (controller != null)
        {
            controller.OnCheatActivated -= HandleCheatActivated;
        }
    }

    private void ResolveReferences()
    {
        controller = GetComponent<PlayerController>();
        if (controller == null) controller = GetComponentInChildren<PlayerController>();
        if (controller == null) controller = GetComponentInParent<PlayerController>();

        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null) playerMovement = GetComponentInChildren<PlayerMovement>();
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();

        playerSkills = GetComponent<PlayerSkills>();
        if (playerSkills == null) playerSkills = GetComponentInChildren<PlayerSkills>();
        if (playerSkills == null) playerSkills = GetComponentInParent<PlayerSkills>();
    }

    private FlockManager GetFlockManager()
    {
        if (flockManager == null)
        {
            flockManager = FindFirstObjectByType<FlockManager>();
        }

        return flockManager;
    }

    private CheatSlotConfig FindSlot(int cheatId)
    {
        return cheatSlots.Find(slot => slot != null && slot.cheatId == cheatId);
    }

    private void HandleCheatActivated(int cheatId)
    {
        ActivateCheat(cheatId);
    }

    public void ActivateCheat(int cheatId)
    {
        if (!IsOwner) return;

        CheatSlotConfig slot = FindSlot(cheatId);
        if (slot == null)
        {
            Debug.LogWarning($"[Cheats] No slot configured for Cheat ID {cheatId}.");
            return;
        }

        ApplyCheatSlotServerRpc(cheatId);
    }

    [ServerRpc]
    private void ApplyCheatSlotServerRpc(int cheatId)
    {
        ResolveReferences();

        CheatSlotConfig slot = FindSlot(cheatId);
        if (slot == null)
        {
            Debug.LogWarning($"[Cheats] Server missing slot config for Cheat ID {cheatId}.");
            return;
        }

        if (slot.buffs == null || slot.buffs.Count == 0)
        {
            Debug.LogWarning($"[Cheats] Slot '{slot.cheatName}' has no buffs configured.");
            return;
        }

        foreach (CheatBuffConfig buff in slot.buffs)
        {
            if (buff == null) continue;
            ApplyBuff(buff);
        }
    }

    private void ApplyBuff(CheatBuffConfig buff)
    {
        switch (buff.buffType)
        {
            case CheatBuffType.ShieldAllLambs:
                ApplyShieldAllLambs(buff.duration);
                break;

            case CheatBuffType.SpawnMaxLambs:
                ApplySpawnMaxLambs();
                break;

            case CheatBuffType.SkillDamageMultiplier:
                ApplySkillDamageMultiplier(buff.value);
                break;

            case CheatBuffType.SpeedBoostPlayerAndFlock:
                ApplySpeedBoost(buff.value, buff.duration, buff.extraValue);
                break;
        }
    }

    private void ApplyShieldAllLambs(float duration)
    {
        FlockManager fm = GetFlockManager();
        if (fm == null)
        {
            Debug.LogWarning("[Cheats] No FlockManager found for shield buff.");
            return;
        }

        float finalDuration = Mathf.Max(0f, duration);
        foreach (LambAI lamb in fm.activeLambs)
        {
            if (lamb != null && lamb.gameObject.activeInHierarchy)
            {
                lamb.SetShieldedState(true, finalDuration);
            }
        }

        Debug.Log($"[Cheats] Applied shield to all lambs for {finalDuration:0.##}s.");
    }

    private void ApplySpawnMaxLambs()
    {
        FlockManager fm = GetFlockManager();
        if (fm == null)
        {
            Debug.LogWarning("[Cheats] No FlockManager found for spawn buff.");
            return;
        }

        int maxLambs = fm.GetCurrentLevelConfig().maxLambs;
        int currentCount = fm.activeLambs.Count;

        for (int i = currentCount; i < maxLambs; i++)
        {
            fm.SpawnLamb(fm.currentFlockCenter.Value);
        }

        Debug.Log($"[Cheats] Spawn buff applied: {currentCount} -> {fm.activeLambs.Count} lambs.");
    }

    private void ApplySkillDamageMultiplier(float multiplier)
    {
        if (playerSkills == null)
        {
            Debug.LogWarning("[Cheats] PlayerSkills not found; cannot apply damage multiplier.");
            return;
        }

        float finalMultiplier = Mathf.Max(0f, multiplier);
        playerSkills.damageMultiplier.Value = finalMultiplier;
        Debug.Log($"[Cheats] Damage multiplier set to {finalMultiplier:0.##}x.");
    }

    private void ApplySpeedBoost(float multiplier, float duration, float accelerationDuration)
    {
        float finalMultiplier = Mathf.Max(0f, multiplier);
        float finalDuration = Mathf.Max(0f, duration);
        float finalAccelerationDuration = Mathf.Max(0f, accelerationDuration);

        if (playerMovement != null)
        {
            playerMovement.ApplyTemporarySpeedMultiplier(finalMultiplier, finalDuration, finalAccelerationDuration);
        }

        FlockManager fm = GetFlockManager();
        if (fm != null)
        {
            foreach (LambAI lamb in fm.activeLambs)
            {
                if (lamb != null && lamb.gameObject.activeInHierarchy)
                {
                    lamb.ApplySpeedBoost(finalMultiplier, finalDuration);
                }
            }
        }

        Debug.Log($"[Cheats] Speed boost applied. Multiplier={finalMultiplier:0.##}, Duration={finalDuration:0.##}s.");
    }
}