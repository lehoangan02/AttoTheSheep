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
        [SerializeField] private string nextLevelSceneName = "Level2";

        [Header("Reward UI")]
        [SerializeField] private TMPro.TextMeshProUGUI earnedGoldText;

        private void Start()
        {
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            // GetComponent<Animator>()?.Play("PopUp");
        }

        private void OnNextLevelClicked()
        {

            Time.timeScale = 1f;

            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }

            string currentScene = SceneManager.GetActiveScene().name;
            string targetScene = nextLevelSceneName;

            // if (currentScene.StartsWith("Level") && int.TryParse(currentScene.Replace("Level", ""), out int currentLevelNum))
            // {

            //     if (currentLevelNum >= 3)
            //     {
            //
            //         targetScene = mainMenuSceneName;
            //     }
            //     else
            //     {
            //         string nextLevel = "Level" + (currentLevelNum + 1);
            //         if (Application.CanStreamedLevelBeLoaded(nextLevel))
            //         {
            //             targetScene = nextLevel;
            //         }
            //         else
            //         {
            //
            //             targetScene = mainMenuSceneName;
            //         }
            //     }
            // }

            SceneManager.LoadScene(targetScene);
        }

        private void OnMainMenuClicked()
        {

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
