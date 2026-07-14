using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class UnityRelayService : IRelayService
{
    public async Task<RelayHostData> CreateAllocationAsync(int playerCount)
    {
        // Unity Service expects max connections as max players minus host (1)
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(playerCount - 1);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        return new RelayHostData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            joinCode
        );
    }

    public async Task<RelayClientData> JoinAllocationAsync(string joinCode)
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        return new RelayClientData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData
        );
    }
}
