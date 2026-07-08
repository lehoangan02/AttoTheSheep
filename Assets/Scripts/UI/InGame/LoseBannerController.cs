using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AttoTheSheep.UI.InGame
{
    public class LoseBannerController : MonoBehaviour
    {
        [Header("UI Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Scene Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Reward UI")]
        [SerializeField] private TMPro.TextMeshProUGUI earnedGoldText;

        private void Start()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnRetryClicked()
        {
            Debug.Log("[LoseBanner] Đang chơi lại Level hiện tại...");
            Time.timeScale = 1f; // Bỏ trạng thái pause trước khi load lại
            
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }

            // Load lại chính scene hiện tại
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnMainMenuClicked()
        {
            Debug.Log("[LoseBanner] Về Main Menu...");
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
                earnedGoldText.text = $"Coin:\n<color=red>+{earnedGold}</color>";
                
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
