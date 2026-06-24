using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;
using System;
using System.Collections;

namespace AttoTheSheep.UI.Shared
{
    public class SettingsUIController : MonoBehaviour
    {
        [Header("Audio Settings")]
        public AudioMixer mainAudioMixer;
        [Tooltip("Kéo Slider Âm thanh (SFX) vào đây")]
        public Slider soundSlider;
        public TextMeshProUGUI soundValueText;
        [Tooltip("Kéo Slider Nhạc nền (Music) vào đây")]
        public Slider musicSlider;
        public TextMeshProUGUI musicValueText;

        [Header("Language Settings")]
        [Tooltip("Kéo Dropdown Ngôn ngữ vào đây")]
        public TMP_Dropdown languageDropdown;
        // Event bắn ra toàn bộ hệ thống khi ngôn ngữ thay đổi (0: English, 1: Vietnamese)
        public static event Action<int> OnLanguageChanged; 

        [Header("FPS Settings")]
        [Tooltip("Kéo Dropdown FPS vào đây")]
        public TMP_Dropdown fpsDropdown; // Options: 30, 60, 120, Uncapped

        [Header("Username Settings")]
        public TMP_InputField usernameInput;
        public Button saveUsernameButton;

        // Keys lưu trữ cục bộ
        private const string PREF_SOUND = "Settings_SoundVol";
        private const string PREF_MUSIC = "Settings_MusicVol";
        private const string PREF_LANG = "Settings_Language";
        private const string PREF_FPS = "Settings_TargetFPS";

        private async void Start()
        {
            // 1. Gắn sự kiện (Listener)
            if (soundSlider != null) soundSlider.onValueChanged.AddListener(OnSoundSliderChanged);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(SetLanguage);
            if (fpsDropdown != null) fpsDropdown.onValueChanged.AddListener(SetFPSFromDropdown);
            if (saveUsernameButton != null) saveUsernameButton.onClick.AddListener(OnSaveUsernameClicked);

            // 2. Load thiết lập từ máy người chơi
            LoadSettings();

            // 3. Kết nối Cloud lấy tên người chơi
            await LoadCloudUsername();
        }

        private void LoadSettings()
        {
            // --- SOUND ---
            float soundVol = PlayerPrefs.GetFloat(PREF_SOUND, 0.8f); // Mặc định 80%
            if (soundSlider != null)
            {
                soundSlider.value = soundVol;
                if (soundValueText != null) soundValueText.text = Mathf.RoundToInt(soundSlider.value * 100) + "%";
            }
            SetSoundVolume(soundVol);

            // --- MUSIC ---
            float musicVol = PlayerPrefs.GetFloat(PREF_MUSIC, 0.8f);
            if (musicSlider != null)
            {
                musicSlider.value = musicVol;
                if (musicValueText != null) musicValueText.text = Mathf.RoundToInt(musicSlider.value * 100) + "%";
            }
            SetMusicVolume(musicVol);

            // --- LANGUAGE ---
            int lang = PlayerPrefs.GetInt(PREF_LANG, 0);
            if (languageDropdown != null) languageDropdown.value = lang;
            SetLanguage(lang);

            // --- FPS ---
            int fps;
            if (PlayerPrefs.HasKey(PREF_FPS))
            {
                // Người chơi đã từng chỉnh setting, lấy từ bộ nhớ
                fps = PlayerPrefs.GetInt(PREF_FPS);
            }
            else
            {
                // Lần đầu vào game: Đồng bộ lấy giá trị mặc định từ Inspector của FPSLimiter
                if (FPSLimiter.Instance != null)
                    fps = FPSLimiter.Instance.GetCurrentTargetFPS();
                else
                    fps = 60;
            }

            if (FPSLimiter.Instance != null) FPSLimiter.Instance.SetTargetFPS(fps);
            if (fpsDropdown != null)
            {
                if (fps == 30) fpsDropdown.value = 0;
                else if (fps == 60) fpsDropdown.value = 1;
                else if (fps == 120) fpsDropdown.value = 2;
                else fpsDropdown.value = 3; // Uncapped
            }
        }

        // ================= AUDIO LOGIC =================
        public void SetSoundVolume(float sliderValue)
        {
            if (mainAudioMixer != null)
            {
                // Công thức chuẩn chuyển từ % sang decibel (dB)
                float db = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
                mainAudioMixer.SetFloat("SFXVol", db);
            }
            PlayerPrefs.SetFloat(PREF_SOUND, sliderValue);
        }

