using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionSlot : MonoBehaviour
{
    [Header("Item Data")]
    public ActionItem itemData;

    [Header("UI References")]
    public Image iconDisplay;
    public TextMeshProUGUI amountText;
    public Image counterImage;

    private PlayerProfile _playerProfile;
    private IPlayerRepository _playerRepository;

    void Awake()
    {
        // Auto-assign counterImage early so InitializeUI can dim it even before Start
        if (counterImage == null)
        {
            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject.name == "Counter")
                {
                    counterImage = img;
                    break;
                }
            }
        }
    }

    void Start()
    {

        if (itemData != null && iconDisplay != null)
        {
            iconDisplay.sprite = itemData.icon;
        }
        else
        {

        }
    }

    public void InjectDependencies(PlayerProfile profile, IPlayerRepository repository)
    {
        _playerProfile = profile;
        _playerRepository = repository;
    }

    public void InitializeUI()
    {

        UpdateUI();
    }

    // Called by the ActionBarController when the player presses a hotkey
    public bool UseItem()
    {
        // Safety check to ensure there is an item assigned to this slot
        if (itemData == null)
        {

            return false;
        }

        bool isMultiplayer = IsMultiplayerScene();

        if (!isMultiplayer && _playerProfile == null)
        {

            return false;
        }

        string assetName = ((UnityEngine.Object)itemData).name;
        if (isMultiplayer || _playerProfile.ConsumeItem(assetName))
        {
            // Update the visual UI directly from the new profile state
            UpdateUI();

            // Save the updated profile to the cloud
            if (!isMultiplayer && _playerRepository != null)
            {
                _playerRepository.SaveAsync(_playerProfile);
            }

            return true;
        }
        else
        {

            return false;
        }
    }

    private bool IsMultiplayerScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return sceneName == "MultiplayerLevel" || sceneName == "SampleScene";
    }

    private int GetCurrentAmount()
    {
        if (IsMultiplayerScene()) return 99;

        if (_playerProfile == null || itemData == null) return 0;

        string assetName = ((UnityEngine.Object)itemData).name;

        int amount = 0;
        switch (assetName)
        {
            case "Shield": amount = _playerProfile.FlockShieldCount; break;
            case "DeathTotem": amount = _playerProfile.SpawnMaxLambsCount; break;
            case "Meat": amount = _playerProfile.SkillDamageBoostCount; break;
            case "MushShroom": amount = _playerProfile.SpeedBoostCount; break;
            default: amount = 0; break;
        }

        return amount;
    }

    private void UpdateUI()
    {
        int amount = GetCurrentAmount();
        amountText.text = amount.ToString();

        // Visual feedback: Dim the icon and counter if we run out of items
        if (amount <= 0)
        {
            iconDisplay.color = new Color(1f, 1f, 1f, 0.4f); // Dimmed
            if (counterImage != null) counterImage.color = new Color(1f, 1f, 1f, 0.4f);
        }
        else
        {
            iconDisplay.color = Color.white; // Normal
            if (counterImage != null) counterImage.color = Color.white;
        }
    }
}