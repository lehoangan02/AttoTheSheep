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

        private bool _subscribed = false;

        [Header("Detail Panel References")]
        [SerializeField] private GameObject placeholderView;
        [SerializeField] private GameObject detailView;
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailName;
        [SerializeField] private TextMeshProUGUI detailDesc;
        [SerializeField] private Button useButton;
        [SerializeField] private Button dropButton;

        [Header("Cloud Buttons")]
        [SerializeField] private Button uploadButton;
        [SerializeField] private Button fetchButton;

        private List<InventorySlotUI> _activeSlots = new List<InventorySlotUI>();
        private ActionItem _selectedItem;

        private void OnEnable()
        {
            TrySubscribe();
            RefreshUI();

            if (uploadButton != null) uploadButton.onClick.AddListener(() => InventoryManager.Instance?.UploadInventoryToCloud());
            if (fetchButton != null) fetchButton.onClick.AddListener(() => InventoryManager.Instance?.FetchInventoryFromCloud());

            if (useButton != null) useButton.onClick.AddListener(OnUseClicked);
            if (dropButton != null) dropButton.onClick.AddListener(OnDropClicked);

            HideDetails();
        }

        private void Start()
        {

            if (!_subscribed)
            {
                TrySubscribe();
                RefreshUI();
            }
        }

        private void TrySubscribe()
        {
            if (_subscribed || InventoryManager.Instance == null) return;
            InventoryManager.Instance.onInventoryUpdated += RefreshUI;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (_subscribed && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.onInventoryUpdated -= RefreshUI;
                _subscribed = false;
            }

            if (uploadButton != null) uploadButton.onClick.RemoveAllListeners();
            if (fetchButton != null) fetchButton.onClick.RemoveAllListeners();
            if (useButton != null) useButton.onClick.RemoveAllListeners();
            if (dropButton != null) dropButton.onClick.RemoveAllListeners();
        }

        private void RefreshUI()
        {
            if (InventoryManager.Instance == null) return;

            if (goldText != null) goldText.text = InventoryManager.Instance.Gold.ToString();

            var items = InventoryManager.Instance.GetAllItems().ToList();

            while (_activeSlots.Count < Mathf.Max(20, items.Count))
            {
                GameObject obj = Instantiate(slotPrefab, slotsParent);
                _activeSlots.Add(obj.GetComponent<InventorySlotUI>());
            }

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
            _selectedItem = item;
            if (placeholderView != null) placeholderView.SetActive(false);
            if (detailView != null) detailView.SetActive(true);

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
        }

        public void HideDetails()
        {
            _selectedItem = null;
            if (placeholderView != null) placeholderView.SetActive(true);
            if (detailView != null) detailView.SetActive(false);
        }

        private void OnUseClicked()
        {
            if (_selectedItem != null && InventoryManager.Instance != null)
            {

                InventoryManager.Instance.RemoveItem(_selectedItem, 1);

                if (InventoryManager.Instance.GetItemCount(_selectedItem) <= 0)
                {
                    HideDetails();
                }
            }
        }

        private void OnDropClicked()
        {
            if (_selectedItem != null && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RemoveItem(_selectedItem, 1);
                if (InventoryManager.Instance.GetItemCount(_selectedItem) <= 0)
                {
                    HideDetails();
                }
            }
        }
    }
}
