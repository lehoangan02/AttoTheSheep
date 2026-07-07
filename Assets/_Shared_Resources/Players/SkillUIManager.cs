using UnityEngine;
using UnityEngine.UI;
using System.Collections;
 using TMPro;
// Nếu bạn dùng TextMeshPro, hãy đổi Text thành TextMeshProUGUI và thêm: using TMPro;

public class SkillUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public Image cooldownOverlay;
    public GameObject lockedOverlay; // Lớp phủ đen khi chưa đủ cừu
    public TextMeshProUGUI lambsReqText;        // Text hiển thị số cừu cần thiết
    
    [Header("Skill Binding")]
    public int boundSkillId;

    private PlayerSkills playerSkills;
    private SkillData skillData;

    // THAY ĐỔI: Nhận thêm biến PlayerSkills để đọc dữ liệu NetworkVariable
    public void SetupSlot(SkillData data, PlayerSkills pSkills)
    {
        skillData = data;
        boundSkillId = data.skillId;
        iconImage.sprite = data.skillIcon;
        cooldownOverlay.fillAmount = 0f;

        // Quản lý đăng ký sự kiện NetworkVariable an toàn
        if (playerSkills != null) playerSkills.unlockedSkillTier.OnValueChanged -= OnTierChanged;
        playerSkills = pSkills;
        if (playerSkills != null) playerSkills.unlockedSkillTier.OnValueChanged += OnTierChanged;

        // Cập nhật Text số lượng cừu
        if (skillData.lambsRequired > 0)
        {
            lambsReqText.gameObject.SetActive(true);
            lambsReqText.text = skillData.lambsRequired.ToString();
        }
        else
        {
            lambsReqText.gameObject.SetActive(false);
        }

        // Kiểm tra trạng thái Khóa/Mở ngay khi load UI
        CheckLockState(playerSkills.unlockedSkillTier.Value);
    }

    private void OnEnable()
    {
        PlayerSkills.OnSkillCooldownStarted += HandleSkillCooldown;
    }

    private void OnDisable()
    {
        PlayerSkills.OnSkillCooldownStarted -= HandleSkillCooldown;
        // Gỡ lắng nghe khi UI bị tắt để tránh lỗi Memory Leak
        if (playerSkills != null) playerSkills.unlockedSkillTier.OnValueChanged -= OnTierChanged;
    }

    // Hàm này tự động chạy mỗi khi biến unlockedSkillTier trên Server/Client thay đổi
    private void OnTierChanged(int previousValue, int newValue)
    {
        CheckLockState(newValue);
    }

    private void CheckLockState(int currentSheepValue)
    {
        if (skillData == null) return;
        
        // CÁCH MỚI: Bị khóa nếu "Số cừu yêu cầu" lớn hơn "Số cừu/Cấp độ hiện tại"
        bool isLocked = skillData.lambsRequired > currentSheepValue;
        
        lockedOverlay.SetActive(isLocked);
    }

    private void HandleSkillCooldown(int skillId, float cooldownDuration)
    {
        if (skillId == boundSkillId)
        {
            StartCoroutine(CooldownRoutine(cooldownDuration));
        }
    }

    private IEnumerator CooldownRoutine(float duration)
    {
        if (duration <= 0) yield break;
        float timer = duration;
        cooldownOverlay.fillAmount = 1f;

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            cooldownOverlay.fillAmount = timer / duration;
            yield return null;
        }
        cooldownOverlay.fillAmount = 0f;
    }
}