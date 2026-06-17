using System.Threading.Tasks;

public class LeaveLobbyUseCase
{
    private readonly ILobbyService _lobbyService;

    public LeaveLobbyUseCase(ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    public async Task ExecuteAsync(string lobbyId, string playerId)
    {
        await _lobbyService.LeaveLobbyAsync(lobbyId, playerId);
    }
}
