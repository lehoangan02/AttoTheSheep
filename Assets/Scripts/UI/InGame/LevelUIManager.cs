using UnityEngine;

namespace AttoTheSheep.UI.InGame
{
    /// <summary>
    /// Lắng nghe tín hiệu từ LevelManager (Win) và PlayerEntity (Lose) 
    /// để gọi đúng Banner UI ra hiển thị.
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
            // Đăng ký nghe ngóng sự kiện
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnLevelComplete += HandleWin;
            }
            
            PlayerEntity.OnAnyPlayerDied += HandleLose;
        }

        private void OnDisable()
        {
            // Hủy đăng ký để tránh lỗi bộ nhớ (Memory Leak)
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnLevelComplete -= HandleWin;
            }
            
            PlayerEntity.OnAnyPlayerDied -= HandleLose;
        }

        private void HandleWin()
        {
            // Kiểm tra xem có NPC Spawner trong màn này không
            var npcSpawner = Object.FindFirstObjectByType<LevelCompletionNPCSpawner>();
            if (npcSpawner != null)
            {
                Debug.Log("[LevelUIManager] Nhận tín hiệu WIN! Nhưng có NPC Spawner, chờ Dialogue kết thúc...");
                return; // WinBanner sẽ được gọi từ WinBannerOnDialogueEnd
            }

            Debug.Log("[LevelUIManager] Nhận tín hiệu WIN! Đang bật WinBanner...");
            ShowBanner(winBannerPrefab);
        }

        /// <summary>
        /// Được gọi từ bên ngoài (ví dụ như sau khi kết thúc Dialogue) để ép mở WinBanner.
        /// </summary>
        public void ShowWinBannerNow()
        {
            Debug.Log("[LevelUIManager] Hiển thị WinBanner từ yêu cầu bên ngoài...");
            ShowBanner(winBannerPrefab);
        }

        private void HandleLose()
        {
            Debug.Log("[LevelUIManager] Nhận tín hiệu LOSE! Đang bật LoseBanner...");
            ShowBanner(loseBannerPrefab);
        }

        private void ShowBanner(GameObject bannerPrefab)
        {
            if (bannerPrefab == null)
            {
                Debug.LogError("[LevelUIManager] Chưa gán file Banner Prefab trong Inspector!");
                return;
            }

            // Xóa Banner cũ (nếu có)
            if (_activeBanner != null)
            {
                Destroy(_activeBanner);
            }

            // Spawn Banner mới
            if (canvasParent != null)
            {
                _activeBanner = Instantiate(bannerPrefab, canvasParent);
            }
            else 
            {
                _activeBanner = Instantiate(bannerPrefab);
            }

            // Đảm bảo Banner được active
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

            // Tạm dừng game hoàn toàn khi hiện bảng Win/Lose
            Time.timeScale = 0f;
        }
    }
}
