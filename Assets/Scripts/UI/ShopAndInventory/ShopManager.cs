using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace AttoTheSheep.UI.ShopAndInventory
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("Available Items to Sell")]
        public List<ActionItem> shopItems = new List<ActionItem>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void BuyItem(ActionItem item)
        {
            if (InventoryManager.Instance.SpendGold(item.price))
            {
                InventoryManager.Instance.AddItem(item, 1);

            }
            else
            {

            }
        }
    }
}
