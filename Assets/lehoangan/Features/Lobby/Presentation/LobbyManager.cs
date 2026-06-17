using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using QFSW.QC;
using System.Collections.Generic;
using System;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private float heartbeatTimer;
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
            new HeartbeatLobbyUseCase(lobbyService)
        );
    }

    private async void Start()
    {
        // Initialization is handled by GameBootstrapper, but we ensure Services are ready
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            InitializationOptions options = new InitializationOptions();
#if UNITY_EDITOR
            if (ParrelSync.ClonesManager.IsClone())
            {
                string customArgument = ParrelSync.ClonesManager.GetArgument();
                options.SetProfile($"Clone{customArgument}");
            }
#endif
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
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
        Debug.Log($"Created lobby: {Presenter.JoinedLobby.Name} ID: {Presenter.JoinedLobby.Id}");
    }

    [Command]
    public async void ListLobbies()
    {
        await Presenter.RefreshLobbyList();
        Debug.Log($"Number of lobbies found: {Presenter.AvailableLobbies.Count}");
        foreach (var lobby in Presenter.AvailableLobbies)
        {
            Debug.Log($"Lobby name: {lobby.Name} | Lobby ID: {lobby.Id}");
        }
    }

    [Command]
    public async void JoinPrivateLobby(string lobbyCode, string playerName)
    {
        await Presenter.JoinLobbyByCode(lobbyCode, playerName);
        Debug.Log($"Successfully joined lobby with code: {lobbyCode}");
    }

    [Command]
    public async void JoinLobby(string lobbyId, string playerName)
    {
        await Presenter.JoinLobby(lobbyId, playerName);
        Debug.Log($"Successfully joined lobby with id: {lobbyId}");
    }

    [Command]
    public async void LeaveLobby()
    {
        await Presenter.LeaveLobby();
        Debug.Log("Successfully left lobby");
    }
}
