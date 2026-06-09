using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[System.Serializable]
public class SkillSlot
{
    public string slotName = "New Skill"; 
    public SkillData data;                
    public BaseSkillComponent logicScript;
}

public class PlayerSkills : NetworkBehaviour
{
    private PlayerController controller;
    private NetworkEntity entity;

    // Cấp độ bầy cừu (0: Không có cừu, 1: 3 cừu, 2: 6 cừu, 3: 10 cừu)
    public NetworkVariable<int> unlockedSkillTier = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Player có đang đứng trong vòng bán kính của bầy cừu không
    public NetworkVariable<bool> isInsideFlock = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Bảng Kỹ Năng Đang Lắp (Kéo thả vào đây)")]
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
        if (IsOwner && controller != null) controller.OnSkillActivated += TryCastSkill;
    }

    private SkillSlot GetSkillSlot(int skillId)
    {
        return equippedSkills.Find(slot => slot.data != null && slot.data.skillId == skillId);
    }

    private void TryCastSkill(int skillId)
    {
        SkillSlot slot = GetSkillSlot(skillId);
        if (slot == null || slot.data == null || slot.logicScript == null) return;

        // BƯỚC 0: KIỂM TRA PHẠM VI BẦY CỪU — Chỉ được dùng skill khi đứng trong vòng bán kính
        if (!isInsideFlock.Value) return;

        // BƯỚC 1: KIỂM TRA ĐIỀU KIỆN UNLOCK TỰ ĐỘNG
        // Vì Đánh thường có skillId = 0, và unlockedSkillTier luôn >= 0, nó sẽ luôn luôn lọt qua bài Test này!
        if (skillId > unlockedSkillTier.Value) return;

        // BƯỚC 2: CHECK COOLDOWN (Tốc độ đánh)
        if (lastCastTimes.TryGetValue(skillId, out float lastTime))
        {
            if (Time.time < lastTime + slot.data.cooldown) return; 
        }

        lastCastTimes[skillId] = Time.time;
        CastSkillServerRpc(skillId);
    }

    [ServerRpc]
    private void CastSkillServerRpc(int skillId)
    {
        SkillSlot slot = GetSkillSlot(skillId);
        if (slot == null || slot.data == null || slot.logicScript == null) return;

        // Kiểm tra phạm vi bầy cừu trên Server (chống Hack)
        if (!isInsideFlock.Value) return;

        // Kiểm tra lại trên Server chống Hack
        if (skillId > unlockedSkillTier.Value) return;

        // BƯỚC 3: TRỪ MANA (Nếu skill đó có set manaCost > 0)
        if (slot.data.manaCost > 0)
        {
            if (entity != null && !entity.ConsumeMana((int)slot.data.manaCost)) return;
        }

        // BƯỚC 4: KÍCH HOẠT LOGIC TRÊN SERVER (Đánh thường, Rắm, Lướt, v.v.)
        slot.logicScript.ServerExecute(slot.data, entity, controller);

        // BƯỚC 5: PHÁT ĐỘNG HÌNH ẢNH TRÊN MỌI CLIENT
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