using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AttoTheSheep.UI.InGame
{
    public class PauseUIController : MonoBehaviour
    {
        [Header("UI Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Scene Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Pause Panel")]
        [Tooltip("Kéo Panel chứa nền đen mờ và giao diện Pause vào đây (không bắt buộc nữa)")]
        [SerializeField] private GameObject pausePanel; 

        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();

            // Tự động tích hợp Stop.cs vào chính Prefab này nếu chưa có
            if (GetComponent<Stop>() == null && Stop.Instance == null)
            {
                gameObject.AddComponent<Stop>();
            }
        }

        private void Start()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            // Mặc định ẩn toàn bộ Canvas khi mới vào game
            if (_canvas != null) 
                _canvas.enabled = false;
            else if (pausePanel != null) 
                pausePanel.SetActive(false);
        }

        private void Update()
        {
            // Lắng nghe phím ESC để tự động gọi hàm TogglePause của đồng đội
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Stop.Instance != null)
                {
                    Stop.Instance.TogglePause();
                    UpdateUIVisibility();
                }
                else
                {
                    Debug.LogWarning("[PauseUI] Không tìm thấy Stop.Instance! Hãy đảm bảo script Stop.cs đã được gắn vào 1 GameObject trong Scene.");
                }
            }
        }

        /// <summary>
        /// Đồng bộ giao diện UI dựa trên trạng thái Pause của Game (từ Stop.cs)
        /// </summary>
        private void UpdateUIVisibility()
        {
            if (Stop.Instance == null) return;
            
            // Hiện panel nếu game đang pause, ẩn panel nếu game đang resume
            if (_canvas != null)
                _canvas.enabled = Stop.Instance.IsPaused;
            else if (pausePanel != null)
                pausePanel.SetActive(Stop.Instance.IsPaused);
        }

        private void OnResumeClicked()
        {
            if (Stop.Instance != null)
            {
                Stop.Instance.ResumeGame();
                UpdateUIVisibility();
            }
        }

        private void OnOptionsClicked()
        {
            Debug.Log("[PauseUI] Mở menu Cài Đặt (Feature sẽ được thêm sau)");
        }

        private void OnMainMenuClicked()
        {
            Debug.Log("[PauseUI] Thoát về Main Menu...");
            // Nhớ Resume Game để Time.timeScale quay lại 1, tránh lỗi khựng hình ở Main Menu
            if (Stop.Instance != null) Stop.Instance.ResumeGame(); 
            
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
