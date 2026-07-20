using UnityEngine;
using TMPro;
using AttoTheSheep.UI.ShopAndInventory;

/// <summary>

/// </summary>
public class PlayerLobbyHUDDisplay : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI goldText;

    private bool _subscribed = false;

    private void OnEnable()
    {
        TrySubscribe();
        RefreshGold();
    }

    private void Start()
    {

        if (!_subscribed)
        {
            TrySubscribe();
            RefreshGold();
        }
    }

    private void OnDisable()
    {
        if (_subscribed && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryUpdated -= RefreshGold;
            _subscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (_subscribed || InventoryManager.Instance == null) return;
        InventoryManager.Instance.onInventoryUpdated += RefreshGold;
        _subscribed = true;
    }

    /// <summary>

    /// </summary>
    public void RefreshGold()
    {
        if (goldText == null) return;

        if (InventoryManager.Instance != null)
        {

            goldText.text = InventoryManager.Instance.Gold.ToString("N0");
        }
        else
        {

            goldText.text = "---";
        }
    }
}
