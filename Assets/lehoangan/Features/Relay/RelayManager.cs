using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using QFSW.QC;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class RelayManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        if (GameBootstrapper.Instance == null)
        {
            Debug.LogWarning("[LobbyManager] GameBootstrapper Instance not found. Lobby might not work correctly if services aren't initialized.");
        }
    }
    [Command]
    private async void CreateRelay(int playerCount)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(playerCount - 1);

            string joincode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log(joincode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
        } catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }
    [Command]
    private async void JoinRelay(string joincode)
    {
        try
        {
            Debug.Log("Joining relay with code " + joincode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joincode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
