using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AttoTheSheep.UI.ShopAndInventory
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        public int Gold 
        { 
            get 
            {
                if (GameBootstrapper.Instance != null && GameBootstrapper.Instance.CurrentProfile != null)
                    return GameBootstrapper.Instance.CurrentProfile.Coins;
                return _localGoldFallback;
            }
        }
        private int _localGoldFallback = 0;

        // Dữ liệu item và số lượng đang có
        private Dictionary<ActionItem, int> _inventory = new Dictionary<ActionItem, int>();

        public delegate void OnInventoryUpdated();
        public event OnInventoryUpdated onInventoryUpdated;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (GameBootstrapper.Instance == null)
            {
                // Không có Bootstrapper (chạy test scene trực tiếp) → dùng local fallback
                return;
            }

            if (GameBootstrapper.Instance.CurrentProfile != null)
            {
                // Cloud đã load xong trước khi Start() chạy → fetch ngay
                FetchInventoryFromCloud();
            }
            else
            {
                // Cloud chưa xong → đăng ký chờ event OnBootstrapped
                GameBootstrapper.Instance.OnBootstrapped += FetchInventoryFromCloud;
            }
        }

        private void OnDestroy()
        {
            // Cleanup event để tránh memory leak
            if (GameBootstrapper.Instance != null)
                GameBootstrapper.Instance.OnBootstrapped -= FetchInventoryFromCloud;
        }

        private bool IsMultiplayerScene()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return sceneName == "MultiplayerLevel" || sceneName == "SampleScene";
        }

        public void AddItem(ActionItem item, int amount)
        {
            if (IsMultiplayerScene()) return;

            if (_inventory.ContainsKey(item))
                _inventory[item] += amount;
            else
                _inventory[item] = amount;
                
            onInventoryUpdated?.Invoke();
            UploadInventoryToCloud();
        }

        public void RemoveItem(ActionItem item, int amount)
        {
            if (IsMultiplayerScene()) return;

            if (_inventory.ContainsKey(item))
            {
                _inventory[item] -= amount;
                if (_inventory[item] <= 0) _inventory.Remove(item);
                onInventoryUpdated?.Invoke();
                UploadInventoryToCloud();
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
            if (IsMultiplayerScene()) return;

            if (GameBootstrapper.Instance != null && GameBootstrapper.Instance.CurrentProfile != null)
            {
                GameBootstrapper.Instance.CurrentProfile.AddCoins(amount);
                UploadInventoryToCloud();
            }
            else
            {
                _localGoldFallback += amount;
            }
            onInventoryUpdated?.Invoke();
        }

        public bool SpendGold(int amount)
        {
            if (IsMultiplayerScene()) return false;

            if (GameBootstrapper.Instance != null && GameBootstrapper.Instance.CurrentProfile != null)
            {
                if (GameBootstrapper.Instance.CurrentProfile.Coins >= amount)
                {
                    GameBootstrapper.Instance.CurrentProfile.SpendCoins(amount);
                    UploadInventoryToCloud();
                    onInventoryUpdated?.Invoke();
                    return true;
                }
            }
            else if (_localGoldFallback >= amount)
            {
                _localGoldFallback -= amount;
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
            if (GameBootstrapper.Instance == null || GameBootstrapper.Instance.CurrentProfile == null) return;
            
            Debug.Log("[InventoryManager] Fetching data from Cloud Save profile...");
            var profile = GameBootstrapper.Instance.CurrentProfile;
            
            if (ShopManager.Instance != null)
            {
                _inventory.Clear();
                foreach (var item in ShopManager.Instance.shopItems)
                {
                    int count = 0;
                    if (item.itemName == "Shield" || item.name == "Shield") count = profile.FlockShieldCount;
                    else if (item.itemName == "DeathTotem" || item.name == "DeathTotem") count = profile.SpawnMaxLambsCount;
                    else if (item.itemName == "Meat" || item.name == "Meat") count = profile.SkillDamageBoostCount;
                    else if (item.itemName == "MushShroom" || item.name == "MushShroom") count = profile.SpeedBoostCount;
                    
                    if (count > 0)
                    {
                        _inventory[item] = count;
                    }
                }
            }
            onInventoryUpdated?.Invoke();
        }

        public void UploadInventoryToCloud()
        {
            if (GameBootstrapper.Instance == null || GameBootstrapper.Instance.CurrentProfile == null) return;

            Debug.Log("[InventoryManager] Syncing to Cloud Save...");
            var profile = GameBootstrapper.Instance.CurrentProfile;
            
            int shieldCount = 0;
            int deathTotemCount = 0;
            int meatCount = 0;
            int mushShroomCount = 0;

            foreach (var kvp in _inventory)
            {
                string name = !string.IsNullOrEmpty(kvp.Key.itemName) ? kvp.Key.itemName : kvp.Key.name;
                if (name == "Shield") shieldCount = kvp.Value;
                else if (name == "DeathTotem") deathTotemCount = kvp.Value;
                else if (name == "Meat") meatCount = kvp.Value;
                else if (name == "MushShroom") mushShroomCount = kvp.Value;
            }

            profile.RestoreState(
                profile.Coins, profile.Exp, profile.UnlockedStage,
                profile.DamageLevel, profile.HpLevel, profile.HerdHpLevel,
                profile.HasArmor, profile.HasHorn,
                shieldCount, deathTotemCount, meatCount, mushShroomCount
            );

            _ = GameBootstrapper.Instance.PlayerRepository.SaveAsync(profile);
        }
    }
}
