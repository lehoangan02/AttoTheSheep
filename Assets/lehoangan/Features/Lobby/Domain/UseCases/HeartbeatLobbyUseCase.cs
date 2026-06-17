using System.Threading.Tasks;

public class HeartbeatLobbyUseCase
{
    private readonly ILobbyService _lobbyService;

    public HeartbeatLobbyUseCase(ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    public async Task ExecuteAsync(string lobbyId)
    {
        await _lobbyService.SendHeartbeatPingAsync(lobbyId);
    }
}
