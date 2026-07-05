using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AttoTheSheep.UI.ShopAndInventory
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        public int Gold { get; private set; } = 1000; // Khởi tạo 1000 vàng để test

        // Dữ liệu item và số lượng đang có
        private Dictionary<ActionItem, int> _inventory = new Dictionary<ActionItem, int>();

        public delegate void OnInventoryUpdated();
        public event OnInventoryUpdated onInventoryUpdated;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void AddItem(ActionItem item, int amount)
        {
            if (_inventory.ContainsKey(item))
                _inventory[item] += amount;
            else
                _inventory[item] = amount;
                
            onInventoryUpdated?.Invoke();
        }

        public void RemoveItem(ActionItem item, int amount)
        {
            if (_inventory.ContainsKey(item))
            {
                _inventory[item] -= amount;
                if (_inventory[item] <= 0) _inventory.Remove(item);
                onInventoryUpdated?.Invoke();
            }
        }

        public int GetItemCount(ActionItem item)
        {
            if (_inventory.TryGetValue(item, out int count))
                return count;
            return 0;
        }

        public Dictionary<ActionItem, int> GetAllItems()
        {
            return _inventory;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            onInventoryUpdated?.Invoke();
        }

        public bool SpendGold(int amount)
        {
            if (Gold >= amount)
            {
                Gold -= amount;
                onInventoryUpdated?.Invoke();
                return true;
            }
            return false;
        }

        // =========================================
        // HÀM DÀNH CHO TEAM CALL CLOUD API
        // =========================================
        public void FetchInventoryFromCloud()
        {
            Debug.Log("[InventoryManager] Đang Fetch dữ liệu từ Cloud...");
            // TODO: Team gắn API Fetch ở đây
            // Giả lập load xong
            onInventoryUpdated?.Invoke();
        }

        public void UploadInventoryToCloud()
        {
            Debug.Log("[InventoryManager] Đang Upload dữ liệu lên Cloud...");
            // TODO: Team gắn API Upload ở đây
        }
    }
}
