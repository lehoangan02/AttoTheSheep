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

    private int currentAmount = 0;

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

    public void SetInitialAmount(int loadedAmount)
    {
        currentAmount = loadedAmount;
        UpdateUI();
    }

    // Called by the ActionBarController when the player presses a hotkey
    public void UseItem()
    {
        // Safety check to ensure there is an item assigned to this slot
        if (itemData == null) 
        {
            Debug.LogWarning("No ActionItem assigned to this slot!");
            return; 
        }

        if (currentAmount > 0)
        {
            // 1. Decrease the amount
            currentAmount--;
            
            // 2. Update the visual UI
            UpdateUI();
            
            // 3. Trigger the skill/item effect here
            Debug.Log($"Used {itemData.itemName}! Remaining: {currentAmount}");

            // ----------------------------------------------------
            // YOUR CUSTOM CODE HERE:
            // Use itemData.itemName to update your save system.
            // Example: MySaveManager.SaveItemCount(itemData.itemName, currentAmount);
            // ----------------------------------------------------
        }
        else
        {
            Debug.Log($"{itemData.itemName} is empty!");
        }
    }

    private void UpdateUI()
    {
        amountText.text = currentAmount.ToString();
        
        // Visual feedback: Dim the icon if we run out of items
        if (currentAmount <= 0)
        {
            iconDisplay.color = new Color(1f, 1f, 1f, 0.4f); // Dimmed
        }
        else
        {
            iconDisplay.color = Color.white; // Normal
        }
    }
}