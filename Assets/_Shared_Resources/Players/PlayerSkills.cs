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

    // THAY ĐỔI: Lưu trực tiếp số lượng cừu thực tế đang có trên mạng thay vì lưu Tier
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

            // Tìm tất cả SkillBoardUI trên Scene và nạp dữ liệu
            SkillBoardUI[] boardUIs = FindObjectsByType<SkillBoardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[PlayerSkills DEBUG] OnNetworkSpawn. IsOwner: {IsOwner}, IsServer: {IsServer}. Found {boardUIs.Length} SkillBoardUIs in scene.");
            
            foreach (SkillBoardUI boardUI in boardUIs)
            {
                // Bỏ qua SkillBoardUI của các Player khác (nếu nó được gắn trên Prefab của Player)
                Unity.Netcode.NetworkObject parentNetObj = boardUI.GetComponentInParent<Unity.Netcode.NetworkObject>();
                
                Debug.Log($"[PlayerSkills DEBUG] Checking {boardUI.gameObject.name}. Parent NetObj: {(parentNetObj != null ? parentNetObj.name + " ID:" + parentNetObj.NetworkObjectId : "None")}. My ID: {this.NetworkObject.NetworkObjectId}");
                
                if (parentNetObj != null && parentNetObj != this.NetworkObject)
                {
                    Debug.Log($"[PlayerSkills DEBUG] Skipping {boardUI.gameObject.name} because it belongs to another player (ID: {parentNetObj.NetworkObjectId})");
                    continue; 
                }

                Debug.Log($"[PlayerSkills DEBUG] Calling InitializeSkillBoard on {boardUI.gameObject.name}");
                boardUI.InitializeSkillBoard(this);
            }
        }
        else
        {
            // Tắt UI màn hình của các người chơi khác (Remote Players) để không bị đè lên màn hình của Host/Local Player
            SkillBoardUI[] myBoardUIs = this.NetworkObject.GetComponentsInChildren<SkillBoardUI>(true);
            Debug.Log($"[PlayerSkills DEBUG] Non-owner player spawned. Found {myBoardUIs.Length} SkillBoardUIs in children.");
            foreach (SkillBoardUI ui in myBoardUIs)
            {
                Canvas parentCanvas = ui.GetComponentInParent<Canvas>();
                if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
                {
                    Debug.Log($"[PlayerSkills DEBUG] Disabling remote player Canvas: {parentCanvas.gameObject.name}");
                    parentCanvas.gameObject.SetActive(false);
                }
                else
                {
                    Debug.Log($"[PlayerSkills DEBUG] Disabling remote player SkillBoardUI object directly: {ui.gameObject.name}");
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

        // THAY ĐỔI: So sánh trực tiếp số cừu yêu cầu với số cừu thực tế đang sở hữu
        if (!isMultiplayer && slot.data.lambsRequired > currentLambCount.Value) return;

        // STEP 2: CHECK COOLDOWN (Attack speed)
        // Trong Multiplayer, dùng cooldown nhỏ (0.5s) để spam. Singleplayer thì xài cooldown gốc.
        float actualCooldown = isMultiplayer ? 0.5f : slot.data.cooldown;
        
        if (lastCastTimes.TryGetValue(skillId, out float lastTime))
        {
            if (Time.time < lastTime + actualCooldown) return; 
        }

        // Cập nhật thời gian thi triển
        lastCastTimes[skillId] = Time.time;

        // CHỈ BẮN EVENT NẾU LÀ LOCAL PLAYER
        if (IsOwner)
        {
            float visualCooldown = isMultiplayer ? 0f : slot.data.cooldown;
            OnSkillCooldownStarted?.Invoke(skillId, visualCooldown); 
        }

        // Gọi logic lên Server
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

        // THAY ĐỔI: Kiểm tra chống hack trên Server bằng số cừu thực tế
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