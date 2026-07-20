using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;

public class ScreenshotManager : MonoBehaviour
{
    public static ScreenshotManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Input Action Reference for screenshot (Keyboard 9). Assign in inspector or leave empty for auto-bind.")]
    public InputActionReference screenshotAction;

    [Tooltip("Size multiplier. 1 is normal screen resolution. 2 is double, etc.")]
    public int superSize = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (screenshotAction == null)
            TryAutoBindScreenshotAction();
    }

    private void OnEnable()
    {
        if (screenshotAction != null)
        {
            screenshotAction.action.Enable();
            screenshotAction.action.performed += OnScreenshot;
        }
    }

    private void OnDisable()
    {
        if (screenshotAction != null)
        {
            screenshotAction.action.performed -= OnScreenshot;
            screenshotAction.action.Disable();
        }
    }

    private void TryAutoBindScreenshotAction()
    {
        var assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        foreach (var asset in assets)
        {
            var action = asset.FindAction("Screenshot");
            if (action != null)
            {
                screenshotAction = InputActionReference.Create(action);

                return;
            }
        }

    }

    private void OnScreenshot(InputAction.CallbackContext ctx)
    {
        TakeScreenshot();
    }

    public void TakeScreenshot()
    {
        string directoryPath;

#if UNITY_EDITOR
        directoryPath = Path.Combine(Application.dataPath, "../Screenshots");
#else
        directoryPath = Path.Combine(Application.persistentDataPath, "Screenshots");
#endif

        directoryPath = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        string fileName = "Screenshot_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
        string filePath = Path.Combine(directoryPath, fileName);

        ScreenCapture.CaptureScreenshot(filePath, superSize);

    }
}
