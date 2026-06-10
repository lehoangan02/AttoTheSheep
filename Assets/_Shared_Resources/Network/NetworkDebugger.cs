using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;

#if UNITY_EDITOR
using ParrelSync;
#endif

public class NetworkDebugger : MonoBehaviour
{
    [Header("Auto Host Settings")]
    [SerializeField] private ushort hostPort = 7777;
    [SerializeField] private bool autoFallbackPort = true;
    [SerializeField, Min(1)] private int fallbackPortAttempts = 10;

    void Start()
    {
#if UNITY_EDITOR
        // If running in ParrelSync Clone -> Automatically join as Client
        if (ClonesManager.IsClone())
        {
            Debug.Log("🔵 [DEBUGGER] This is a Clone window. Automatically connecting as Client...");
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = hostPort;
            NetworkManager.Singleton.StartClient();
            return;
        }
#endif

        Debug.Log("🟡 [DEBUGGER] 1. Starting game. Auto Hosting...");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("🔴 [DEBUGGER] FATAL ERROR: NetworkManager not found in the Scene! Please create a NetworkManager.");
            return;
        }

        StartAutoHost();
    }

    private void StartAutoHost()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("🟡 [DEBUGGER] NetworkManager is already running, skipping new Host command.");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("🔴 [DEBUGGER] UnityTransport not found on NetworkManager.");
            return;
        }

        // Automatically find a free port to avoid "Address already in use" error
        int attempts = autoFallbackPort ? Mathf.Max(1, fallbackPortAttempts) : 1;
        for (int i = 0; i < attempts; i++)
        {
            int portCandidate = hostPort + i;
            if (portCandidate > ushort.MaxValue) break;

            ushort portToTry = (ushort)portCandidate;
            if (!IsUdpPortAvailable(portToTry))
            {
                Debug.LogWarning($"🟡 [DEBUGGER] UDP Port {portToTry} is busy, trying next port...");
                continue;
            }

            // Assign free port to transport and start
            transport.ConnectionData.Port = portToTry;
            Debug.Log($"🟡 [DEBUGGER] 2. Issuing StartHost() command on port {portToTry}...");

            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log($"🟢 [DEBUGGER] 3. SUCCESS! Host has started. Check if Atto has Spawned.");
                return;
            }

            // If failed despite port being free, shut down and retry
            NetworkManager.Singleton.Shutdown();
        }

        int lastPort = Mathf.Min(ushort.MaxValue, hostPort + attempts - 1);
        Debug.LogError($"🔴 [DEBUGGER] 4. FAILED! Could not bind any port from {hostPort} to {lastPort}. Close other applications occupying the ports.");
    }

    // Function to check if Port is currently occupied by another app (or older Unity instance)
    private static bool IsUdpPortAvailable(ushort port)
    {
        try
        {
            using (new UdpClient(new IPEndPoint(IPAddress.Any, port)))
            {
                return true;
            }
        }
        catch (SocketException)
        {
            return false;
        }
    }
}