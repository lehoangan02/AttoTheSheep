using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MatchMakingController : MonoBehaviour
{
    // ==========================================
    // SERVER / NETWORK EVENTS
    // ==========================================
    public event System.Action<string> OnRequestCreateLobby;
    public event System.Action<string> OnRequestJoinPrivateLobby;
    public event System.Action<string> OnRequestJoinPublicLobby;

    [Header("Scene References")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    [Header("UI Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinPrivateButton;
    
    [Header("Modals Shared Overlay")]
    [SerializeField] private GameObject modalOverlay;

    [Header("Create Lobby Modal")]
    [SerializeField] private GameObject createLobbyModal;
    [SerializeField] private Button createCancelButton;
    [SerializeField] private Button createOkButton;
    [SerializeField] private TMP_InputField createNameInput;

    [Header("Join Private Modal")]
    [SerializeField] private GameObject joinPrivateModal;
    [SerializeField] private Button joinCancelButton;
    [SerializeField] private Button joinOkButton;
    [SerializeField] private TMP_InputField joinCodeInput;

    [Header("Confirm Join Modal")]
    [SerializeField] private GameObject confirmJoinModal;
    [SerializeField] private Button confirmCancelButton;
    [SerializeField] private Button confirmOkButton;
    [SerializeField] private TextMeshProUGUI confirmJoinText;

    [Header("Lobby List Content")]
    [SerializeField] private Transform lobbyContent;
    [SerializeField] private GameObject lobbyRowPrefab;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.4f;

    private bool _isTransitioning;
    private string _selectedLobbyId = "";
    private string _selectedLobbyDisplayName = "";

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        if (createLobbyButton != null) createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        if (joinPrivateButton != null) joinPrivateButton.onClick.AddListener(OnJoinPrivateClicked);

        if (createCancelButton != null) createCancelButton.onClick.AddListener(CloseAllModals);
        if (createOkButton != null) createOkButton.onClick.AddListener(OnCreateOkClicked);

        if (joinCancelButton != null) joinCancelButton.onClick.AddListener(CloseAllModals);
        if (joinOkButton != null) joinOkButton.onClick.AddListener(OnJoinOkClicked);

        if (confirmCancelButton != null) confirmCancelButton.onClick.AddListener(CloseAllModals);
        if (confirmOkButton != null) confirmOkButton.onClick.AddListener(OnConfirmOkClicked);

        CloseAllModals();

        if (fadeOverlay != null)
        {
            var fadeImg = fadeOverlay.GetComponent<UnityEngine.UI.Image>();
            var bgObj = GameObject.Find("Background");
            
            // If we have a sharp background sprite, use it for the overlay
            Sprite sharpBg = null;
            #if UNITY_EDITOR
            sharpBg = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/bg.png");
            #endif
            
            if (fadeImg != null && sharpBg != null)
            {
                fadeImg.sprite = sharpBg;
                fadeImg.color = Color.white;
            }

            fadeOverlay.alpha = 1f;
            fadeOverlay.blocksRaycasts = true;
            StartCoroutine(FadeIn());
        }

        if (LobbyManager.Instance != null && LobbyManager.Instance.Presenter != null)
        {
            LobbyManager.Instance.Presenter.OnLobbyListUpdated += OnRealLobbyListUpdated;
            LobbyManager.Instance.ListLobbies(); // Fetch real lobbies immediately
        }
        else
        {
            GenerateMockLobbyList();
        }
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null && LobbyManager.Instance.Presenter != null)
        {
            LobbyManager.Instance.Presenter.OnLobbyListUpdated -= OnRealLobbyListUpdated;
        }
    }

    private void OnRealLobbyListUpdated()
    {
        var realLobbies = new System.Collections.Generic.List<AttoTheSheep.Core.LobbyData>();
        
        if (LobbyManager.Instance.Presenter.AvailableLobbies != null)
        {
            Debug.Log($"[MatchMaking] Auto-Refresh: Found {LobbyManager.Instance.Presenter.AvailableLobbies.Count} lobbies from server.");
            foreach (var unityLobby in LobbyManager.Instance.Presenter.AvailableLobbies)
            {
                string customCode = "Hidden";
                if (unityLobby.Data != null && unityLobby.Data.TryGetValue("PublicLobbyCode", out var codeData))
                {
                    customCode = codeData.Value;
                }

                realLobbies.Add(new AttoTheSheep.Core.LobbyData
                {
                    LobbyId = unityLobby.Id,
                    LobbyName = unityLobby.Name,
                    CurrentPlayers = unityLobby.Players.Count,
                    MaxPlayers = unityLobby.MaxPlayers,
                    GameMode = "Multiplayer", // Always Multiplayer
                    IsPrivate = unityLobby.IsPrivate,
                    LobbyCode = customCode 
                });
            }
        }
        PopulateLobbies(realLobbies);
    }

    private void GenerateMockLobbyList()
    {
        var mockLobbies = new System.Collections.Generic.List<AttoTheSheep.Core.LobbyData>();
        for (int i = 1; i <= 5; i++)
        {
            mockLobbies.Add(new AttoTheSheep.Core.LobbyData
            {
                LobbyId = $"Lobby_ID_{i}",
                LobbyName = $"Public Lobby {i}",
                CurrentPlayers = Random.Range(1, 10),
                MaxPlayers = 10,
                GameMode = "Multiplayer",
                IsPrivate = false,
                LobbyCode = $"XYZ{i}9{i}"
            });
        }

        PopulateLobbies(mockLobbies);
    }

    /// <summary>
    /// Gọi hàm này từ Server/NetworkManager để cập nhật danh sách các phòng chờ
    /// </summary>
    public void PopulateLobbies(System.Collections.Generic.List<AttoTheSheep.Core.LobbyData> lobbies)
    {
        if (lobbyRowPrefab != null && lobbyContent != null)
        {
            // Xóa các row tĩnh hoặc row cũ đi
            foreach (Transform child in lobbyContent)
            {
                Destroy(child.gameObject);
            }

            // Render các row mới từ data truyền vào
            foreach (var lobby in lobbies)
            {
                var rowObj = Instantiate(lobbyRowPrefab, lobbyContent);
                var rowUI = rowObj.GetComponent<LobbyRowUI>();
                if (rowUI != null)
                {
                    rowUI.Setup(lobby, OnLobbyRowClicked);
                }
            }
        }
    }

    private void OnBackClicked()
    {
        if (_isTransitioning) return;
        StartCoroutine(FadeAndLoad(mainMenuSceneName));
    }

    private void OnCreateLobbyClicked()
    {
        if (_isTransitioning) return;
        CloseAllModals();
        if (modalOverlay != null) modalOverlay.SetActive(true);
        if (createLobbyModal != null)
        {
            createLobbyModal.SetActive(true);
            if (createNameInput != null)
            {
                createNameInput.text = "";
                createNameInput.Select();
                createNameInput.ActivateInputField();
            }
        }
    }

    private void OnJoinPrivateClicked()
    {
        if (_isTransitioning) return;
        CloseAllModals();
        if (modalOverlay != null) modalOverlay.SetActive(true);
        if (joinPrivateModal != null)
        {
            joinPrivateModal.SetActive(true);
            if (joinCodeInput != null)
            {
                joinCodeInput.text = "";
                joinCodeInput.Select();
                joinCodeInput.ActivateInputField();
            }
        }
    }

    private void OnLobbyRowClicked(AttoTheSheep.Core.LobbyData data)
    {
        if (_isTransitioning) return;
        _selectedLobbyId = data.LobbyId;
        _selectedLobbyDisplayName = data.LobbyName;
        CloseAllModals();
        if (modalOverlay != null) modalOverlay.SetActive(true);
        if (confirmJoinModal != null)
        {
            confirmJoinModal.SetActive(true);
            if (confirmJoinText != null)
            {
                confirmJoinText.text = $"Do you want to join '{data.LobbyName}'?";
            }
        }
    }

    private void CloseAllModals()
    {
        if (modalOverlay != null) modalOverlay.SetActive(false);
        if (createLobbyModal != null) createLobbyModal.SetActive(false);
        if (joinPrivateModal != null) joinPrivateModal.SetActive(false);
        if (confirmJoinModal != null) confirmJoinModal.SetActive(false);
    }

    private async void OnCreateOkClicked()
    {
        if (_isTransitioning) return;
        if (createNameInput != null && !string.IsNullOrEmpty(createNameInput.text))
        {
            _isTransitioning = true;
            Debug.Log($"[MatchMaking] Creating lobby: {createNameInput.text}");
            CloseAllModals();

            if (LobbyManager.Instance != null)
            {
                AttoTheSheep.Core.LoadingManager.Instance?.Show("msg_creating_lobby");
                string defaultPlayerName = await GetCloudPlayerName();
                try 
                {
                    await LobbyManager.Instance.Presenter.CreateLobby(createNameInput.text, 10, false, defaultPlayerName);
                    await LobbyManager.Instance.Presenter.SubscribeToCurrentLobby();
                    
                    AttoTheSheep.Core.LobbySession.CurrentLobbyName = createNameInput.text;
                    AttoTheSheep.Core.LobbySession.MaxPlayers = 10;
                    AttoTheSheep.Core.LobbySession.IsHost = true;

                    StartCoroutine(FadeAndLoad("WaitLobby"));
                }
                catch (System.Exception e)
                {
                    _isTransitioning = false;
                    AttoTheSheep.Core.LoadingManager.Instance?.Hide();
                    Debug.LogError($"[MatchMaking] Failed to create lobby: {e}");
                }
            }
            else if (OnRequestCreateLobby != null)
            {
                OnRequestCreateLobby.Invoke(createNameInput.text);
            }
            else
            {
                // Fallback Mock Logic
                AttoTheSheep.Core.LobbySession.CurrentLobbyName = createNameInput.text;
                AttoTheSheep.Core.LobbySession.MaxPlayers = 4; // Mock
                AttoTheSheep.Core.LobbySession.IsHost = true;
                StartCoroutine(FadeAndLoad("WaitLobby"));
            }
        }
    }

    private async void OnJoinOkClicked()
    {
        if (_isTransitioning) return;
        if (joinCodeInput != null && !string.IsNullOrEmpty(joinCodeInput.text))
        {
            _isTransitioning = true;
            Debug.Log($"[MatchMaking] Joining private lobby code: {joinCodeInput.text}");
            CloseAllModals();

            if (LobbyManager.Instance != null)
            {
                AttoTheSheep.Core.LoadingManager.Instance?.Show("msg_joining_lobby");
                string defaultPlayerName = await GetCloudPlayerName();
                try 
                {
                    await LobbyManager.Instance.Presenter.JoinLobbyByCode(joinCodeInput.text, defaultPlayerName);
                    await LobbyManager.Instance.Presenter.SubscribeToCurrentLobby();

                    AttoTheSheep.Core.LobbySession.CurrentLobbyName = "Private Lobby";
                    AttoTheSheep.Core.LobbySession.MaxPlayers = 10;
                    AttoTheSheep.Core.LobbySession.IsHost = false;

                    StartCoroutine(FadeAndLoad("WaitLobby"));
                }
                catch (System.Exception e)
                {
                    _isTransitioning = false;
                    AttoTheSheep.Core.LoadingManager.Instance?.Hide();
                    Debug.LogError($"[MatchMaking] Failed to join private lobby: {e}");
                }
            }
            else if (OnRequestJoinPrivateLobby != null)
            {
                OnRequestJoinPrivateLobby.Invoke(joinCodeInput.text);
            }
            else
            {
                // Fallback Mock Logic
                AttoTheSheep.Core.LobbySession.CurrentLobbyName = "Private " + joinCodeInput.text;
                AttoTheSheep.Core.LobbySession.MaxPlayers = 4;
                AttoTheSheep.Core.LobbySession.IsHost = false;
                StartCoroutine(FadeAndLoad("WaitLobby"));
            }
        }
    }

    private async void OnConfirmOkClicked()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        
        Debug.Log($"[MatchMaking] Joining public lobby ID: {_selectedLobbyId}");
        CloseAllModals();

        if (LobbyManager.Instance != null)
        {
            AttoTheSheep.Core.LoadingManager.Instance?.Show("msg_joining_lobby");
            string defaultPlayerName = await GetCloudPlayerName();
            try 
            {
                await LobbyManager.Instance.Presenter.JoinLobby(_selectedLobbyId, defaultPlayerName);
                await LobbyManager.Instance.Presenter.SubscribeToCurrentLobby();

                AttoTheSheep.Core.LobbySession.CurrentLobbyName = _selectedLobbyDisplayName;
                AttoTheSheep.Core.LobbySession.MaxPlayers = 10;
                AttoTheSheep.Core.LobbySession.IsHost = false;

                StartCoroutine(FadeAndLoad("WaitLobby"));
            }
            catch (System.Exception e)
            {
                _isTransitioning = false;
                AttoTheSheep.Core.LoadingManager.Instance?.Hide();
                Debug.LogError($"[MatchMaking] Failed to join public lobby: {e}");
            }
        }
        else if (OnRequestJoinPublicLobby != null)
        {
            OnRequestJoinPublicLobby.Invoke(_selectedLobbyId); // Pass LobbyId
        }
        else
        {
            // Fallback Mock Logic
            AttoTheSheep.Core.LobbySession.CurrentLobbyName = _selectedLobbyDisplayName;
            AttoTheSheep.Core.LobbySession.MaxPlayers = 4;
            AttoTheSheep.Core.LobbySession.IsHost = false;
            StartCoroutine(FadeAndLoad("WaitLobby"));
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeOverlay.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        fadeOverlay.alpha = 0f;
        fadeOverlay.blocksRaycasts = false;
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        _isTransitioning = true;
        if (fadeOverlay != null)
        {
            fadeOverlay.blocksRaycasts = true;
            var fadeImage = fadeOverlay.GetComponent<UnityEngine.UI.Image>();
            
            Sprite sharpBg = null;
            #if UNITY_EDITOR
            sharpBg = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/bg.png");
            #endif
            
            if (fadeImage != null && sharpBg != null)
            {
                fadeImage.sprite = sharpBg;
                fadeImage.color = Color.white;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            fadeOverlay.alpha = 1f;
        }
        SceneManager.LoadScene(sceneName);
    }

    public void Setup(Button back, Button create, Button joinPrivate, 
                      GameObject overlay,
                      GameObject createModal, Button createCancel, Button createOk, TMP_InputField createName,
                      GameObject joinModal, Button joinCancel, Button joinOk, TMP_InputField joinCode,
                      GameObject confirmModal, Button confirmCancel, Button confirmOk, TextMeshProUGUI confirmText,
                      Transform content, GameObject rowPrefab, CanvasGroup fade)
    {
        backButton = back;
        createLobbyButton = create;
        this.joinPrivateButton = joinPrivate;
        
        modalOverlay = overlay;
        
        createLobbyModal = createModal;
        createCancelButton = createCancel;
        createOkButton = createOk;
        createNameInput = createName;
        
        joinPrivateModal = joinModal;
        joinCancelButton = joinCancel;
        joinOkButton = joinOk;
        joinCodeInput = joinCode;
        
        confirmJoinModal = confirmModal;
        confirmCancelButton = confirmCancel;
        confirmOkButton = confirmOk;
        confirmJoinText = confirmText;
        
        lobbyContent = content;
        lobbyRowPrefab = rowPrefab;
        fadeOverlay = fade;
    }

    private async System.Threading.Tasks.Task<string> GetCloudPlayerName()
    {
        string defaultName = "Player_" + Random.Range(1000, 9999);
        try
        {
            if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized 
                && Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
            {
                string cloudName = await Unity.Services.Authentication.AuthenticationService.Instance.GetPlayerNameAsync();
                if (!string.IsNullOrEmpty(cloudName)) return cloudName;
            }
        }
        catch {}
        return defaultName;
    }
}
