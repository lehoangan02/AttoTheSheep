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
            Debug.LogWarning("ActionSlot is missing Item Data or Icon Display reference!", this);
        }
    }

    public void InjectDependencies(PlayerProfile profile, IPlayerRepository repository)
    {
        _playerProfile = profile;
        _playerRepository = repository;
    }

    public void InitializeUI()
    {
        Debug.Log($"[ActionSlot] InitializeUI called for {itemData?.itemName}. Profile is null? {_playerProfile == null}. GetCurrentAmount(): {GetCurrentAmount()}");
        UpdateUI();
    }

    // Called by the ActionBarController when the player presses a hotkey
    public bool UseItem()
    {
        // Safety check to ensure there is an item assigned to this slot
        if (itemData == null) 
        {
            Debug.LogWarning("No ActionItem assigned to this slot!");
            return false; 
        }

        if (_playerProfile == null)
        {
            Debug.LogWarning("PlayerProfile not loaded yet!");
            return false;
        }

        if (_playerProfile.ConsumeItem(itemData.itemName))
        {
            // Update the visual UI directly from the new profile state
            UpdateUI();
            
            Debug.Log($"Used {itemData.itemName}! Remaining: {GetCurrentAmount()}");

            // Save the updated profile to the cloud
            if (_playerRepository != null)
            {
                _playerRepository.SaveAsync(_playerProfile);
            }

            return true;
        }
        else
        {
            Debug.Log($"{itemData.itemName} is empty!");
            return false;
        }
    }

    private int GetCurrentAmount()
    {
        if (_playerProfile == null || itemData == null) return 0;
        
        switch (itemData.itemName)
        {
            case "Shield": return _playerProfile.FlockShieldCount;
            case "DeathTotem": return _playerProfile.SpawnMaxLambsCount;
            case "Meat": return _playerProfile.SkillDamageBoostCount;
            case "MushShroom": return _playerProfile.SpeedBoostCount;
            default: return 0;
        }
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