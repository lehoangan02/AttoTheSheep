using UnityEngine;
using AttoTheSheep.UI.InGame;

public class WinBannerOnDialogueEnd : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo WinBannerController từ Hierarchy vào đây")]
    [SerializeField] private WinBannerController winBannerController;

    /// <summary>

    /// </summary>
    public void ShowWinBanner()
    {
        // Don't show the win banner if we are in the FTUE (tutorial) scene
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "FTUE")
        {

            return;
        }

        var uiManager = Object.FindFirstObjectByType<LevelUIManager>();
        if (uiManager != null)
        {

            uiManager.ShowWinBannerNow();
        }
        else
        {

        }
    }
}