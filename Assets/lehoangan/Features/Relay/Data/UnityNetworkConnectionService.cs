using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class UnityNetworkConnectionService : INetworkConnectionService
{
    public void StartHost(RelayHostData hostData)
    {
        if (NetworkManager.Singleton == null)
        {
            UnityEngine.Debug.LogError("[UnityNetworkConnectionService] NetworkManager.Singleton is null! Make sure a NetworkManager GameObject exists in your active scene.");
            return;
        }

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
        if (NetworkManager.Singleton == null)
        {
            UnityEngine.Debug.LogError("[UnityNetworkConnectionService] NetworkManager.Singleton is null! Make sure a NetworkManager GameObject exists in your active scene.");
            return;
        }

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
