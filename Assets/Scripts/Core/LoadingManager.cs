using UnityEngine;
using TMPro;

namespace AttoTheSheep.Core
{
    public class LoadingManager : MonoBehaviour
    {
        public static LoadingManager Instance { get; private set; }

        [Header("UI References")]
        [Tooltip("The root GameObject containing the background, animation, and text")]
        public GameObject loadingCanvas;
        [Tooltip("The TMP_Text component to display the loading status")]
        public TMP_Text statusText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Hide();
        }

        /// <summary>
        /// Show the loading screen with a specific text or localization key.
        /// </summary>
        public void Show(string textKeyOrValue)
        {
            if (loadingCanvas != null)
                loadingCanvas.SetActive(true);

            if (statusText != null)
            {
                if (LocalizationManager.Instance != null)
                    statusText.text = LocalizationManager.Instance.GetText(textKeyOrValue);
                else
                    statusText.text = textKeyOrValue;
            }
        }

        public void Hide()
        {
            if (loadingCanvas != null)
                loadingCanvas.SetActive(false);
        }
    }
}
