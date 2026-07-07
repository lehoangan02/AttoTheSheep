using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AttoTheSheep.UI.ShopAndInventory
{
    public class ShopUI : MonoBehaviour
    {
        [Header("Popup References")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailName;
        [SerializeField] private TextMeshProUGUI detailDesc;
        [SerializeField] private TextMeshProUGUI detailPrice;
        
        [Header("Buy Button")]
        [SerializeField] private Button buyButton;

        private ActionItem _selectedItem;

        private void OnEnable()
        {
            if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
            if (closeButton != null) closeButton.onClick.AddListener(HideDetails);
            HideDetails();
        }

        private void OnDisable()
        {
            if (buyButton != null) buyButton.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
        }

        public void ShowItemDetails(ActionItem item)
        {
            _selectedItem = item;
            if (detailIcon != null) 
            {
                detailIcon.sprite = item.icon;
                detailIcon.gameObject.SetActive(true);
            }
            
            string nameKey = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;
            if (string.IsNullOrEmpty(nameKey)) nameKey = "Unknown Item";
            string descKey = string.IsNullOrEmpty(item.itemDescription) ? "Mysterious Item" : item.itemDescription;
            
            string translatedName = AttoTheSheep.Core.LocalizationManager.Instance != null ? AttoTheSheep.Core.LocalizationManager.Instance.GetText(nameKey) : nameKey;
            string translatedDesc = AttoTheSheep.Core.LocalizationManager.Instance != null ? AttoTheSheep.Core.LocalizationManager.Instance.GetText(descKey) : descKey;

            if (detailName != null) detailName.text = translatedName;
            if (detailDesc != null) detailDesc.text = translatedDesc;
            if (detailPrice != null) detailPrice.text = "Price: " + item.price + "$";

            if (buyButton != null) buyButton.gameObject.SetActive(true);
            if (popupRoot != null) popupRoot.SetActive(true);
        }

        public void HideDetails()
        {
            _selectedItem = null;
            if (popupRoot != null) popupRoot.SetActive(false);
        }

        private void OnBuyClicked()
        {
            if (_selectedItem != null && ShopManager.Instance != null)
            {
                ShopManager.Instance.BuyItem(_selectedItem);
            }
        }
    }
}
