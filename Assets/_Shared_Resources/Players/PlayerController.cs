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
    private void Update()
    {
        // Extreme Fallback for Mobile: If PlayerInput "Click" action completely fails to fire
        // because of control scheme bugs, we manually check the raw touchscreen device every frame.
        if (Application.isMobilePlatform && IsOwner && UnityEngine.InputSystem.Touchscreen.current != null)
        {
            if (UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                ProcessHerdMovementClick(UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue());
            }
        }
    }

    public void OnClick(InputValue value)
    {

        if (!IsOwner || !value.isPressed) return;

        if (UnityEngine.InputSystem.Pointer.current != null)
        {
            ProcessHerdMovementClick(UnityEngine.InputSystem.Pointer.current.position.ReadValue());
        }
    }

    private void ProcessHerdMovementClick(Vector2 screenPos)
    {
        // Prevent clicking through actual mobile UI buttons, but DO NOT block clicks if they just hit the fullscreen Joystick background
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            var pointerEventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            pointerEventData.position = screenPos;

            var raycastResults = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerEventData, raycastResults);

            bool hitValidUI = false;
            foreach (var result in raycastResults)
            {
                // Ignore the joystick's giant invisible touch zone.
                if (result.gameObject.GetComponentInParent<UnityEngine.InputSystem.OnScreen.OnScreenStick>() != null)
                {

                    continue;
                }

                // If we hit a valid Button underneath the joystick (like an NPC's E Button), click it manually!
                var btn = result.gameObject.GetComponentInParent<UnityEngine.UI.Button>();
                if (btn != null)
                {

                    btn.onClick.Invoke();
                    return; // Don't move the herd
                }

                // If we hit any other UI element, block the click
                hitValidUI = true;

                break;
            }

            if (hitValidUI) return;
        }

        if (Camera.main != null)
        {
            Vector2 pointerWorldPos = Camera.main.ScreenToWorldPoint(screenPos);

            // Check if we clicked near an interactive NPC (using a generous 1.5 unit radius)
            // This ensures that even if you click the E-Button hovering above the NPC,
            // the physics circle will still overlap the NPC's actual body collider!
            Collider2D[] hits = Physics2D.OverlapCircleAll(pointerWorldPos, 1.5f);
            foreach (var hit in hits)
            {
                var dt = hit.GetComponent<DialogueTrigger>() ?? hit.GetComponentInParent<DialogueTrigger>();
                if (dt != null)
                {

                    dt.TriggerDialogue();
                    return; // Don't move the herd
                }
            }

            OnMapClicked?.Invoke(pointerWorldPos);
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

        // Fix: Bubble Dialogs (SpriteRenderers) render behind Screen Space - Overlay canvases.
        // On Mobile, we must convert the Mobile Controls Canvas to Camera Space so the dialogs (Order 32000) can render above the buttons.
        if (Application.isMobilePlatform)
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // Identify the Mobile UI Canvas by checking for on-screen controls
                    if (canvas.GetComponentInChildren<UnityEngine.InputSystem.OnScreen.OnScreenControl>(true) != null)
                    {
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera = Camera.main;
                        canvas.planeDistance = 5f;
                        canvas.sortingLayerName = "UI";
                        canvas.sortingOrder = 10000; // Dialogues are 32000, so they will be on top!
                    }
                }
            }
        }

        // On clients, pre-placed scene objects that were despawned by the server
        // will not have OnNetworkSpawn called and will remain unspawned.
        // We must destroy them to prevent them from becoming "extra" phantom players that steal input.
        // FIX: Only do this in Multiplayer levels. In single-player, the pre-placed Atto is REQUIRED.
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (sceneName == "MultiplayerLevel" || sceneName == "SampleScene")
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (!NetworkObject.IsSpawned)
                {

                    Destroy(gameObject);
                }
            }
        }
    }

    private TMPro.TextMeshPro _nameTag;

    public override void OnNetworkSpawn()
    {

        // Fix: In multiplayer scenes (MultiplayerLevel), we must destroy any manually pre-placed Atto objects
        // otherwise they become a 3rd uncontrollable player that steals input or causes Game Over when killed.
        if (IsServer && !NetworkObject.IsPlayerObject)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if (currentScene == "MultiplayerLevel" || currentScene == "SampleScene")
            {

                NetworkObject.Despawn(true);
                return;
            }
        }

        if (IsOwner)
        {
            // Automatically assign a random name upon spawning into the game
            TestSetRandomName();

            // Ensure PlayerInput is enabled for the owner (but don't re-enable if already enabled to avoid losing devices on Mac)
            var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null && !playerInput.enabled)
            {
                playerInput.enabled = true;
            }
        }
        else
        {
            // Disable PlayerInput for non-owners so they don't steal the Gamepad/Keyboard from the local player
            var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null && playerInput.enabled)
            {
                playerInput.enabled = false;
            }
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

        if (IsOwner)
        {
            FixedString64Bytes newName = $"Player {UnityEngine.Random.Range(1000, 9999)}";
            SetPlayerNameRpc(newName);

        }
        else
        {

        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void SetPlayerNameRpc(FixedString64Bytes newName)
    {
        netPlayerPublicData.Value = new PlayerPublicData { playerName = newName };

    }
}