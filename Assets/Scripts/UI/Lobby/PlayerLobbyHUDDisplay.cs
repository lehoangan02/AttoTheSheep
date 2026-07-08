using UnityEngine;
using TMPro;
using AttoTheSheep.UI.ShopAndInventory;

/// <summary>
/// Runtime component gắn vào PlayerLobbyHUD.
/// Tự động lắng nghe sự kiện onInventoryUpdated từ InventoryManager
/// để cập nhật số vàng hiển thị realtime.
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
        // Retry ở Start phòng trường hợp InventoryManager chưa Awake kịp lúc OnEnable
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
    /// Cập nhật số vàng lên UI.
    /// Được gọi khi InventoryManager.onInventoryUpdated fire,
    /// hoặc gọi thủ công nếu cần.
    /// </summary>
    public void RefreshGold()
    {
        if (goldText == null) return;

        if (InventoryManager.Instance != null)
        {
            // Format N0: 1000 -> "1,000" (dấu phẩy ngàn)
            goldText.text = InventoryManager.Instance.Gold.ToString("N0");
        }
        else
        {
            // Placeholder khi chưa có InventoryManager (test trong Editor)
            goldText.text = "---";
        }
    }
}
