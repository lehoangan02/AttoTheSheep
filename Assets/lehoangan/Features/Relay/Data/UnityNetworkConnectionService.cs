using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class UnityNetworkConnectionService : INetworkConnectionService
{
    public void StartHost(RelayHostData hostData)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
            hostData.Ip,
            hostData.Port,
            hostData.AllocationId,
            hostData.Key,
            hostData.ConnectionData
        );
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient(RelayClientData clientData)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
            clientData.Ip,
            clientData.Port,
            clientData.AllocationId,
            clientData.Key,
            clientData.ConnectionData,
            clientData.HostConnectionData
        );
        NetworkManager.Singleton.StartClient();
    }
}
