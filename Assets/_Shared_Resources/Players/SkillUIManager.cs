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

        iconImage.color = Color.white;

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

        CheckLockState(playerSkills.currentLambCount.Value);
    }

    private void OnEnable()
    {
        PlayerSkills.OnSkillCooldownStarted += HandleSkillCooldown;
    }

    private void OnDisable()
    {
        PlayerSkills.OnSkillCooldownStarted -= HandleSkillCooldown;

        if (playerSkills != null) playerSkills.currentLambCount.OnValueChanged -= OnLambCountChanged;
    }

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