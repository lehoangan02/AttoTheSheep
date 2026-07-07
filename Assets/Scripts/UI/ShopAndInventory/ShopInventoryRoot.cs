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
            GameObject bagGo = GameObject.Find("BagButton");
            if (bagGo != null)
            {
                Button bagBtn = bagGo.GetComponent<Button>();
                if (bagBtn != null) 
                {
                    bagBtn.onClick.RemoveAllListeners();
                    bagBtn.onClick.AddListener(() => inventoryPanel.SetActive(true));
                }
            }
            else
            {
                Debug.LogWarning("[ShopInventoryRoot] Không tìm thấy GameObject nào tên 'BagButton' trên Scene để móc sự kiện!");
            }

            GameObject shopGo = GameObject.Find("ShopButton");
            if (shopGo != null)
            {
                Button shopBtn = shopGo.GetComponent<Button>();
                if (shopBtn != null) 
                {
                    shopBtn.onClick.RemoveAllListeners();
                    shopBtn.onClick.AddListener(() => shopPanel.SetActive(true));
                }
            }
            else
            {
                Debug.LogWarning("[ShopInventoryRoot] Không tìm thấy GameObject nào tên 'ShopButton' trên Scene để móc sự kiện!");
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
