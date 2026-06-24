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
            SceneManager.LoadScene(nextLevelSceneName);
        }

        private void OnMainMenuClicked()
        {
            Debug.Log("[WinBanner] Về Main Menu...");
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void ShowBanner()
        {
            gameObject.SetActive(true);
        }

        public void HideBanner()
        {
            gameObject.SetActive(false);
        }
    }
}
