using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MatchMakingController : MonoBehaviour
{
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
    private string _selectedLobbyName = "";

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
        GenerateMockLobbyList();

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

        GenerateMockLobbyList();
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
                GameMode = Random.value > 0.5f ? "Custom" : "Classic",
                IsPrivate = false
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

    private void OnLobbyRowClicked(string lobbyName)
    {
        _selectedLobbyName = lobbyName;
        CloseAllModals();
        if (modalOverlay != null) modalOverlay.SetActive(true);
        if (confirmJoinModal != null)
        {
            confirmJoinModal.SetActive(true);
            if (confirmJoinText != null)
            {
                confirmJoinText.text = $"Do you want to join '{lobbyName}'?";
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

    private void OnCreateOkClicked()
    {
        if (createNameInput != null && !string.IsNullOrEmpty(createNameInput.text))
        {
            Debug.Log($"[MatchMaking] Creating lobby: {createNameInput.text}");
            AttoTheSheep.Core.LobbySession.CurrentLobbyName = createNameInput.text;
            AttoTheSheep.Core.LobbySession.MaxPlayers = 4; // Mock
            AttoTheSheep.Core.LobbySession.IsHost = true;
            CloseAllModals();
            StartCoroutine(FadeAndLoad("WaitLobby"));
        }
    }

    private void OnJoinOkClicked()
    {
        if (joinCodeInput != null && !string.IsNullOrEmpty(joinCodeInput.text))
        {
            Debug.Log($"[MatchMaking] Joining private lobby code: {joinCodeInput.text}");
            AttoTheSheep.Core.LobbySession.CurrentLobbyName = "Private " + joinCodeInput.text;
            AttoTheSheep.Core.LobbySession.MaxPlayers = 4;
            AttoTheSheep.Core.LobbySession.IsHost = false;
            CloseAllModals();
            StartCoroutine(FadeAndLoad("WaitLobby"));
        }
    }

    private void OnConfirmOkClicked()
    {
        Debug.Log($"[MatchMaking] Joining public lobby: {_selectedLobbyName}");
        AttoTheSheep.Core.LobbySession.CurrentLobbyName = _selectedLobbyName;
        AttoTheSheep.Core.LobbySession.MaxPlayers = 4;
        AttoTheSheep.Core.LobbySession.IsHost = false;
        CloseAllModals();
        StartCoroutine(FadeAndLoad("WaitLobby"));
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
}
