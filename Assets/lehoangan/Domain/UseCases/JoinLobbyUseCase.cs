using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;

public class JoinLobbyUseCase
{
    private readonly ILobbyService _lobbyService;

    public JoinLobbyUseCase(ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    public async Task<Lobby> JoinByIdAsync(string lobbyId, Player player)
    {
        var options = new JoinLobbyByIdOptions { Player = player };
        return await _lobbyService.JoinLobbyByIdAsync(lobbyId, options);
    }

    public async Task<Lobby> JoinByCodeAsync(string lobbyCode, Player player)
    {
        var options = new JoinLobbyByCodeOptions { Player = player };
        return await _lobbyService.JoinLobbyByCodeAsync(lobbyCode, options);
    }

    public async Task<Lobby> QuickJoinAsync(QuickJoinLobbyOptions options = null)
    {
        return await _lobbyService.QuickJoinLobbyAsync(options);
    }
}
