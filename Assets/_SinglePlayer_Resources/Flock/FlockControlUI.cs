using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlockControlUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Settings - Auto Mode")]
    [SerializeField] private string autoModeText = "AUTO";
    [SerializeField] private float autoFontSize = 18f;

    [Header("Settings - Manual Mode")]
    [SerializeField] private string manualModeText = "MANUAL";
    [SerializeField] private float manualFontSize = 14f;

    private FlockManager flockManager;

    void Start()
    {
        flockManager = Object.FindFirstObjectByType<FlockManager>();

        if (flockManager != null)
        {
            toggleButton.onClick.AddListener(OnButtonClicked);
            flockManager.OnControlModeChanged += UpdateUI;

            UpdateUI(flockManager.currentControlMode);
        }
        else
        {

            gameObject.SetActive(false);
        }
    }

    private void OnButtonClicked()
    {
        if (flockManager != null)
        {
            flockManager.RequestToggleControlMode();
        }
    }

    private void UpdateUI(FlockControlMode currentMode)
    {
        if (buttonText == null) return;

        if (currentMode == FlockControlMode.Auto)
        {
            buttonText.text = autoModeText;
            buttonText.color = Color.red;
            buttonText.fontSize = autoFontSize;
        }
        else
        {
            buttonText.text = manualModeText;
            buttonText.color = Color.yellow;
            buttonText.fontSize = manualFontSize;
        }
    }

    void OnDestroy()
    {
        if (flockManager != null)
        {
            flockManager.OnControlModeChanged -= UpdateUI;
        }
    }
}