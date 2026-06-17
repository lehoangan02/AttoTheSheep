using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;

public class CreateLobbyUseCase
{
    private readonly ILobbyService _lobbyService;

    public CreateLobbyUseCase(ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    public async Task<Lobby> ExecuteAsync(string lobbyName, int maxPlayers, bool isPrivate, Player player)
    {
        return await _lobbyService.CreateLobbyAsync(lobbyName, maxPlayers, isPrivate, player);
    }
}
