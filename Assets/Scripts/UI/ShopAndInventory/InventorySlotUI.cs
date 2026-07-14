using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace AttoTheSheep.UI.ShopAndInventory
{
    public class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Button selectButton;

        private ActionItem _currentItem;
        private InventoryUI _parentUI;

        public void Setup(ActionItem item, int count, InventoryUI parentUI)
        {
            _currentItem = item;
            _parentUI = parentUI;

            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.gameObject.SetActive(true);
            }

            if (countText != null) 
            {
                countText.text = "x" + count.ToString();
                countText.gameObject.SetActive(true);
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnSlotClicked);
            }
        }

        public void SetupEmpty()
        {
            _currentItem = null;
            if (iconImage != null) iconImage.gameObject.SetActive(false);
            if (countText != null) countText.gameObject.SetActive(false);
            
            if (selectButton != null) selectButton.onClick.RemoveAllListeners();
        }

        private void OnSlotClicked()
        {
            if (_currentItem != null && _parentUI != null)
            {
                _parentUI.ShowItemDetails(_currentItem);
            }
        }
    }
}
