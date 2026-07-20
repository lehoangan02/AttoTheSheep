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

            TogglePauseMenu();
        }

        private void TogglePauseMenu()
        {
            _isPauseMenuOpen = !_isPauseMenuOpen;

            if (pausePanel != null)
                pausePanel.SetActive(_isPauseMenuOpen);

            if (_isPauseMenuOpen)
            {

                if (Stop.Instance != null) Stop.Instance.PauseGame();
            }
            else
            {

                bool isDialogueActive = (DialogueManager.Instance != null && DialogueManager.Instance.panelRoot != null && DialogueManager.Instance.panelRoot.activeInHierarchy);

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

        }

        private void OnMainMenuClicked()
        {

            if (Stop.Instance != null) Stop.Instance.ResumeGame();

            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }

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

                toggleAutoText.text = _isAutoMode ? "A" : "M";
            }
        }
    }
}
