using System.Collections.Generic;
using UnityEngine;

public class SkillBoardUI : MonoBehaviour
{
    [Header("UI Slots (Kéo 4 SkillUISlot từ Hierarchy vào đây)")]
    public List<SkillUIManager> uiSlots = new List<SkillUIManager>();

    private void Start()
    {
        // Only disable the PC SkillBoard. Mobile skill buttons might also use this script!
        if (Application.isMobilePlatform && gameObject.name == "SkillBoard")
        {
            gameObject.SetActive(false);
        }
    }

    public void InitializeSkillBoard(PlayerSkills localPlayerSkills)
    {

        foreach (SkillUIManager slotUI in uiSlots)
        {
            if (slotUI == null) continue;

            int targetSkillId = slotUI.boundSkillId;

            SkillSlot matchedSkill = localPlayerSkills.equippedSkills.Find(
                slot => slot.data != null && slot.data.skillId == targetSkillId
            );

            if (matchedSkill != null)
            {

                slotUI.gameObject.SetActive(true);
                slotUI.SetupSlot(matchedSkill.data, localPlayerSkills);
            }
            else
            {

                slotUI.gameObject.SetActive(false);
            }
        }
    }
}