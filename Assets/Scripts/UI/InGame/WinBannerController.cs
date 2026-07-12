using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AttoTheSheep.UI.InGame
{
    public class WinBannerController : MonoBehaviour
    {
        [Header("UI Buttons")]
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Scene Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string nextLevelSceneName = "Level2"; // Thay đổi tên scene màn tiếp theo nếu cần

        [Header("Reward UI")]
        [SerializeField] private TMPro.TextMeshProUGUI earnedGoldText;

        private void Start()
        {
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            // TODO: Bạn có thể play animation popup ở đây bằng Animator hoặc DOTween
            // GetComponent<Animator>()?.Play("PopUp");
        }

        private void OnNextLevelClicked()
        {
            Debug.Log("[WinBanner] Chuyển sang màn tiếp theo...");
            Time.timeScale = 1f; // Đảm bảo game không bị khựng khi sang màn mới
            
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }

            // Tự động tính toán màn tiếp theo nếu tên màn hiện tại có dạng "LevelX"
            string currentScene = SceneManager.GetActiveScene().name;
            string targetScene = nextLevelSceneName;

            if (currentScene.StartsWith("Level") && int.TryParse(currentScene.Replace("Level", ""), out int currentLevelNum))
            {
                // Nếu là Level 3 (màn cuối), ép buộc về Main Menu
                if (currentLevelNum >= 3)
                {
                    Debug.Log("[WinBanner] Đã hoàn thành Level 3, chuyển về Main Menu.");
                    targetScene = mainMenuSceneName;
                }
                // else
                // {
                //     string nextLevel = "Level" + (currentLevelNum + 1);
                //     if (Application.CanStreamedLevelBeLoaded(nextLevel))
                //     {
                //         targetScene = nextLevel;
                //     }
                //     else
                //     {
                //         Debug.LogWarning($"[WinBanner] Không tìm thấy {nextLevel} trong Build Settings. Về Main Menu.");
                //         targetScene = mainMenuSceneName;
                //     }
                // }
            }
            Debug.Log($"[WinBanner] Chuyển sang scene: {targetScene}");
            SceneManager.LoadScene(targetScene);
        }

        private void OnMainMenuClicked()
        {
            Debug.Log("[WinBanner] Về Main Menu...");
            Time.timeScale = 1f;
            
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void ShowBanner(int earnedGold = 0)
        {
            if (earnedGoldText != null)
            {
                earnedGoldText.text = $"<color=green>+{earnedGold}</color>";
                
                // Nếu muốn Banner này TỰ ĐỘNG cộng tiền luôn, mở comment dòng dưới:
                // if (AttoTheSheep.UI.ShopAndInventory.InventoryManager.Instance != null)
                //     AttoTheSheep.UI.ShopAndInventory.InventoryManager.Instance.AddGold(earnedGold);
            }
            gameObject.SetActive(true);
        }

        public void HideBanner()
        {
            gameObject.SetActive(false);
        }
    }
}
