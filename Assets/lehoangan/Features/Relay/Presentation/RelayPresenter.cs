using System;
using System.Threading.Tasks;

public class RelayPresenter
{
    private readonly CreateRelayUseCase _createRelayUseCase;
    private readonly JoinRelayUseCase _joinRelayUseCase;

    public RelayHostData HostData { get; private set; }
    public RelayClientData ClientData { get; private set; }

    public event Action<RelayHostData> OnRelayHostCreated;
    public event Action<RelayClientData> OnRelayClientJoined;
    public event Action<string> OnErrorOccurred;

    public RelayPresenter(CreateRelayUseCase createRelayUseCase, JoinRelayUseCase joinRelayUseCase)
    {
        _createRelayUseCase = createRelayUseCase;
        _joinRelayUseCase = joinRelayUseCase;
    }

    public async Task CreateRelay(int playerCount)
    {
        try
        {
            HostData = await _createRelayUseCase.ExecuteAsync(playerCount);
            OnRelayHostCreated?.Invoke(HostData);
        }
        catch (Exception e)
        {
            OnErrorOccurred?.Invoke(e.Message);
            throw;
        }
    }

    public async Task JoinRelay(string joinCode)
    {
        try
        {
            ClientData = await _joinRelayUseCase.ExecuteAsync(joinCode);
            OnRelayClientJoined?.Invoke(ClientData);
        }
        catch (Exception e)
        {
            OnErrorOccurred?.Invoke(e.Message);
            throw;
        }
    }
}
