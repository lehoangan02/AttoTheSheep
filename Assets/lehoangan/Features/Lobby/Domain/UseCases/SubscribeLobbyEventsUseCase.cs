using System.Threading.Tasks;
using Unity.Services.Lobbies;

public class SubscribeLobbyEventsUseCase
{
    private readonly ILobbyService _lobbyService;

    public SubscribeLobbyEventsUseCase(ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    public async Task<ILobbyEvents> ExecuteAsync(string lobbyId, LobbyEventCallbacks callbacks)
    {
        return await _lobbyService.SubscribeToLobbyEventsAsync(lobbyId, callbacks);
    }
}
