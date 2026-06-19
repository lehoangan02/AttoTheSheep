using System.Threading.Tasks;

public class JoinRelayUseCase
{
    private readonly IRelayService _relayService;
    private readonly INetworkConnectionService _networkConnectionService;

    public JoinRelayUseCase(IRelayService relayService, INetworkConnectionService networkConnectionService)
    {
        _relayService = relayService;
        _networkConnectionService = networkConnectionService;
    }

    public async Task<RelayClientData> ExecuteAsync(string joinCode)
    {
        RelayClientData clientData = await _relayService.JoinAllocationAsync(joinCode);
        _networkConnectionService.StartClient(clientData);
        return clientData;
    }
}
