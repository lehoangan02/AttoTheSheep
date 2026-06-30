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
            Debug.Log("[LevelUIManager] Nhận tín hiệu WIN! Đang bật WinBanner...");
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

            // Tạm dừng game hoàn toàn khi hiện bảng Win/Lose
            Time.timeScale = 0f;
        }
    }
}
