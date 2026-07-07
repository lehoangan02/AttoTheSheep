using UnityEngine;

namespace AttoTheSheep.UI.Mobile
{
    /// <summary>
    /// Convenience helper that auto-populates an OnScreenControl's <c>controlPath</c>
    /// from a serialized string, simplifying prefab wiring for on-screen sticks and buttons.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.InputSystem.OnScreen.OnScreenControl))]
    public class OnScreenControlsBinder : MonoBehaviour
    {
        [SerializeField]
        private UnityEngine.InputSystem.InputActionReference action;

        [SerializeField]
        private string controlPath = "<Gamepad>/leftStick";

        private void Awake()
        {
            var onScreen = GetComponent<UnityEngine.InputSystem.OnScreen.OnScreenControl>();
            if (onScreen != null)
            {
                if (string.IsNullOrEmpty(onScreen.controlPath))
                {
                    onScreen.controlPath = controlPath;
                }
            }
            else
            {
                Debug.LogWarning($"[OnScreenControlsBinder] No OnScreenControl component found on {gameObject.name}. Expected OnScreenStick or OnScreenButton.");
            }
        }
    }
}
