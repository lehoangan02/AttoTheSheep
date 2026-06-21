using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NumericUpDown : MonoBehaviour
{
    public TMP_InputField inputField;
    public Button upButton;
    public Button downButton;

    public int minValue = 2;
    public int maxValue = 10;
    private int currentValue = 4;

    private void Start()
    {
        if (inputField != null)
        {
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputField.text = currentValue.ToString();
            inputField.onValueChanged.AddListener(OnInputValueChanged);
            inputField.onEndEdit.AddListener(OnInputEndEdit);
        }

        if (upButton != null) upButton.onClick.AddListener(Increment);
        if (downButton != null) downButton.onClick.AddListener(Decrement);
        
        UpdateButtonStates();
    }

    private void Increment()
    {
        currentValue++;
        if (currentValue > maxValue) currentValue = maxValue;
        UpdateUI();
    }

    private void Decrement()
    {
        currentValue--;
        if (currentValue < minValue) currentValue = minValue;
        UpdateUI();
    }

    private void OnInputValueChanged(string text)
    {
        if (int.TryParse(text, out int val))
        {
            currentValue = val;
            UpdateButtonStates();
        }
    }

    private void OnInputEndEdit(string text)
    {
        if (int.TryParse(text, out int val))
        {
            currentValue = Mathf.Clamp(val, minValue, maxValue);
        }
        else
        {
            currentValue = minValue; // Default fallback
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (inputField != null) inputField.text = currentValue.ToString();
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        if (upButton != null) upButton.interactable = currentValue < maxValue;
        if (downButton != null) downButton.interactable = currentValue > minValue;
    }

    public int GetValue()
    {
        return currentValue;
    }
}
