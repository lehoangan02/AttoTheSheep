using UnityEngine;

namespace AttoTheSheep.UI.InGame
{
    /// <summary>

    /// </summary>
    public class LevelUIManager : MonoBehaviour
    {
        [Header("Banner Prefabs")]
        [Tooltip("Kéo file WinBannerUI.prefab vào đây")]
        [SerializeField] private GameObject winBannerPrefab;

        [Tooltip("Kéo file LoseBannerUI.prefab vào đây")]
        [SerializeField] private GameObject loseBannerPrefab;

        [Header("Settings")]
        [Tooltip("Kéo PlayerScreen_Canvas (hoặc Canvas bất kỳ) vào đây để UI đè lên đúng chỗ. Nếu để trống, script tự định vị ở Root.")]
        [SerializeField] private Transform canvasParent;

        private GameObject _activeBanner;

        private void OnEnable()
        {

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnLevelComplete += HandleWin;
            }

            PlayerEntity.OnAnyPlayerDied += HandleLose;
        }

        private void OnDisable()
        {

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnLevelComplete -= HandleWin;
            }

            PlayerEntity.OnAnyPlayerDied -= HandleLose;
        }

        private void HandleWin()
        {

            var npcSpawner = Object.FindFirstObjectByType<LevelCompletionNPCSpawner>();
            if (npcSpawner != null)
            {

                return;
            }

            ShowBanner(winBannerPrefab);
        }

        /// <summary>

        /// </summary>
        public void ShowWinBannerNow()
        {

            ShowBanner(winBannerPrefab);
        }

        private void HandleLose()
        {

            ShowBanner(loseBannerPrefab);
        }

        private void ShowBanner(GameObject bannerPrefab)
        {
            if (bannerPrefab == null)
            {

                return;
            }

            if (_activeBanner != null)
            {
                Destroy(_activeBanner);
            }

            _activeBanner = bannerPrefab;
            _activeBanner.SetActive(true);

            // Pass the earned coins to the banner controller if it exists
            var winBanner = _activeBanner.GetComponent<WinBannerController>();
            if (winBanner != null)
            {
                int earnedCoins = PlayerSaveManager.Instance != null ? PlayerSaveManager.Instance.CoinsEarnedThisSession : 0;
                winBanner.ShowBanner(earnedCoins);
            }
            else
            {
                var loseBanner = _activeBanner.GetComponent<LoseBannerController>();
                if (loseBanner != null)
                {
                    loseBanner.ShowBanner(0); // Lose usually gets 0, or logic can be added here
                }
            }

            Time.timeScale = 0f;
        }
    }
}
