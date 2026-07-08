using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SkillUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public Image cooldownOverlay;
    public GameObject lockedOverlay; 
    public TextMeshProUGUI lambsReqText;        
    
    [Header("Skill Binding")]
    public int boundSkillId;

    private PlayerSkills playerSkills;
    private SkillData skillData;

    public void SetupSlot(SkillData data, PlayerSkills pSkills)
    {
        skillData = data;
        boundSkillId = data.skillId;
        iconImage.sprite = data.skillIcon;
        cooldownOverlay.fillAmount = 0f;

        // THAY ĐỔI: Đăng ký lắng nghe biến số lượng cừu mới
        if (playerSkills != null) playerSkills.currentLambCount.OnValueChanged -= OnLambCountChanged;
        playerSkills = pSkills;
        if (playerSkills != null) playerSkills.currentLambCount.OnValueChanged += OnLambCountChanged;

        if (skillData.lambsRequired >= 0)
        {
            lambsReqText.gameObject.SetActive(true);
            lambsReqText.text = skillData.lambsRequired.ToString();
        }
        else
        {
            lambsReqText.gameObject.SetActive(false);
        }

        // Kiểm tra trạng thái Khóa/Mở ngay lập tức bằng số cừu thực tế
        CheckLockState(playerSkills.currentLambCount.Value);
    }

    private void OnEnable()
    {
        PlayerSkills.OnSkillCooldownStarted += HandleSkillCooldown;
    }

    private void OnDisable()
    {
        PlayerSkills.OnSkillCooldownStarted -= HandleSkillCooldown;
        // THAY ĐỔI: Hủy đăng ký an toàn bằng biến đếm cừu mới
        if (playerSkills != null) playerSkills.currentLambCount.OnValueChanged -= OnLambCountChanged;
    }

    // THAY ĐỔI: Hàm tự động chạy khi số cừu của Player thay đổi trên Server
    private void OnLambCountChanged(int previousValue, int newValue)
    {
        CheckLockState(newValue);
    }

    private void CheckLockState(int currentSheepValue)
    {
        if (skillData == null) return;
        
        // Bị khóa nếu "Số cừu yêu cầu" lớn hơn "Số cừu hiện tại đang có"
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