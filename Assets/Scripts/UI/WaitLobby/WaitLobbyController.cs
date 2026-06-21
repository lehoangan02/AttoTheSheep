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
        if (leaveButton != null) leaveButton.onClick.AddListener(OnLeaveClicked);
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);

        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmYesClicked);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(OnConfirmNoClicked);

        GenerateMockData();
        RefreshUI();
    }

    private void GenerateMockData()
    {
        _players.Clear();
        
        bool hostMode = AttoTheSheep.Core.LobbySession.IsHost;

        // Mock the current player
        _players.Add(new AttoTheSheep.Core.PlayerData 
        { 
            PlayerId = "p_1",
            PlayerName = "Atto (You)", 
            IsHost = hostMode, 
            IsLocalPlayer = true, 
            AvatarIndex = Random.Range(0, mockAvatars != null ? mockAvatars.Length : 1) 
        });

        // Mock other players
        _players.Add(new AttoTheSheep.Core.PlayerData { PlayerId = "p_2", PlayerName = "Spider-Man", IsHost = !hostMode, IsLocalPlayer = false, AvatarIndex = Random.Range(0, mockAvatars != null ? mockAvatars.Length : 1) });
        _players.Add(new AttoTheSheep.Core.PlayerData { PlayerId = "p_3", PlayerName = "Iron Man", IsHost = false, IsLocalPlayer = false, AvatarIndex = Random.Range(0, mockAvatars != null ? mockAvatars.Length : 1) });
        _players.Add(new AttoTheSheep.Core.PlayerData { PlayerId = "p_4", PlayerName = "Black Widow", IsHost = false, IsLocalPlayer = false, AvatarIndex = Random.Range(0, mockAvatars != null ? mockAvatars.Length : 1) });
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
        else
        {
            // Fallback Mock Logic
            _players.Remove(p);
            RefreshUI();
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
        else
        {
            // Fallback Mock Logic
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
        if (OnRequestReady != null)
        {
            OnRequestReady.Invoke();
        }
        else
        {
            // Fallback Mock Logic: For now, no actual game scene exists, just log it.
            // SceneManager.LoadScene("MainGame");
        }
    }
}
