using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;
using Unity.Collections;

public class PlayerController : NetworkBehaviour
{
    // Events (Interfaces) for child components to plug into and get data
    public event Action<Vector2> OnMoveInputChanged;
    public event Action<int> OnSkillActivated; // Returns Skill ID (0, 1, 2, 3)
    public event Action<Vector2> OnMapClicked; 

    // ----------------------------------------------------
    // RECEIVE MOVEMENT INPUT
    // ----------------------------------------------------
    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        Vector2 moveInput = value.Get<Vector2>();
        OnMoveInputChanged?.Invoke(moveInput);
    }

    // ----------------------------------------------------
    // RECEIVE BASIC ATTACK INPUT (SKILL 0) - Recently added!
    // ----------------------------------------------------
    public void OnBasicAttack(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(0); // Call SkillSlot 0
    }

    // ----------------------------------------------------
    // RECEIVE OTHER SKILLS INPUT (SKILL 1, 2, 3)
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
    // RECEIVE MOUSE CLICK INPUT (For Flock)
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

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerController] OnNetworkSpawn is running on: {gameObject.name}. IsOwner: {IsOwner}");
        if (IsOwner)
        {
            // Automatically assign a random name upon spawning into the game
            TestSetRandomName();
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