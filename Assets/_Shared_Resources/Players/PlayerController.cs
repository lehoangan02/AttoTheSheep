using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;
using Unity.Collections;

public class PlayerController : NetworkBehaviour
{
    // Events (Interfaces) for child components to plug into and get data
    public event Action<Vector2> OnMoveInputChanged;
    public event Action<int> OnSkillActivated; // Returns Skill ID (0, 1, 2, 3, 4)
    public event Action<int> OnCheatActivated; // Returns Cheat ID (1, 2, 3, 4)
    public event Action<Vector2> OnMapClicked; 

    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        Vector2 moveInput = value.Get<Vector2>();
        OnMoveInputChanged?.Invoke(moveInput);
    }

    // ----------------------------------------------------
    // RECEIVE OTHER SKILLS INPUT (SKILL 1, 2, 3) - Matches actions "Skill1", "Skill2", "Skill3" in PlayerInputActions
    // ----------------------------------------------------
    public void OnSkill1(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(1); // Call SkillSlot 1 (Dash)
    }

    public void OnSkill2(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(2); // Call SkillSlot 2 (Fart)
    }

    public void OnSkill3(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(3); // Call SkillSlot 3 (Ultimate)
    }

    // ----------------------------------------------------
    // RECEIVE BASIC ATTACK / HEADBUTT INPUT (SKILL 0)
    // ----------------------------------------------------
    public void OnHeadbutt(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(0); // Call SkillSlot with skillId=0 (Headbutt)
    }

    // ----------------------------------------------------
    // RECEIVE CHEAT INPUT (Cheat1..Cheat4 from Input Actions)
    // ----------------------------------------------------
    public void OnCheat1(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        // OnCheatActivated?.Invoke(1); // Disabled to let ActionBarController handle it and consume items
    }

    public void OnCheat2(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        // OnCheatActivated?.Invoke(2);
    }

    public void OnCheat3(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        // OnCheatActivated?.Invoke(3);
    }

    public void OnCheat4(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        // OnCheatActivated?.Invoke(4);
    }

    // ----------------------------------------------------
    // RECEIVE MOUSE CLICK INPUT (For Flock) - Matches action "Click" in PlayerInputActions
    // ----------------------------------------------------
    public void OnClick(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        if (Camera.main != null)
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            OnMapClicked?.Invoke(mouseWorldPos);
        }
    }


    // section by lehoangan02
    // shared variables
    public struct PlayerPublicData : INetworkSerializable, IEquatable<PlayerPublicData>
    {
        public FixedString64Bytes playerName;
        public void NetworkSerialize<T> (BufferSerializer<T> serializer) where T: IReaderWriter
        {
            serializer.SerializeValue(ref playerName);
        }

        // Required for NetworkVariable to accurately compare old/new data
        public bool Equals(PlayerPublicData other)
        {
            return playerName == other.playerName;
        }
    }

    public NetworkVariable<PlayerPublicData> netPlayerPublicData = new NetworkVariable<PlayerPublicData>(
        new PlayerPublicData(), 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private System.Collections.IEnumerator Start()
    {
        // Fix: PC vs Mac race condition where dynamically spawned players are destroyed before OnNetworkSpawn runs
        yield return new WaitForSeconds(0.1f);

        // On clients, pre-placed scene objects that were despawned by the server 
        // will not have OnNetworkSpawn called and will remain unspawned.
        // We must destroy them to prevent them from becoming "extra" phantom players that steal input.
        // FIX: Only do this in Multiplayer levels. In single-player, the pre-placed Atto is REQUIRED.
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        if (sceneName == "MultiplayerLevel" || sceneName == "SampleScene" || sceneName == "FTUE")
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (!NetworkObject.IsSpawned)
                {
                    Debug.Log($"[PlayerController] Destroying unspawned pre-placed player object: {gameObject.name}");
                    Destroy(gameObject);
                }
            }
        }
    }

    private TMPro.TextMeshPro _nameTag;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerController] OnNetworkSpawn is running on: {gameObject.name}. IsOwner: {IsOwner}");
        
        // Fix: In multiplayer scenes (MultiplayerLevel), we must destroy any manually pre-placed Atto objects
        // otherwise they become a 3rd uncontrollable player that steals input or causes Game Over when killed.
        if (IsServer && !NetworkObject.IsPlayerObject)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if (sceneName == "MultiplayerLevel" || sceneName == "SampleScene" || sceneName == "FTUE")
            {
                Debug.Log($"[PlayerController] Destroying redundant pre-placed Atto in networked scene: {gameObject.name}");
                NetworkObject.Despawn(true);
                return;
            }
        }

        if (IsOwner)
        {
            // Automatically assign a random name upon spawning into the game
            TestSetRandomName();
            
            // Ensure PlayerInput is enabled for the owner
            var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null) playerInput.enabled = true;
        }
        else
        {
            // Disable PlayerInput for non-owners so they don't steal the Gamepad/Keyboard from the local player
            var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "MultiplayerLevel" || sceneName == "SampleScene")
        {
            CreateNameTag();
            netPlayerPublicData.OnValueChanged += OnPlayerNameChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        netPlayerPublicData.OnValueChanged -= OnPlayerNameChanged;
    }

    private void CreateNameTag()
    {
        GameObject tagObj = new GameObject("PlayerNameTag");
        tagObj.transform.SetParent(transform);
        tagObj.transform.localPosition = new Vector3(0, 0.7f, 0); // Position reduced in half (0.7f)

        _nameTag = tagObj.AddComponent<TMPro.TextMeshPro>();
        _nameTag.alignment = TMPro.TextAlignmentOptions.Center;
        _nameTag.fontSize = 2.5f;
        _nameTag.color = IsOwner ? Color.yellow : Color.white;
        _nameTag.sortingOrder = 100;
        
        UpdateNameTagText(netPlayerPublicData.Value.playerName.ToString());
    }

    private void OnPlayerNameChanged(PlayerPublicData prev, PlayerPublicData next)
    {
        UpdateNameTagText(next.playerName.ToString());
    }

    private void UpdateNameTagText(string newName)
    {
        if (_nameTag != null)
        {
            // Fully opaque black background (#000000) for maximum contrast
            _nameTag.text = $"<mark=#000000><b>{newName}</b></mark>";
        }
    }

    // --- TESTING SECTION ---
    [ContextMenu("Test Set Random Name")]
    public void TestSetRandomName()
    {
        Debug.Log($"[PlayerController] Executing TestSetRandomName command. IsOwner of this object is: {IsOwner}");
        if (IsOwner)
        {
            FixedString64Bytes newName = $"Player {UnityEngine.Random.Range(1000, 9999)}";
            SetPlayerNameRpc(newName);
            Debug.Log($"🟢 [LOCAL] Sent request to Server to change name to: {newName}");
        }
        else
        {
            Debug.LogWarning("🟡 [PlayerController] You are not the Owner of this Player, cannot change name!");
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void SetPlayerNameRpc(FixedString64Bytes newName)
    {
        netPlayerPublicData.Value = new PlayerPublicData { playerName = newName };
        Debug.Log($"[SERVER] Approved and updated name to {newName}");
    }
}