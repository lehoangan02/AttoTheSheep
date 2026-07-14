using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Auto-activates the on-screen mobile control canvas on Android/iOS
/// or when a Touchscreen is detected at runtime.
///
/// OnScreenStick and OnScreenButton components synthesize a virtual Gamepad;
/// PlayerInput then auto-switches to the Touch control scheme when that
/// virtual Gamepad device appears.
/// </summary>
public class MobileControlsUI : MonoBehaviour
{
    [SerializeField]
    private bool forceShowInEditor = false;

    [SerializeField]
    private GameObject mobileControlsCanvas;

    private void Awake()
    {
        bool shouldShow = forceShowInEditor
            || Application.isMobilePlatform
            || Touchscreen.current != null;

        if (!shouldShow)
        {
            gameObject.SetActive(false);
            return;
        }

        if (mobileControlsCanvas != null)
            mobileControlsCanvas.SetActive(true);
    }
}
