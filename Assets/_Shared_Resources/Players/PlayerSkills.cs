using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

[System.Serializable]
public class SkillSlot
{
    public string slotName = "New Skill"; 
    public SkillData data;                
    public BaseSkillComponent logicScript;
}

public class PlayerSkills : NetworkBehaviour
{
    public static event Action<int, float> OnSkillCooldownStarted;
    private PlayerController controller;
    private NetworkEntity entity;

    // Flock tier (0: No flock, 1: 3 lambs, 2: 6 lambs, 3: 10 lambs)
    public NetworkVariable<int> unlockedSkillTier = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Is the player standing within the flock's radius
    public NetworkVariable<bool> isInsideFlock = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Damage multiplier for all skills (1.0 = normal, 1.25 = +25% boost)
    public NetworkVariable<float> damageMultiplier = new NetworkVariable<float>(1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Equipped Skill Board (Drag and drop here)")]
    public List<SkillSlot> equippedSkills = new List<SkillSlot>();

    private Dictionary<int, float> lastCastTimes = new Dictionary<int, float>();

    void Awake()
    {
        controller = GetComponentInParent<PlayerController>();
        if (controller == null) controller = GetComponentInChildren<PlayerController>();
        
        entity = GetComponentInParent<NetworkEntity>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (controller != null) controller.OnSkillActivated += TryCastSkill;

            // Tìm SkillBoardUI trên Scene và nạp dữ liệu 4 skills vào
            SkillBoardUI boardUI = FindFirstObjectByType<SkillBoardUI>();
            if (boardUI != null)
            {
                boardUI.InitializeSkillBoard(this);
            }
        }
    }

    private SkillSlot GetSkillSlot(int skillId)
    {
        return equippedSkills.Find(slot => slot.data != null && slot.data.skillId == skillId);
    }

    // Base skill (always available: 0 = Headbutt)
    private bool IsBaseSkill(int skillId)
    {
        return skillId == 0;
    }

    private void TryCastSkill(int skillId)
    {
        SkillSlot slot = GetSkillSlot(skillId);
        if (slot == null || slot.data == null || slot.logicScript == null) return;

        // STEP 0: CHECK FLOCK RANGE — Base skills don't need flock
        if (!IsBaseSkill(skillId) && !isInsideFlock.Value) return;

        // STEP 1: CHECK AUTO-UNLOCK CONDITION — Base skills always pass
        if (slot.data.lambsRequired > unlockedSkillTier.Value) return;
        // STEP 2: CHECK COOLDOWN (Attack speed)
        if (lastCastTimes.TryGetValue(skillId, out float lastTime))
        {
            if (Time.time < lastTime + slot.data.cooldown) return; 
        }

        // Cập nhật thời gian thi triển
        lastCastTimes[skillId] = Time.time;

        // CHỈ BẮN EVENT NẾU LÀ LOCAL PLAYER
        if (IsOwner)
        {
            OnSkillCooldownStarted?.Invoke(skillId, slot.data.cooldown);
        }

        // Gọi logic lên Server
        CastSkillServerRpc(skillId);
    }

    [ServerRpc]
    private void CastSkillServerRpc(int skillId)
    {
        SkillSlot slot = GetSkillSlot(skillId);
        if (slot == null || slot.data == null || slot.logicScript == null) return;

        // Check flock range on Server (Anti-hack) — Base skills bypass
        if (!IsBaseSkill(skillId) && !isInsideFlock.Value) return;

        // Double check on Server to prevent Hack — Base skills bypass
        if (slot.data.lambsRequired > unlockedSkillTier.Value) return;

        // STEP 3: DEDUCT MANA (If skill has manaCost > 0)
        if (slot.data.manaCost > 0)
        {
            if (entity != null && !entity.ConsumeMana((int)slot.data.manaCost)) return;
        }

        // STEP 4: TRIGGER SERVER LOGIC (Basic Attack, Headbutt, Fart, Dash, etc.)
        slot.logicScript.ServerExecute(slot.data, entity, controller);

        // STEP 5: TRIGGER VISUALS ON ALL CLIENTS
        PlaySkillVisualClientRpc(skillId);
    }

    [ClientRpc]
    private void PlaySkillVisualClientRpc(int skillId)
    {
        SkillSlot slot = GetSkillSlot(skillId);
        if (slot != null && slot.logicScript != null)
        {
            slot.logicScript.ClientPlayVisual(slot.data);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && controller != null) controller.OnSkillActivated -= TryCastSkill;
    }
}