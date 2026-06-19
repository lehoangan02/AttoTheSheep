using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class UnityLobbyService : ILobbyService
{
    public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate, Player player)
    {
        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = isPrivate,
            Player = player
        };
        return await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
    }

    public async Task<QueryResponse> QueryLobbiesAsync(QueryLobbiesOptions options)
    {
        return await LobbyService.Instance.QueryLobbiesAsync(options);
    }

    public async Task<Lobby> JoinLobbyByIdAsync(string lobbyId, JoinLobbyByIdOptions options)
    {
        return await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
    }

    public async Task<Lobby> JoinLobbyByCodeAsync(string lobbyCode, JoinLobbyByCodeOptions options)
    {
        return await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
    }

    public async Task<Lobby> QuickJoinLobbyAsync(QuickJoinLobbyOptions options)
    {
        return await LobbyService.Instance.QuickJoinLobbyAsync(options);
    }

    public async Task LeaveLobbyAsync(string lobbyId, string playerId)
    {
        await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
    }

    public async Task DeleteLobbyAsync(string lobbyId)
    {
        await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
    }

    public async Task SendHeartbeatPingAsync(string lobbyId)
    {
        await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
    }

    public async Task<Lobby> UpdateLobbyAsync(string lobbyId, UpdateLobbyOptions options)
    {
        return await LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);
    }

    public async Task<ILobbyEvents> SubscribeToLobbyEventsAsync(string lobbyId, LobbyEventCallbacks callbacks)
    {
        return await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, callbacks);
    }
}
