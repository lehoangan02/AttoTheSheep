using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SkillUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public Image cooldownOverlay;
    
    [Header("Skill Binding")]
    public int boundSkillId; // ID của skill mà ô UI này đại diện

    public void SetupSlot(SkillData data)
    {
        boundSkillId = data.skillId;
        
        // Lưu ý: Đảm bảo bạn đã thêm biến 'public Sprite skillIcon;' vào file SkillData.cs nhé!
        // Nếu chưa thêm, đoạn code này sẽ báo lỗi đỏ ở chữ skillIcon.
        iconImage.sprite = data.skillIcon; 
        
        cooldownOverlay.fillAmount = 0f;
    }

    private void OnEnable()
    {
        // Lắng nghe sự kiện tung chiêu từ PlayerSkills
        PlayerSkills.OnSkillCooldownStarted += HandleSkillCooldown;
    }

    private void OnDisable()
    {
        PlayerSkills.OnSkillCooldownStarted -= HandleSkillCooldown;
    }

    private void HandleSkillCooldown(int skillId, float cooldownDuration)
    {
        // Chỉ chạy hiệu ứng xoay cooldown nếu ID của chiêu trùng khớp với ô UI này
        if (skillId == boundSkillId)
        {
            StartCoroutine(CooldownRoutine(cooldownDuration));
        }
    }

    private IEnumerator CooldownRoutine(float duration)
    {
        if (duration <= 0) yield break;

        float timer = duration;
        cooldownOverlay.fillAmount = 1f; // Phủ đen toàn bộ icon

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            // Giảm dần lớp phủ đen theo thời gian
            cooldownOverlay.fillAmount = timer / duration;
            yield return null;
        }

        cooldownOverlay.fillAmount = 0f; // Kết thúc hồi chiêu
    }
}