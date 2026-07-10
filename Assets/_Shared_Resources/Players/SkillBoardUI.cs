using System.Collections.Generic;
using UnityEngine;

public class SkillBoardUI : MonoBehaviour
{
    [Header("UI Slots (Kéo 4 SkillUISlot từ Hierarchy vào đây)")]
    public List<SkillUIManager> uiSlots = new List<SkillUIManager>();

    private void Start()
    {
        if (Application.isMobilePlatform)
        {
            gameObject.SetActive(false);
        }
    }

    // Hàm này sẽ được gọi khi Local Player spawn thành công
    public void InitializeSkillBoard(PlayerSkills localPlayerSkills)
    {
        // ĐẢO NGƯỢC LOGIC: Duyệt qua từng ô UI đang hiện thị trên màn hình của bạn
        foreach (SkillUIManager slotUI in uiSlots)
        {
            if (slotUI == null) continue;

            // Đọc ID mà ô UI này yêu cầu (Ví dụ: Bạn cài đặt ô này chuyên nhận chiêu có ID = 1)
            int targetSkillId = slotUI.boundSkillId;

            // Tìm kiếm trong danh sách của Player xem có Skill nào trùng ID này không
            SkillSlot matchedSkill = localPlayerSkills.equippedSkills.Find(
                slot => slot.data != null && slot.data.skillId == targetSkillId
            );
            
            Debug.Log($"[SkillBoardUI DEBUG] Slot checking for targetSkillId: {targetSkillId}. equippedSkills Count: {localPlayerSkills.equippedSkills.Count}. Found match: {matchedSkill != null}");

            if (matchedSkill != null)
            {
                // Nếu tìm thấy: Kích hoạt hiển thị ô UI và nạp dữ liệu chiêu thức vào
                slotUI.gameObject.SetActive(true);
                slotUI.SetupSlot(matchedSkill.data, localPlayerSkills);
            }
            else
            {
                // Nếu không tìm thấy chiêu thức này trong người Player: Ẩn ô UI này đi
                Debug.Log($"[SkillBoardUI DEBUG] Matched skill was NULL for targetSkillId {targetSkillId}. Disabling slot UI GameObject {slotUI.gameObject.name}");
                slotUI.gameObject.SetActive(false);
            }
        }
    }
}