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
    [SerializeField] private string newGameSceneName = "Tutorial";

    [Header("Cloud Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button multiplayerButton;

    [Header("Settings")]
    [SerializeField] private Button settingsIconButton;
    [SerializeField] private GameObject settingsPanel;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.4f;

    private bool _isTransitioning;
    private bool _settingsPanelOpen;

    private void Start()
    {
        // Wire up buttons
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);

        // Continue is NOT implemented — keep it but grey it out
        if (continueButton != null)
        {
            continueButton.interactable = false;
            // Optionally add a "Coming Soon" tooltip in the future
        }

        // Multiplayer is NOT implemented — keep it but grey it out
        if (multiplayerButton != null)
        {
            multiplayerButton.interactable = false;
        }

        // Settings toggle
        if (settingsIconButton != null)
            settingsIconButton.onClick.AddListener(OnSettingsToggled);

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

            // Also grab the Image so we can animate its color alpha
            var fadeImage = fadeOverlay.GetComponent<UnityEngine.UI.Image>();

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                fadeOverlay.alpha = t;
                if (fadeImage != null)
                    fadeImage.color = new Color(0f, 0f, 0f, t);
                yield return null;
            }
            fadeOverlay.alpha = 1f;
            if (fadeImage != null) fadeImage.color = Color.black;
        }

        SceneManager.LoadScene(sceneName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Called by Editor setup tool to inject references at design time
    // ─────────────────────────────────────────────────────────────────────────
    public void Setup(Button newGame, Button cont, Button multi, Button settingsIcon, GameObject settPanel, CanvasGroup fade)
    {
        newGameButton = newGame;
        continueButton = cont;
        multiplayerButton = multi;
        settingsIconButton = settingsIcon;
        settingsPanel = settPanel;
        fadeOverlay = fade;
    }
}
