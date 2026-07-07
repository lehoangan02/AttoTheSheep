using UnityEngine;
using System.IO;

public class ScreenshotManager : MonoBehaviour
{
    public static ScreenshotManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("The key to press to take a screenshot.")]
    public KeyCode screenshotKey = KeyCode.Alpha9;
    
    [Tooltip("Size multiplier. 1 is normal screen resolution. 2 is double, etc.")]
    public int superSize = 1; 

    private void Awake()
    {
        // Standard Singleton Pattern that persists across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeScreenshot();
        }
    }

    /// <summary>
    /// Captures the screen and saves it to a "Screenshots" folder.
    /// </summary>
    public void TakeScreenshot()
    {
        string directoryPath;
        
        // In the editor, save it to a folder right next to your Assets folder.
        // In a built game, save it to the OS's persistent data path.
#if UNITY_EDITOR
        directoryPath = Path.Combine(Application.dataPath, "../Screenshots");
#else
        directoryPath = Path.Combine(Application.persistentDataPath, "Screenshots");
#endif

        // Ensure the directory exists
        directoryPath = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Generate a unique filename based on the current date and time
        string fileName = "Screenshot_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
        string filePath = Path.Combine(directoryPath, fileName);
        
        // Capture!
        ScreenCapture.CaptureScreenshot(filePath, superSize);
        
        Debug.Log($"[ScreenshotManager] 📸 Screenshot saved to: {filePath}");
    }
}
