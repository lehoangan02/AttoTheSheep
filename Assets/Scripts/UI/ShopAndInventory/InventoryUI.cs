using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace AttoTheSheep.UI.ShopAndInventory
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Grid References")]
        [SerializeField] private Transform slotsParent;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private TextMeshProUGUI goldText;

        [Header("Detail Panel References")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailName;
        [SerializeField] private TextMeshProUGUI detailDesc;
        
        [Header("Cloud Buttons")]
        [SerializeField] private Button uploadButton;
        [SerializeField] private Button fetchButton;

        private List<InventorySlotUI> _activeSlots = new List<InventorySlotUI>();

        private void OnEnable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.onInventoryUpdated += RefreshUI;
                RefreshUI();
            }

            if (uploadButton != null) uploadButton.onClick.AddListener(() => InventoryManager.Instance?.UploadInventoryToCloud());
            if (fetchButton != null) fetchButton.onClick.AddListener(() => InventoryManager.Instance?.FetchInventoryFromCloud());
        }

        private void OnDisable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.onInventoryUpdated -= RefreshUI;
            }
            
            if (uploadButton != null) uploadButton.onClick.RemoveAllListeners();
            if (fetchButton != null) fetchButton.onClick.RemoveAllListeners();
        }

        private void RefreshUI()
        {
            if (InventoryManager.Instance == null) return;

            if (goldText != null) goldText.text = InventoryManager.Instance.Gold.ToString();

            var items = InventoryManager.Instance.GetAllItems().ToList();

            // Đảm bảo đủ số slot UI
            while (_activeSlots.Count < Mathf.Max(20, items.Count))
            {
                GameObject obj = Instantiate(slotPrefab, slotsParent);
                _activeSlots.Add(obj.GetComponent<InventorySlotUI>());
            }

            // Gán dữ liệu vào slot
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                if (i < items.Count)
                {
                    _activeSlots[i].Setup(items[i].Key, items[i].Value, this);
                }
                else
                {
                    _activeSlots[i].SetupEmpty();
                }
            }
        }

        public void ShowItemDetails(ActionItem item)
        {
            if (detailIcon != null) 
            {
                detailIcon.sprite = item.icon;
                detailIcon.gameObject.SetActive(true);
            }
            if (detailName != null) detailName.text = item.itemName;
            if (detailDesc != null) detailDesc.text = item.itemDescription;
        }
    }
}
