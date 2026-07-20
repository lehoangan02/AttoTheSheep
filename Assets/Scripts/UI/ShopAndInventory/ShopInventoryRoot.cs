using UnityEngine;
using UnityEngine.UI;

namespace AttoTheSheep.UI.ShopAndInventory
{
    public class ShopInventoryRoot : MonoBehaviour
    {
        public GameObject shopPanel;
        public GameObject inventoryPanel;

        [Header("Button References (kéo trực tiếp từ Inspector)")]
        [SerializeField] private Button bagButton;
        [SerializeField] private Button shopButton;

        private void Start()
        {

            if (bagButton == null)
            {
                var go = GameObject.Find("BagButton");
                if (go != null) bagButton = go.GetComponent<Button>();
                else
            }

            if (shopButton == null)
            {
                var go = GameObject.Find("ShopButton");
                if (go != null) shopButton = go.GetComponent<Button>();
                else
            }

            if (bagButton != null)
            {
                bagButton.onClick.RemoveAllListeners();
                bagButton.onClick.AddListener(() => inventoryPanel.SetActive(true));
            }

            if (shopButton != null)
            {
                shopButton.onClick.RemoveAllListeners();
                shopButton.onClick.AddListener(() => shopPanel.SetActive(true));
            }

            if (shopPanel != null)
            {
                Transform closeBtn = shopPanel.transform.Find("ShopPanel/CloseBtn");
                if (closeBtn != null && closeBtn.TryGetComponent<Button>(out var btn))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => shopPanel.SetActive(false));
                }
            }

            if (inventoryPanel != null)
            {
                Transform closeBtn = inventoryPanel.transform.Find("InventoryPanel/CloseBtn");
                if (closeBtn != null && closeBtn.TryGetComponent<Button>(out var btn))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => inventoryPanel.SetActive(false));
                }
            }
        }
    }
}
