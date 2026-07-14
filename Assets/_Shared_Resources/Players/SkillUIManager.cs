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
        Debug.Log($"[SkillUIManager DEBUG] SetupSlot for skill {data.skillId} '{data.skillName}'. Icon is null? {data.skillIcon == null}. Setting onto {gameObject.name}");
        cooldownOverlay.fillAmount = 0f;
        
        // Cố tình ép màu trắng và alpha = 1 để tránh bị đen
        iconImage.color = Color.white;

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

    private bool IsMultiplayerScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return sceneName == "MultiplayerLevel" || sceneName == "SampleScene";
    }

    private void CheckLockState(int currentSheepValue)
    {
        if (skillData == null) return;
        
        // Force unlock cho Multiplayer, normal lock for Singleplayer
        bool isMultiplayer = IsMultiplayerScene();
        bool isLocked = !isMultiplayer && (currentSheepValue < skillData.lambsRequired); 
        
        if (lockedOverlay != null) 
        {
            lockedOverlay.SetActive(isLocked);
            Debug.Log($"[SkillUIManager DEBUG] CheckLockState: {skillData.skillName} | isLocked = {isLocked} | lockedOverlay active self: {lockedOverlay.activeSelf}");
        }

        // Add greyed-out visual effect using the cooldown overlay
        if (cooldownOverlay != null)
        {
            if (isLocked)
            {
                cooldownOverlay.fillAmount = 1f;
            }
            else if (cooldownOverlay.fillAmount >= 1f) // Only reset if it was locked, let normal cooldown routines handle themselves
            {
                cooldownOverlay.fillAmount = 0f;
            }
        }
        else
        {
            Debug.Log($"[SkillUIManager DEBUG] CheckLockState: {skillData.skillName} | lockedOverlay is NULL!");
        }
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