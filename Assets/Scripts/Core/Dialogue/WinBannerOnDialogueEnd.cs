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
        if (winBannerController != null)
        {
            Debug.Log("[WinBannerOnDialogueEnd] Mở Win Banner...");
            winBannerController.ShowBanner();
        }
        else
        {
            Debug.LogError("[WinBannerOnDialogueEnd] Chưa gán WinBannerController!");
        }
    }
}