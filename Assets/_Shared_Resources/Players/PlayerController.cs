using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;
using Unity.Collections;

public class PlayerController : NetworkBehaviour
{
    // Các Event (Interface) để các component con cắm vào lấy dữ liệu
    public event Action<Vector2> OnMoveInputChanged;
    public event Action<int> OnSkillActivated; // Trả về ID của Skill (0, 1, 2, 3)
    public event Action<Vector2> OnMapClicked; 

    // ----------------------------------------------------
    // NHẬN INPUT DI CHUYỂN
    // ----------------------------------------------------
    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        Vector2 moveInput = value.Get<Vector2>();
        OnMoveInputChanged?.Invoke(moveInput);
    }

    // ----------------------------------------------------
    // NHẬN INPUT ĐÁNH THƯỜNG (SKILL 0) - Dòng này vừa được thêm!
    // ----------------------------------------------------
    public void OnBasicAttack(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(0); // Gọi SkillSlot số 0
    }

    // ----------------------------------------------------
    // NHẬN INPUT CÁC KỸ NĂNG KHÁC (SKILL 1, 2, 3)
    // ----------------------------------------------------
    public void OnSkill1(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(1); // Gọi SkillSlot số 1 (Lướt)
    }

    public void OnSkill2(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(2); // Gọi SkillSlot số 2 (Đánh rắm)
    }

    public void OnSkill3(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(3); // Gọi SkillSlot số 3 (Chiêu cuối)
    }

    // ----------------------------------------------------
    // NHẬN INPUT CLICK CHUỘT (Cho Bầy Cừu)
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

        // Bắt buộc phải có để NetworkVariable so sánh dữ liệu mới/cũ chính xác
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
        Debug.Log($"[PlayerController] OnNetworkSpawn đang chạy trên: {gameObject.name}. IsOwner: {IsOwner}");
        if (IsOwner)
        {
            // Tự động cấp một cái tên ngẫu nhiên ngay khi vừa spawn vào game
            TestSetRandomName();
        }
    }

    // --- TESTING SECTION ---
    [ContextMenu("Test Set Random Name")]
    public void TestSetRandomName()
    {
        Debug.Log($"[PlayerController] Thực thi lệnh TestSetRandomName. IsOwner của object này là: {IsOwner}");
        if (IsOwner)
        {
            FixedString64Bytes newName = $"Player {UnityEngine.Random.Range(1000, 9999)}";
            SetPlayerNameRpc(newName);
            Debug.Log($"🟢 [LOCAL] Đã gửi yêu cầu Server đổi tên thành: {newName}");
        }
        else
        {
            Debug.LogWarning("🟡 [PlayerController] Bạn không phải Owner của Player này, không thể đổi tên!");
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void SetPlayerNameRpc(FixedString64Bytes newName)
    {
        netPlayerPublicData.Value = new PlayerPublicData { playerName = newName };
        Debug.Log($"[SERVER] Đã phê duyệt và cập nhật tên thành {newName}");
    }
}