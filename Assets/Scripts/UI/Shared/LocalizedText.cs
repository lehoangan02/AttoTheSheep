using UnityEngine;
using TMPro;
using AttoTheSheep.Core;

namespace AttoTheSheep.UI.Shared
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("Mã ID của từ vựng (Ví dụ: settings_sound)")]
        public string textID;

        private TextMeshProUGUI _textComponent;

        private void Awake()
        {
            _textComponent = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += UpdateText;
            UpdateText();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= UpdateText;
        }

        private void UpdateText()
        {
            if (LocalizationManager.Instance == null) return;
            if (string.IsNullOrEmpty(textID)) return;

            string translated = LocalizationManager.Instance.GetText(textID);
            if (!string.IsNullOrEmpty(translated))
            {
                _textComponent.text = translated;
            }
        }

        public void SetTextID(string newID)
        {
            textID = newID;
            UpdateText();
        }
    }
}
