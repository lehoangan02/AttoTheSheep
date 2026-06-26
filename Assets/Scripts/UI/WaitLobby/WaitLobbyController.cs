using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class WaitLobbyController : MonoBehaviour
{
    // ==========================================
    // SERVER / NETWORK EVENTS
    // ==========================================
    public event System.Action OnRequestLeaveLobby;
    public event System.Action OnRequestReady;
    public event System.Action<string> OnRequestKickPlayer;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerRowPrefab;

    [SerializeField] private Button leaveButton;
    [SerializeField] private Button readyButton;

    [Header("Confirm Panel")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Mock Data Testing")]
    [SerializeField] private Sprite[] mockAvatars;

    private List<AttoTheSheep.Core.PlayerData> _players = new List<AttoTheSheep.Core.PlayerData>();

    private void Start()
    {
        AttoTheSheep.Core.LoadingManager.Instance?.Hide();

        if (leaveButton != null) leaveButton.onClick.AddListener(OnLeaveClicked);
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);

        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmYesClicked);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(OnConfirmNoClicked);

        if (LobbyManager.Instance != null && LobbyManager.Instance.Presenter != null)
        {
            LobbyManager.Instance.Presenter.OnJoinedLobbyUpdated += OnJoinedLobbyUpdated;
            UpdateRealLobbyData(LobbyManager.Instance.Presenter.JoinedLobby);
        }
        else
        {
            Debug.LogWarning("[WaitLobbyController] No LobbyManager found! UI will be empty.");
            RefreshUI();
        }
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null && LobbyManager.Instance.Presenter != null)
        {
            LobbyManager.Instance.Presenter.OnJoinedLobbyUpdated -= OnJoinedLobbyUpdated;
        }
    }

    private void OnJoinedLobbyUpdated(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        if (lobby == null)
        {
            Debug.Log("[WaitLobby] Lobby was deleted or left. Returning to MatchMaking.");
            SceneManager.LoadScene("MatchMaking");
            return;
        }
        UpdateRealLobbyData(lobby);
    }

    private void UpdateRealLobbyData(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        if (lobby == null) return;

        _players.Clear();
        foreach (var p in lobby.Players)
        {
            string pName = "Unknown";
            if (p.Data != null && p.Data.TryGetValue("PlayerName", out var dataObj))
            {
                pName = dataObj.Value;
            }

            bool isHost = (lobby.HostId == p.Id);
            bool isMe = (p.Id == Unity.Services.Authentication.AuthenticationService.Instance.PlayerId);

            _players.Add(new AttoTheSheep.Core.PlayerData 
            {
                PlayerId = p.Id,
                PlayerName = pName,
                IsHost = isHost,
                IsLocalPlayer = isMe,
                AvatarIndex = 0 // Mock avatar for now
            });
        }

        AttoTheSheep.Core.LobbySession.CurrentLobbyName = lobby.Name;
        AttoTheSheep.Core.LobbySession.MaxPlayers = lobby.MaxPlayers;
        AttoTheSheep.Core.LobbySession.IsHost = (lobby.HostId == Unity.Services.Authentication.AuthenticationService.Instance.PlayerId);

        // Hide ready button if not host
        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(AttoTheSheep.Core.LobbySession.IsHost);
        }

        RefreshUI();
    }

    private Sprite GetAvatarSprite(int index)
    {
        if (mockAvatars == null || mockAvatars.Length == 0) return null;
        if (index < 0 || index >= mockAvatars.Length) return mockAvatars[0];
        return mockAvatars[index];
    }

    private void RefreshUI()
    {
        // Update headers
        if (lobbyNameText != null) lobbyNameText.text = "JOINED LOBBY: " + AttoTheSheep.Core.LobbySession.CurrentLobbyName;
        if (playerCountText != null) playerCountText.text = $"{_players.Count}/{AttoTheSheep.Core.LobbySession.MaxPlayers}";
        if (gameModeText != null) gameModeText.text = "Conquest";

        PopulatePlayers(_players);
    }

    /// <summary>
    /// Gọi hàm này từ Server/NetworkManager để cập nhật danh sách người chơi trong phòng
    /// </summary>
    public void PopulatePlayers(List<AttoTheSheep.Core.PlayerData> players)
    {
        // Clear existing rows
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        // Spawn rows
        foreach (var p in players)
        {
            var rowObj = Instantiate(playerRowPrefab, playerListContent);
            var rowUI = rowObj.GetComponent<PlayerRowUI>();
            if (rowUI != null)
            {
                // Capture the current player for the lambda closure
                var playerToKick = p;
                rowUI.Setup(
                    avatar: GetAvatarSprite(p.AvatarIndex), 
                    playerName: p.PlayerName, 
                    isHost: p.IsHost, 
                    isLocalPlayerHost: AttoTheSheep.Core.LobbySession.IsHost, 
                    isMe: p.IsLocalPlayer, 
                    onKickClicked: () => OnKickPlayer(playerToKick)
                );
            }
        }
    }

    private void OnKickPlayer(AttoTheSheep.Core.PlayerData p)
    {
        Debug.Log($"[WaitLobby] Kicking player: {p.PlayerName}");
        if (OnRequestKickPlayer != null)
        {
            OnRequestKickPlayer.Invoke(p.PlayerId);
        }
        else if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.KickPlayer(p.PlayerId);
        }
    }

    private void OnLeaveClicked()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
        }
        else
        {
            OnConfirmYesClicked();
        }
    }

    private void OnConfirmYesClicked()
    {
        Debug.Log("[WaitLobby] Leaving lobby...");
        if (OnRequestLeaveLobby != null)
        {
            OnRequestLeaveLobby.Invoke();
        }
        else if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.LeaveLobby();
            SceneManager.LoadScene("MatchMaking");
        }
    }

    private void OnConfirmNoClicked()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private void OnReadyClicked()
    {
        Debug.Log("[WaitLobby] Ready clicked! Proceeding to Game...");
        AttoTheSheep.Core.LoadingManager.Instance?.Show("msg_loading_game");
        
        if (OnRequestReady != null)
        {
            OnRequestReady.Invoke();
        }
        else if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.HostStartGame();
        }
    }
}
