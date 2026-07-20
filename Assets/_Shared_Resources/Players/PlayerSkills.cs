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

    public NetworkVariable<int> currentLambCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

            SkillBoardUI[] boardUIs = FindObjectsByType<SkillBoardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (SkillBoardUI boardUI in boardUIs)
            {

                Unity.Netcode.NetworkObject parentNetObj = boardUI.GetComponentInParent<Unity.Netcode.NetworkObject>();

                if (parentNetObj != null && parentNetObj != this.NetworkObject)
                {

                    continue;
                }

                boardUI.InitializeSkillBoard(this);
            }
        }
        else
        {

            SkillBoardUI[] myBoardUIs = this.NetworkObject.GetComponentsInChildren<SkillBoardUI>(true);

            foreach (SkillBoardUI ui in myBoardUIs)
            {
                Canvas parentCanvas = ui.GetComponentInParent<Canvas>();
                if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
                {

                    parentCanvas.gameObject.SetActive(false);
                }
                else
                {

                    ui.gameObject.SetActive(false);
                }
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

    private bool IsMultiplayerScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return sceneName == "MultiplayerLevel" || sceneName == "SampleScene";
    }

    private void TryCastSkill(int skillId)
    {
        SkillSlot slot = GetSkillSlot(skillId);
        if (slot == null || slot.data == null || slot.logicScript == null) return;

        bool isMultiplayer = IsMultiplayerScene();

        // STEP 0: CHECK FLOCK RANGE — Base skills don't need flock
        if (!isMultiplayer && !IsBaseSkill(skillId) && !isInsideFlock.Value) return;

        if (!isMultiplayer && slot.data.lambsRequired > currentLambCount.Value) return;

        // STEP 2: CHECK COOLDOWN (Attack speed)

        float actualCooldown = isMultiplayer ? 0.5f : slot.data.cooldown;

        if (lastCastTimes.TryGetValue(skillId, out float lastTime))
        {
            if (Time.time < lastTime + actualCooldown) return;
        }

        lastCastTimes[skillId] = Time.time;

        if (IsOwner)
        {
            float visualCooldown = isMultiplayer ? 0f : slot.data.cooldown;
            OnSkillCooldownStarted?.Invoke(skillId, visualCooldown);
        }

        CastSkillServerRpc(skillId);
    }

    [ServerRpc]
    private void CastSkillServerRpc(int skillId)
    {
        SkillSlot slot = GetSkillSlot(skillId);
        if (slot == null || slot.data == null || slot.logicScript == null) return;

        bool isMultiplayer = IsMultiplayerScene();

        // Check flock range on Server (Anti-hack) — Base skills bypass
        if (!isMultiplayer && !IsBaseSkill(skillId) && !isInsideFlock.Value) return;

        if (!isMultiplayer && slot.data.lambsRequired > currentLambCount.Value) return;

        // STEP 3: DEDUCT MANA (If skill has manaCost > 0)
        // COMMENTED OUT MANA COST SO YOU CAN TEST FREELY
        // if (slot.data.manaCost > 0)
        // {
        //     if (entity != null && !entity.ConsumeMana((int)slot.data.manaCost)) return;
        // }

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

    [ClientRpc]
    public void PlaySkillHitVisualClientRpc(int skillId, Vector2 hitPosition)
    {
        SkillSlot slot = GetSkillSlot(skillId);
        if (slot != null && slot.logicScript != null)
        {
            slot.logicScript.ClientPlayHitEffect(slot.data, hitPosition);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && controller != null) controller.OnSkillActivated -= TryCastSkill;
    }
}