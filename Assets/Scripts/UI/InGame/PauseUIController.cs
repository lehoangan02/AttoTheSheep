using UnityEngine;
using UnityEngine.InputSystem;
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

        [SerializeField] private Button pauseButton;
        [SerializeField] private Button toggleAutoButton;
        [SerializeField] private TMPro.TextMeshProUGUI toggleAutoText;
        
        private bool _isAutoMode = false;

        [Header("Scene Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Input")]
        [SerializeField] private InputActionReference pauseAction;

        [Header("Pause Panel")]
        [Tooltip("Kéo Panel chứa nền đen mờ và giao diện Pause vào đây")]
        [SerializeField] private GameObject pausePanel; 
        
        private bool _isPauseMenuOpen = false;

        private void Awake()
        {
            // Tự động tích hợp Stop.cs vào chính Prefab này nếu chưa có
            if (GetComponent<Stop>() == null && Stop.Instance == null)
            {
                gameObject.AddComponent<Stop>();
            }
        }

        private void OnEnable()
        {
            if (pauseAction != null)
            {
                pauseAction.action.performed += HandlePauseInput;
                pauseAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (pauseAction != null)
            {
                pauseAction.action.performed -= HandlePauseInput;
                pauseAction.action.Disable();
            }
        }

        private void Start()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
            if (toggleAutoButton != null) toggleAutoButton.onClick.AddListener(OnToggleAutoClicked);
            
            // Sync with FlockManager
            var flockManager = Object.FindFirstObjectByType<FlockManager>();
            if (flockManager != null)
            {
                _isAutoMode = flockManager.currentControlMode == FlockControlMode.Auto;
                flockManager.OnControlModeChanged += HandleControlModeChanged;
            }
            UpdateToggleAutoUI();

            // Mặc định ẩn giao diện Pause (Overlay) khi mới vào game
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            var flockManager = Object.FindFirstObjectByType<FlockManager>();
            if (flockManager != null)
            {
                flockManager.OnControlModeChanged -= HandleControlModeChanged;
            }
        }

        private void HandlePauseInput(InputAction.CallbackContext ctx)
        {
            // Lắng nghe Input Action để bật/tắt bảng Pause
            TogglePauseMenu();
        }

        private void TogglePauseMenu()
        {
            _isPauseMenuOpen = !_isPauseMenuOpen;
            
            if (pausePanel != null) 
                pausePanel.SetActive(_isPauseMenuOpen);

            if (_isPauseMenuOpen)
            {
                // Khi bật Pause Menu, luôn ép game dừng lại
                if (Stop.Instance != null) Stop.Instance.PauseGame();
            }
            else
            {
                // Khi tắt Pause Menu, kiểm tra xem DLG có đang mở không
                bool isDialogueActive = (DialogueManager.Instance != null && DialogueManager.Instance.panelRoot != null && DialogueManager.Instance.panelRoot.activeInHierarchy);
                
                // CHỈ Resume game nếu không vướng hội thoại
                if (!isDialogueActive)
                {
                    if (Stop.Instance != null) Stop.Instance.ResumeGame();
                }
            }
        }

        private void OnPauseClicked()
        {
            if (!_isPauseMenuOpen) TogglePauseMenu();
        }

        private void OnResumeClicked()
        {
            if (_isPauseMenuOpen) TogglePauseMenu();
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

        private void OnToggleAutoClicked()
        {
            var flockManager = Object.FindFirstObjectByType<FlockManager>();
            if (flockManager != null)
            {
                flockManager.RequestToggleControlMode();
            }
            else
            {
                _isAutoMode = !_isAutoMode;
                UpdateToggleAutoUI();
            }
        }

        private void HandleControlModeChanged(FlockControlMode newMode)
        {
            _isAutoMode = newMode == FlockControlMode.Auto;
            UpdateToggleAutoUI();
        }

        private void UpdateToggleAutoUI()
        {
            if (toggleAutoText != null)
            {
                // Lúc Auto thì hiện chữ A, lúc Thủ công thì hiện chữ M
                toggleAutoText.text = _isAutoMode ? "A" : "M";
            }
        }
    }
}
