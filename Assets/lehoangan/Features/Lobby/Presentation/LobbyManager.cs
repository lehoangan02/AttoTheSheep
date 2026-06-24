using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using QFSW.QC;
using System.Collections.Generic;
using System;
using Unity.Netcode;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private float heartbeatTimer;
    private float listRefreshTimer = 5f;
    public LobbyPresenter Presenter { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Dependency Injection Setup
        ILobbyService lobbyService = new UnityLobbyService();
        Presenter = new LobbyPresenter(
            new CreateLobbyUseCase(lobbyService),
            new JoinLobbyUseCase(lobbyService),
            new LeaveLobbyUseCase(lobbyService),
            new GetLobbiesUseCase(lobbyService),
            new HeartbeatLobbyUseCase(lobbyService),
            new UpdateLobbyUseCase(lobbyService),
            new SubscribeLobbyEventsUseCase(lobbyService)
        );

        Presenter.OnRelayJoinCodeReceived += OnRelayJoinCodeReceived;
    }

    private async void Start()
    {
        // Wait for GameBootstrapper to finish initialization and authentication
        if (GameBootstrapper.Instance != null)
        {
            await GameBootstrapper.Instance.InitializationTask;
        }
        else
        {
            Debug.LogWarning("[LobbyManager] GameBootstrapper Instance not found. Lobby might not work correctly if services aren't initialized.");
        }
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyRefresh();
    }

    private async void HandleLobbyRefresh()
    {
        // Only refresh if we haven't joined a lobby yet AND we are authenticated
        if (Presenter.JoinedLobby == null && 
            UnityServices.State == ServicesInitializationState.Initialized && 
            AuthenticationService.Instance.IsSignedIn)
        {
            listRefreshTimer -= Time.deltaTime;
            if (listRefreshTimer <= 0)
            {
                listRefreshTimer = 5f;
                await Presenter.RefreshLobbyList();
            }
        }
    }

    private async void HandleLobbyHeartbeat()
    {
        if (Presenter.IsHost)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0)
            {
                heartbeatTimer = 15f;
                await Presenter.HandleHeartbeat();
            }
        }
    }

    [Command]
    public async void CreateLobby(string playerName, bool isPrivate)
    {
        await Presenter.CreateLobby("MyLobby", 5, isPrivate, playerName);
        Debug.Log($"Created lobby: {Presenter.JoinedLobby.Name} | ID: {Presenter.JoinedLobby.Id} | Code: {Presenter.JoinedLobby.LobbyCode}");
        await Presenter.SubscribeToCurrentLobby();
    }

    [Command]
    public async void ListLobbies()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[LobbyManager] Cannot list lobbies yet; still waiting for Unity Services Authentication.");
            return;
        }

        await Presenter.RefreshLobbyList();
        Debug.Log($"Number of lobbies found: {Presenter.AvailableLobbies.Count}");
        foreach (var lobby in Presenter.AvailableLobbies)
        {
            Debug.Log($"Lobby name: {lobby.Name} | Lobby ID: {lobby.Id} | Lobby Code: {lobby.LobbyCode}");
        }
    }

    [Command]
    public async void JoinPrivateLobby(string lobbyCode, string playerName)
    {
        await Presenter.JoinLobbyByCode(lobbyCode, playerName);
        Debug.Log($"Successfully joined lobby with code: {lobbyCode}");
        await Presenter.SubscribeToCurrentLobby();
    }

    [Command]
    public async void JoinLobby(string lobbyId, string playerName)
    {
        await Presenter.JoinLobby(lobbyId, playerName);
        Debug.Log($"Successfully joined lobby with id: {lobbyId}");
        await Presenter.SubscribeToCurrentLobby();
    }

    [Command]
    public async void LeaveLobby()
    {
        await Presenter.LeaveLobby();
        Debug.Log("Successfully left lobby");
    }

    [Command]
    public async void KickPlayer(string playerId)
    {
        await Presenter.KickPlayer(playerId);
        Debug.Log($"Successfully kicked player {playerId} from lobby");
    }

    [Command]
    public void PrintLobbyPlayers()
    {
        if (Presenter.JoinedLobby == null)
        {
            Debug.Log("You are not in any lobby.");
            return;
        }

        Debug.Log($"--- Players in {Presenter.JoinedLobby.Name} ({Presenter.JoinedLobby.Players.Count}/{Presenter.JoinedLobby.MaxPlayers}) ---");
        foreach (var player in Presenter.JoinedLobby.Players)
        {
            string name = player.Data != null && player.Data.TryGetValue("PlayerName", out var dataObj) 
                ? dataObj.Value 
                : "Unknown Player";
            
            Debug.Log($"- {name} (ID: {player.Id})");
        }
    }


    private async void OnRelayJoinCodeReceived(string joinCode)
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost) return;

        if (RelayManager.Instance == null)
        {
            Debug.LogError("[LobbyManager] RelayManager.Instance is null! Clients cannot connect without an active RelayManager in the scene.");
            return;
        }

        Debug.Log($"[Client] Received Relay Join Code: {joinCode}. Connecting to game session...");

        // Connect Client via Relay
        await RelayManager.Instance.Presenter.JoinRelay(joinCode);

        // Register Scene Load Complete
        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }
    }

    [Command]
    public async void HostStartGame()
    {
        if (!Presenter.IsHost) return;

        if (RelayManager.Instance == null)
        {
            Debug.LogError("[LobbyManager] RelayManager.Instance is null! Ensure RelayManager is attached to a GameObject in your active scene.");
            return;
        }

        try
        {
            // 1. Create Relay Allocation using standard Relay Use Case
            Debug.Log("[Host] Allocating Relay session...");
            await RelayManager.Instance.Presenter.CreateRelay(Presenter.JoinedLobby.MaxPlayers);
            string joinCode = RelayManager.Instance.Presenter.HostData.JoinCode;

            // 2. Share Relay Join Code with Lobby
            Debug.Log($"[Host] Relay created. Sharing Join Code {joinCode} with Lobby members...");
            await Presenter.ShareRelayJoinCode(joinCode);

            // Register Scene Load Complete
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
            }

            // 3. Initiate Unity Netcode Scene loading
            Debug.Log("[Host] Starting game scene load...");
            NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Host] Failed to transition to game: {e.Message}");
        }
    }

    private async void OnSceneLoadCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != "SampleScene") return;

        // Unsubscribe from Netcode Scene Manager load completion
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        }

        // Clean up event subscription
        await Presenter.UnsubscribeLobbyEvents();

        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[Host] Scene load completed for all clients. Terminating Lobby cloud session...");
            await Presenter.LeaveLobby(); 
        }
        else
        {
            Debug.Log("[Client] Scene load completed. Disconnecting from Lobby state...");
            await Presenter.LeaveLobby();
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        }
        if (Presenter != null)
        {
            Presenter.OnRelayJoinCodeReceived -= OnRelayJoinCodeReceived;
        }
    }
}
