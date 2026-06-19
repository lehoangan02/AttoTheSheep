public class RelayClientData
{
    public string Ip { get; }
    public ushort Port { get; }
    public byte[] AllocationId { get; }
    public byte[] Key { get; }
    public byte[] ConnectionData { get; }
    public byte[] HostConnectionData { get; }

    public RelayClientData(string ip, ushort port, byte[] allocationId, byte[] key, byte[] connectionData, byte[] hostConnectionData)
    {
        Ip = ip;
        Port = port;
        AllocationId = allocationId;
        Key = key;
        ConnectionData = connectionData;
        HostConnectionData = hostConnectionData;
    }
}
