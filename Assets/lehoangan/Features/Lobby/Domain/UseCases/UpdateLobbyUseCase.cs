using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class UpdateLobbyUseCase
{
    private readonly ILobbyService _lobbyService;

    public UpdateLobbyUseCase(ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    public async Task<Lobby> ExecuteAsync(string lobbyId, UpdateLobbyOptions options)
    {
        return await _lobbyService.UpdateLobbyAsync(lobbyId, options);
    }
}
