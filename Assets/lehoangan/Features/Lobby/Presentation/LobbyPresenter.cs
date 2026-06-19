using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyPresenter
{
    private readonly CreateLobbyUseCase _createLobbyUseCase;
    private readonly JoinLobbyUseCase _joinLobbyUseCase;
    private readonly LeaveLobbyUseCase _leaveLobbyUseCase;
    private readonly GetLobbiesUseCase _getLobbiesUseCase;
    private readonly HeartbeatLobbyUseCase _heartbeatLobbyUseCase;
    private readonly UpdateLobbyUseCase _updateLobbyUseCase;
    private readonly SubscribeLobbyEventsUseCase _subscribeLobbyEventsUseCase;

    private ILobbyEvents _lobbyEvents;

    public Lobby JoinedLobby { get; private set; }
    public List<Lobby> AvailableLobbies { get; private set; } = new List<Lobby>();
    public bool IsHost => JoinedLobby != null && JoinedLobby.HostId == AuthenticationService.Instance.PlayerId;

    public event Action OnLobbyListUpdated;
    public event Action<Lobby> OnJoinedLobbyUpdated;
    public event Action<string> OnErrorOccurred;
    public event Action<string> OnRelayJoinCodeReceived;

    public LobbyPresenter(
        CreateLobbyUseCase createLobbyUseCase,
        JoinLobbyUseCase joinLobbyUseCase,
        LeaveLobbyUseCase leaveLobbyUseCase,
        GetLobbiesUseCase getLobbiesUseCase,
        HeartbeatLobbyUseCase heartbeatLobbyUseCase,
        UpdateLobbyUseCase updateLobbyUseCase,
        SubscribeLobbyEventsUseCase subscribeLobbyEventsUseCase)
    {
        _createLobbyUseCase = createLobbyUseCase;
        _joinLobbyUseCase = joinLobbyUseCase;
        _leaveLobbyUseCase = leaveLobbyUseCase;
        _getLobbiesUseCase = getLobbiesUseCase;
        _heartbeatLobbyUseCase = heartbeatLobbyUseCase;
        _updateLobbyUseCase = updateLobbyUseCase;
        _subscribeLobbyEventsUseCase = subscribeLobbyEventsUseCase;
    }

    public async Task CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, string playerName)
    {
        try
        {
            Player player = GetPlayer(playerName);
            JoinedLobby = await _createLobbyUseCase.ExecuteAsync(lobbyName, maxPlayers, isPrivate, player);
            OnJoinedLobbyUpdated?.Invoke(JoinedLobby);
        }
        catch (Exception e)
        {
            OnErrorOccurred?.Invoke(e.Message);
            throw;
        }
    }

    public async Task RefreshLobbyList()
    {
        try
        {
            var response = await _getLobbiesUseCase.ExecuteAsync();
            AvailableLobbies = response.Results;
            OnLobbyListUpdated?.Invoke();
        }
        catch (Exception e)
        {
            OnErrorOccurred?.Invoke(e.Message);
            throw;
        }
    }

    public async Task JoinLobby(string lobbyId, string playerName)
    {
        try
        {
            Player player = GetPlayer(playerName);
            JoinedLobby = await _joinLobbyUseCase.JoinByIdAsync(lobbyId, player);
            OnJoinedLobbyUpdated?.Invoke(JoinedLobby);
        }
        catch (Exception e)
        {
            OnErrorOccurred?.Invoke(e.Message);
            throw;
        }
    }

    public async Task JoinLobbyByCode(string lobbyCode, string playerName)
    {
        try
        {
            Player player = GetPlayer(playerName);
            JoinedLobby = await _joinLobbyUseCase.JoinByCodeAsync(lobbyCode, player);
            OnJoinedLobbyUpdated?.Invoke(JoinedLobby);
        }
        catch (Exception e)
        {
            OnErrorOccurred?.Invoke(e.Message);
            throw;
        }
    }

    public async Task LeaveLobby()
    {
        try
        {
            if (JoinedLobby != null)
            {
                await _leaveLobbyUseCase.ExecuteAsync(JoinedLobby.Id, AuthenticationService.Instance.PlayerId);
                JoinedLobby = null;
                OnJoinedLobbyUpdated?.Invoke(null);
            }
        }
        catch (Exception e)
        {
            OnErrorOccurred?.Invoke(e.Message);
            throw;
        }
    }

    public async Task HandleHeartbeat()
    {
        if (IsHost && JoinedLobby != null)
        {
            await _heartbeatLobbyUseCase.ExecuteAsync(JoinedLobby.Id);
        }
    }

    public async Task SubscribeToCurrentLobby()
    {
        if (JoinedLobby == null) return;

        try
        {
            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            callbacks.KickedFromLobby += OnKickedFromLobby;

            _lobbyEvents = await _subscribeLobbyEventsUseCase.ExecuteAsync(JoinedLobby.Id, callbacks);
        }
        catch (Exception e)
        {
            OnErrorOccurred?.Invoke($"Lobby event subscription failed: {e.Message}");
        }
    }

    public async Task ShareRelayJoinCode(string joinCode)
    {
        if (JoinedLobby == null) return;

        try
        {
            var options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, LobbyDataObject>
                {
                    { "RelayJoinCode", new LobbyDataObject(LobbyDataObject.VisibilityOptions.Member, joinCode) }
                }
            };

            JoinedLobby = await _updateLobbyUseCase.ExecuteAsync(JoinedLobby.Id, options);
            OnJoinedLobbyUpdated?.Invoke(JoinedLobby);
        }
        catch (Exception e)
        {
            OnErrorOccurred?.Invoke($"Failed to share join code: {e.Message}");
            throw;
        }
    }

    private void OnLobbyChanged(LobbyChanges changes)
    {
        if (JoinedLobby == null) return;

        changes.ApplyToLobby(JoinedLobby);
        OnJoinedLobbyUpdated?.Invoke(JoinedLobby);

        if (changes.Data.Changed && JoinedLobby.Data.TryGetValue("RelayJoinCode", out var dataObject))
        {
            string code = dataObject.Value;
            if (!string.IsNullOrEmpty(code))
            {
                OnRelayJoinCodeReceived?.Invoke(code);
            }
        }
    }

    private void OnKickedFromLobby()
    {
        JoinedLobby = null;
        OnJoinedLobbyUpdated?.Invoke(null);
    }

    public async Task UnsubscribeLobbyEvents()
    {
        if (_lobbyEvents != null)
        {
            await _lobbyEvents.UnsubscribeAsync();
            _lobbyEvents = null;
        }
    }

    private Player GetPlayer(string playerName)
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
    }
}
