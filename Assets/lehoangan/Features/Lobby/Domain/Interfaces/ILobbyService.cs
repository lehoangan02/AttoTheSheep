using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public interface ILobbyService
{
    Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate, Player player);
    Task<QueryResponse> QueryLobbiesAsync(QueryLobbiesOptions options);
    Task<Lobby> JoinLobbyByIdAsync(string lobbyId, JoinLobbyByIdOptions options);
    Task<Lobby> JoinLobbyByCodeAsync(string lobbyCode, JoinLobbyByCodeOptions options);
    Task<Lobby> QuickJoinLobbyAsync(QuickJoinLobbyOptions options);
    Task LeaveLobbyAsync(string lobbyId, string playerId);
    Task DeleteLobbyAsync(string lobbyId);
    Task SendHeartbeatPingAsync(string lobbyId);
    Task<Lobby> UpdateLobbyAsync(string lobbyId, UpdateLobbyOptions options);
    Task<ILobbyEvents> SubscribeToLobbyEventsAsync(string lobbyId, LobbyEventCallbacks callbacks);
}
