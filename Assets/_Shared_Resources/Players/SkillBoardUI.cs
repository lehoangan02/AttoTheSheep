using System.Collections.Generic;
using UnityEngine;

public class SkillBoardUI : MonoBehaviour
{
    [Header("UI Slots (Kéo 4 SkillUISlot từ Hierarchy vào đây)")]
    public List<SkillUIManager> uiSlots = new List<SkillUIManager>();

    // Hàm này sẽ được gọi khi Local Player spawn thành công
    public void InitializeSkillBoard(PlayerSkills localPlayerSkills)
    {
        // Lặp qua danh sách skill đang trang bị của Player
        for (int i = 0; i < localPlayerSkills.equippedSkills.Count; i++)
        {
            SkillSlot playerSkill = localPlayerSkills.equippedSkills[i];
            
            // Kiểm tra xem UI Slot có đủ số lượng không và Data có tồn tại không
            if (i < uiSlots.Count && playerSkill.data != null)
            {
                // Truyền dữ liệu vào UI Slot
                uiSlots[i].SetupSlot(playerSkill.data);
            }
        }
    }
}