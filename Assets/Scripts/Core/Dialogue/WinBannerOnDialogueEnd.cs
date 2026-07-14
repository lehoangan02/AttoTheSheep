using UnityEngine;
using AttoTheSheep.UI.InGame; // Import namespace chứa WinBannerController

public class WinBannerOnDialogueEnd : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo WinBannerController từ Hierarchy vào đây")]
    [SerializeField] private WinBannerController winBannerController;

    /// <summary>
    /// Hàm này sẽ được gọi khi hội thoại với Boss/NPC Quest kết thúc.
    /// </summary>
    public void ShowWinBanner()
    {
        // Don't show the win banner if we are in the FTUE (tutorial) scene
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "FTUE")
        {
            Debug.Log("[WinBannerOnDialogueEnd] FTUE scene detected. Skipping Win Banner.");
            return;
        }

        var uiManager = Object.FindFirstObjectByType<LevelUIManager>();
        if (uiManager != null)
        {
            Debug.Log("[WinBannerOnDialogueEnd] Mở Win Banner thông qua LevelUIManager...");
            uiManager.ShowWinBannerNow();
        }
        else
        {
            Debug.LogError("[WinBannerOnDialogueEnd] Chưa tìm thấy LevelUIManager trong Scene!");
        }
    }
}