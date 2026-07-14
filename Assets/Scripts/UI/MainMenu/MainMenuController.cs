using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the Main Menu logic:
/// - New Game => loads Tutorial scene
/// - Continue => placeholder (not implemented, button disabled)
/// - Multiplayer => placeholder (not implemented, button disabled)
/// - Settings icon (gear) => toggles the Settings Panel
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string newGameSceneName = "FTUE";
    [SerializeField] private string multiplayerSceneName = "MatchMaking";

    [Header("Cloud Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button multiplayerButton;

    [Header("Settings")]
    [SerializeField] private Button settingsIconButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Button settingsBackButton;

    [Header("Cursors")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D disabledCursor;

    public Texture2D HoverCursor => hoverCursor;
    public Texture2D DisabledCursor => disabledCursor;
    public Texture2D DefaultCursor => defaultCursor;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.4f;

    private bool _isTransitioning;
    private bool _settingsPanelOpen;

    private void Start()
    {
        // Set default cursor
        if (defaultCursor != null)
        {
            Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
        }

        // Upgrade Fade Overlay to use background for seamless transition
        if (fadeOverlay != null)
        {
            var fadeImg = fadeOverlay.GetComponent<Image>();
            var bgObj = GameObject.Find("Background");
            if (fadeImg != null && bgObj != null)
            {
                var bgImg = bgObj.GetComponent<Image>();
                if (bgImg != null && bgImg.sprite != null)
                {
                    fadeImg.sprite = bgImg.sprite;
                    fadeImg.color = Color.white;
                }
            }
        }

        // Wire up buttons
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);

        // Continue Button now goes to MapLobby
        if (continueButton != null)
        {
            continueButton.interactable = true;
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (multiplayerButton != null)
        {
            multiplayerButton.interactable = true;
            multiplayerButton.onClick.AddListener(OnMultiplayerClicked);
        }

        // Settings toggle
        if (settingsIconButton != null)
            settingsIconButton.onClick.AddListener(OnSettingsToggled);
            
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(OnSettingsToggled);
            
        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(OnSettingsToggled);

        // Make sure settings panel starts closed
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Make sure fade overlay is invisible at start
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
        }
    }

    private void OnDestroy()
    {
        if (newGameButton != null) newGameButton.onClick.RemoveListener(OnNewGameClicked);
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
        if (multiplayerButton != null) multiplayerButton.onClick.RemoveListener(OnMultiplayerClicked);
        if (settingsIconButton != null) settingsIconButton.onClick.RemoveListener(OnSettingsToggled);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Button callbacks
    // ─────────────────────────────────────────────────────────────────────────

    private void OnNewGameClicked()
    {
        if (_isTransitioning) return;
        StartCoroutine(FadeAndLoad(newGameSceneName));
    }

    private void OnContinueClicked()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionTo("MapLobby");
        }
        else
        {
            SceneManager.LoadScene("MapLobby");
        }
    }

    private void OnMultiplayerClicked()
    {
        if (_isTransitioning) return;
        StartCoroutine(FadeAndLoad(multiplayerSceneName));
    }

    private void OnSettingsToggled()
    {
        if (settingsPanel == null) return;
        _settingsPanelOpen = !_settingsPanelOpen;
        settingsPanel.SetActive(_settingsPanelOpen);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scene transition with fade
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator FadeAndLoad(string sceneName)
    {
        _isTransitioning = true;

        if (fadeOverlay != null)
        {
            fadeOverlay.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                fadeOverlay.alpha = t;
                yield return null;
            }
            fadeOverlay.alpha = 1f;
        }

        SceneManager.LoadScene(sceneName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Called by Editor setup tool to inject references at design time
    // ─────────────────────────────────────────────────────────────────────────
    public void Setup(Button newGame, Button cont, Button multi, Button settingsIcon, GameObject settPanel, CanvasGroup fade, Texture2D cDefault = null, Texture2D cHover = null, Texture2D cDisabled = null, Button settClose = null, Button settBack = null)
    {
        newGameButton = newGame;
        continueButton = cont;
        multiplayerButton = multi;
        settingsIconButton = settingsIcon;
        settingsPanel = settPanel;
        fadeOverlay = fade;
        
        defaultCursor = cDefault;
        hoverCursor = cHover;
        disabledCursor = cDisabled;
        
        settingsCloseButton = settClose;
        settingsBackButton = settBack;
    }
}
