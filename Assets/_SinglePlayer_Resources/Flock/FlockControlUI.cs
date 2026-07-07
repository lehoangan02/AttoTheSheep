using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nếu dùng Text thường của Unity cũ thì đổi thành UnityEngine.UI.Text

public class FlockControlUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI buttonText; 

    [Header("Settings - Auto Mode")]
    [SerializeField] private string autoModeText = "AUTO";
    [SerializeField] private float autoFontSize = 18f; // Kích thước chữ khi ở chế độ Auto

    [Header("Settings - Manual Mode")]
    [SerializeField] private string manualModeText = "MANUAL";
    [SerializeField] private float manualFontSize = 14f; // Kích thước chữ khi ở chế độ Manual

    private FlockManager flockManager;

    void Start()
    {
        flockManager = Object.FindFirstObjectByType<FlockManager>();

        if (flockManager != null)
        {
            toggleButton.onClick.AddListener(OnButtonClicked);
            flockManager.OnControlModeChanged += UpdateUI;
            
            // Cập nhật giao diện ban đầu
            UpdateUI(flockManager.currentControlMode);
        }
        else
        {
            Debug.LogError("[FlockControlUI] Không tìm thấy FlockManager trong Scene!");
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
            buttonText.fontSize = autoFontSize; // Đổi kích thước chữ cho Auto
        }
        else
        {
            buttonText.text = manualModeText;
            buttonText.color = Color.yellow;
            buttonText.fontSize = manualFontSize; // Đổi kích thước chữ cho Manual
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