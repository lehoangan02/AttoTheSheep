using UnityEngine;
using UnityEngine.UI;

namespace AttoTheSheep.UI.ShopAndInventory
{
    public class ShopInventoryRoot : MonoBehaviour
    {
        public GameObject shopPanel;
        public GameObject inventoryPanel;

        private void Start()
        {
            Button bagBtn = GameObject.Find("BagButton")?.GetComponent<Button>();
            if (bagBtn != null) 
            {
                bagBtn.onClick.RemoveAllListeners();
                bagBtn.onClick.AddListener(() => inventoryPanel.SetActive(true));
            }

            Button shopBtn = GameObject.Find("ShopButton")?.GetComponent<Button>();
            if (shopBtn != null) 
            {
                shopBtn.onClick.RemoveAllListeners();
                shopBtn.onClick.AddListener(() => shopPanel.SetActive(true));
            }

            // Gắn sự kiện cho các nút CloseBtn bên trong Panel
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
