using System.Threading.Tasks;

public interface IRelayService
{
    Task<RelayHostData> CreateAllocationAsync(int playerCount);
    Task<RelayClientData> JoinAllocationAsync(string joinCode);
}
