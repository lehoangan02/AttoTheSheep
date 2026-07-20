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

        foreach (var lobby in Presenter.AvailableLobbies)
        {

        }
    }

    [Command]
    public async void JoinPrivateLobby(string lobbyCode, string playerName)
    {
        await Presenter.JoinLobbyByCode(lobbyCode, playerName);

        await Presenter.SubscribeToCurrentLobby();
    }

    [Command]
    public async void JoinLobby(string lobbyId, string playerName)
    {
        await Presenter.JoinLobby(lobbyId, playerName);

        await Presenter.SubscribeToCurrentLobby();
    }

    [Command]
    public async void LeaveLobby()
    {
        await Presenter.LeaveLobby();

    }

    [Command]
    public async void KickPlayer(string playerId)
    {
        await Presenter.KickPlayer(playerId);

    }

    [Command]
    public void PrintLobbyPlayers()
    {
        if (Presenter.JoinedLobby == null)
        {

            return;
        }

        foreach (var player in Presenter.JoinedLobby.Players)
        {
            string name = player.Data != null && player.Data.TryGetValue("PlayerName", out var dataObj)
                ? dataObj.Value
                : "Unknown Player";

        }
    }

    private async void OnRelayJoinCodeReceived(string joinCode)
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost) return;

        if (RelayManager.Instance == null)
        {

            return;
        }

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

            return;
        }

        try
        {
            // 1. Create Relay Allocation using standard Relay Use Case

            await RelayManager.Instance.Presenter.CreateRelay(Presenter.JoinedLobby.MaxPlayers);
            string joinCode = RelayManager.Instance.Presenter.HostData.JoinCode;

            // 2. Share Relay Join Code with Lobby

            await Presenter.ShareRelayJoinCode(joinCode);

            // Register Scene Load Complete
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
            }

            // 3. Initiate Unity Netcode Scene loading

            NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerLevel", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        catch (Exception e)
        {

        }
    }

    private async void OnSceneLoadCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != "MultiplayerLevel") return;

        // Unsubscribe from Netcode Scene Manager load completion
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        }

        // Clean up event subscription
        await Presenter.UnsubscribeLobbyEvents();

        if (NetworkManager.Singleton.IsServer)
        {

            // We just let the lobby expire naturally or keep it for late joiners

            // Ensure PlayerSpawnManager exists in MultiplayerLevel
            if (UnityEngine.Object.FindFirstObjectByType<PlayerSpawnManager>() == null)
            {

                var smGo = new GameObject("PlayerSpawnManager");
                smGo.AddComponent<PlayerSpawnManager>();
            }
        }
        else
        {

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
