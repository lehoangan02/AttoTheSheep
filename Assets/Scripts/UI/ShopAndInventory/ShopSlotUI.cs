using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace AttoTheSheep.UI.ShopAndInventory
{
    public class ShopSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Button selectButton;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button buyButton;
        
        [Header("Notification Badge (Inventory Count)")]
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private TextMeshProUGUI badgeCountText;

        [Header("Runtime Data")]
        [SerializeField] private ActionItem _currentItem;
        [SerializeField] private ShopUI _shopUI;

        private void Start()
        {
            // Re-hook listeners at runtime because non-persistent listeners from Editor are lost
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => {
                    if (ShopManager.Instance != null && _currentItem != null)
                        ShopManager.Instance.BuyItem(_currentItem);
                });
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => {
                    if (_shopUI != null && _currentItem != null) 
                        _shopUI.ShowItemDetails(_currentItem);
                });
            }
            UpdateBadge();
        }

        private void OnEnable()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.onInventoryUpdated += UpdateBadge;
        }

        private void OnDisable()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.onInventoryUpdated -= UpdateBadge;
        }

        public void Setup(ActionItem item, ShopUI shopUI)
        {
            _currentItem = item;
            _shopUI = shopUI;
            if (iconImage != null) iconImage.sprite = item.icon;
            if (priceText != null) priceText.text = item.price.ToString() + "$";
            
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => ShopManager.Instance.BuyItem(_currentItem));
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => {
                    if (_shopUI != null) _shopUI.ShowItemDetails(_currentItem);
                });
            }

            UpdateBadge();
        }

        private void UpdateBadge()
        {
            if (_currentItem == null || InventoryManager.Instance == null) return;

            int count = InventoryManager.Instance.GetItemCount(_currentItem);
            if (count > 0)
            {
                if (badgeRoot != null) badgeRoot.SetActive(true);
                if (badgeCountText != null) badgeCountText.text = count.ToString();
            }
            else
            {
                if (badgeRoot != null) badgeRoot.SetActive(false);
            }
        }
    }
}
