using System.Threading.Tasks;

public class CreateRelayUseCase
{
    private readonly IRelayService _relayService;
    private readonly INetworkConnectionService _networkConnectionService;

    public CreateRelayUseCase(IRelayService relayService, INetworkConnectionService networkConnectionService)
    {
        _relayService = relayService;
        _networkConnectionService = networkConnectionService;
    }

    public async Task<RelayHostData> ExecuteAsync(int playerCount)
    {
        RelayHostData hostData = await _relayService.CreateAllocationAsync(playerCount);
        _networkConnectionService.StartHost(hostData);
        return hostData;
    }
}
