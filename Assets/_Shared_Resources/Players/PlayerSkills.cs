using UnityEngine;
using Unity.Netcode;

public class PlayerSkills : NetworkBehaviour
{
    private PlayerController controller;

    [Header("Quản lý Kỹ năng")]
    [Tooltip("Cấp độ Skill được phép dùng (Do Server quyết định dựa vào số lượng cừu)")]
    public NetworkVariable<int> unlockedSkillTier = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        // Tìm PlayerController (có thể nằm ở chính GameObject này hoặc GameObject cha)
        controller = GetComponentInParent<PlayerController>();
        
        if (controller == null)
        {
            Debug.LogError("❌ [LỖI] PlayerSkills không tìm thấy PlayerController!");
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner && controller != null)
        {
            // Đăng ký nhận sự kiện bấm nút Skill từ Controller tổng
            controller.OnSkillActivated += TryCastSkill;
        }
    }

    // -------------------------------------------------------------------
    // XỬ LÝ PHÍA CLIENT (Máy người chơi)
    // -------------------------------------------------------------------
    private void TryCastSkill(int skillId)
    {
        // 1. Bức tường số 1 (Chặn nội bộ):
        // Ngăn không cho Client gửi yêu cầu rác lên Server nếu chưa đủ điều kiện
        if (skillId > unlockedSkillTier.Value)
        {
            Debug.Log($"🛡️ [CLIENT] Từ chối! Chưa đủ số lượng cừu để tung Skill {skillId}. Cấp bầy hiện tại: {unlockedSkillTier.Value}");
            return;
        }

        // Nếu hợp lệ, xin phép Server kích hoạt chiêu
        CastSkillServerRpc(skillId);
    }

    // -------------------------------------------------------------------
    // XỬ LÝ PHÍA SERVER (Máy chủ nắm quyền)
    // -------------------------------------------------------------------
    [ServerRpc]
    private void CastSkillServerRpc(int skillId)
    {
        // 2. Bức tường số 2 (Chống Hack/Cheat):
        // Nếu Client cố tình dùng tool sửa bộ nhớ để vượt qua bức tường 1, Server sẽ bắt tại trận ở đây
        if (skillId > unlockedSkillTier.Value)
        {
            Debug.LogWarning($"🚨 [SERVER] PHÁT HIỆN GIAN LẬN: Player {OwnerClientId} cố tình gửi lệnh xài Skill {skillId} khi chưa mở khóa!");
            return;
        }

        // Lấy ID của máy gửi lệnh để phân biệt ai đang dùng skill
        ulong casterId = OwnerClientId; 

        switch (skillId)
        {
            case 1:
                ExecuteSkill1(casterId);
                break;
            case 2:
                ExecuteSkill2(casterId);
                break;
            case 3:
                ExecuteSkill3(casterId);
                break;
        }
    }

    // ---- KHU VỰC THỰC THI CHIÊU THỨC TRÊN SERVER ----

    private void ExecuteSkill1(ulong casterId)
    {
        Debug.Log($"⚔️ [SERVER] Player {casterId} tung SKILL 1 (Bắn đạn thẳng)");
        // TODO: Viết code Spawn viên đạn tại đây
    }

    private void ExecuteSkill2(ulong casterId)
    {
        Debug.Log($"💨 [SERVER] Player {casterId} tung SKILL 2 (Kỹ năng lướt/Hồi máu)");
        // TODO: Viết code thay đổi vận tốc Rigidbody hoặc buff tại đây
    }

    private void ExecuteSkill3(ulong casterId)
    {
        Debug.Log($"💥 [SERVER] Player {casterId} tung SKILL 3 (CHIÊU CUỐI - AOE)");
        // TODO: Viết code tạo vùng nổ sát thương diện rộng tại đây
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && controller != null)
        {
            // Hủy đăng ký sự kiện khi nhân vật bị hủy để tránh lỗi rò rỉ bộ nhớ (Memory Leak)
            controller.OnSkillActivated -= TryCastSkill;
        }
    }
}