        public void SetMusicVolume(float sliderValue)
        {
            if (mainAudioMixer != null)
            {
                float db = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
                mainAudioMixer.SetFloat("MusicVol", db);
            }
            PlayerPrefs.SetFloat(PREF_MUSIC, sliderValue);
        }

        private void OnSoundSliderChanged(float val)
        {
            if (soundValueText != null) soundValueText.text = Mathf.RoundToInt(val * 100) + "%";
            SetSoundVolume(val);
        }

        private void OnMusicSliderChanged(float val)
        {
            if (musicValueText != null) musicValueText.text = Mathf.RoundToInt(val * 100) + "%";
            SetMusicVolume(val);
        }

        // ================= LANGUAGE LOGIC =================
        public void SetLanguage(int languageIndex)
        {
            PlayerPrefs.SetInt(PREF_LANG, languageIndex);
            
            // Phát sóng (Broadcast) sự kiện đổi ngôn ngữ ra toàn bộ Game
            OnLanguageChanged?.Invoke(languageIndex);
            
            Debug.Log($"[Settings] Language changed to: {(languageIndex == 0 ? "English" : "Vietnamese")}");
        }

        // ================= FPS LOGIC =================
        public void SetFPSFromDropdown(int index)
        {
            int targetFPS = 60;
            switch (index)
            {
                case 0: targetFPS = 30; break;
                case 1: targetFPS = 60; break;
                case 2: targetFPS = 120; break;
                case 3: targetFPS = -1; break; // Uncapped
            }

            // Gọi logic của team
            if (FPSLimiter.Instance != null)
            {
                FPSLimiter.Instance.SetTargetFPS(targetFPS);
            }
            PlayerPrefs.SetInt(PREF_FPS, targetFPS);
        }

        // ================= CLOUD USERNAME LOGIC =================
        private async Task LoadCloudUsername()
        {
            TextMeshProUGUI btnText = saveUsernameButton != null ? saveUsernameButton.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (btnText != null) btnText.text = "Loading...";

            try
            {
                // Đảm bảo Unity Services đã chạy
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync();
                }

                // Đăng nhập ẩn danh nếu chưa đăng nhập
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                // Lấy tên từ Cloud
                string currentName = await AuthenticationService.Instance.GetPlayerNameAsync();
                if (usernameInput != null)
                {
                    usernameInput.text = string.IsNullOrEmpty(currentName) ? "Guest" : currentName;
                }
                if (btnText != null) btnText.text = "Save";
            }
            catch (Exception ex)
            {
                Debug.LogError("[Settings] Failed to fetch Cloud Username: " + ex.Message);
                if (btnText != null) btnText.text = "Offline";
            }
        }

        private async void OnSaveUsernameClicked()
        {
            if (usernameInput == null || string.IsNullOrWhiteSpace(usernameInput.text)) return;
            
            TextMeshProUGUI btnText = saveUsernameButton.GetComponentInChildren<TextMeshProUGUI>();
            saveUsernameButton.interactable = false;

            // Start spinner
            Coroutine spinner = null;
            if (btnText != null) spinner = StartCoroutine(AnimateButtonLoading(btnText));

            try
            {
                string newName = usernameInput.text.Trim();
                
                // Gọi API đẩy tên lên thẳng Unity Cloud Dashboard
                await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
                
                if (spinner != null) StopCoroutine(spinner);
                if (btnText != null) btnText.text = "Saved!";
                Debug.Log($"[Settings] Successfully saved cloud username: {newName}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[Settings] Failed to save Cloud Username: " + ex.Message);
                if (spinner != null) StopCoroutine(spinner);
                if (btnText != null) btnText.text = "Error!";
            }
            finally
            {
                saveUsernameButton.interactable = true;
                // Trả về chữ Save sau 2 giây
                await Task.Delay(2000);
                if (btnText != null && btnText.text != "Loading...") btnText.text = "Save";
            }
        }

        private IEnumerator AnimateButtonLoading(TextMeshProUGUI txt)
        {
            string[] frames = new string[] { "|", "/", "-", "\\" };
            int i = 0;
            while (true)
            {
                txt.text = frames[i];
                i = (i + 1) % frames.Length;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
