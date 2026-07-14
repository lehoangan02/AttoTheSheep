using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;

namespace AttoTheSheep.Core
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        public static event Action OnLanguageChanged;

        private Dictionary<string, string> _localizedText = new Dictionary<string, string>();
        
        // 0: English, 1: Vietnamese (Must match Dropdown indexes)
        private int _currentLanguageIndex = 0;
        public int CurrentLanguageIndex => _currentLanguageIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Tải ngôn ngữ đã lưu
            _currentLanguageIndex = PlayerPrefs.GetInt("Settings_Language", 0);
            LoadDictionaryForLanguage(_currentLanguageIndex);
        }

        public void ChangeLanguage(int languageIndex)
        {
            if (_currentLanguageIndex == languageIndex) return;

            _currentLanguageIndex = languageIndex;
            PlayerPrefs.SetInt("Settings_Language", languageIndex);
            
            LoadDictionaryForLanguage(languageIndex);

            // Bắn event để tất cả các Text đang hiển thị tự động cập nhật
            OnLanguageChanged?.Invoke();
        }

        private void LoadDictionaryForLanguage(int languageIndex)
        {
            string fileName = languageIndex == 0 ? "English" : "Vietnamese";
            
            // Đọc file JSON từ thư mục Resources/Translations/
            TextAsset textAsset = Resources.Load<TextAsset>($"Translations/{fileName}");
            
            if (textAsset != null)
            {
                try
                {
                    var rawDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);
                    _localizedText = new Dictionary<string, string>();
                    if (rawDict != null)
                    {
                        foreach (var kvp in rawDict)
                        {
                            string normalizedKey = kvp.Key.Replace("\r\n", "\n");
                            _localizedText[normalizedKey] = kvp.Value;
                        }
                    }
                    Debug.Log($"[Localization] Loaded {fileName}.json with {_localizedText.Count} entries.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Localization] Failed to parse {fileName}.json: " + e.Message);
                    _localizedText = new Dictionary<string, string>();
                }
            }
            else
            {
                Debug.LogWarning($"[Localization] Could not find Resources/Translations/{fileName}.json");
                _localizedText = new Dictionary<string, string>();
            }
        }

        public string GetText(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            
            string normalizedKey = key.Replace("\r\n", "\n");

            if (_localizedText != null && _localizedText.TryGetValue(normalizedKey, out string translated))
            {
                return translated;
            }

            // Nếu không tìm thấy, trả về nguyên dạng Key để dev dễ phát hiện thiếu sót
            return key;
        }
    }
}
