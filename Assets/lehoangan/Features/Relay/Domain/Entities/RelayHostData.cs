public class RelayHostData
{
    public string Ip { get; }
    public ushort Port { get; }
    public byte[] AllocationId { get; }
    public byte[] Key { get; }
    public byte[] ConnectionData { get; }
    public string JoinCode { get; }

    public RelayHostData(string ip, ushort port, byte[] allocationId, byte[] key, byte[] connectionData, string joinCode)
    {
        Ip = ip;
        Port = port;
        AllocationId = allocationId;
        Key = key;
        ConnectionData = connectionData;
        JoinCode = joinCode;
    }
}
