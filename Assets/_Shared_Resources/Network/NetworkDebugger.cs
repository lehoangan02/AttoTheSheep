using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;

public class NetworkDebugger : MonoBehaviour
{
    [Header("Cài đặt Host Tự Động")]
    [SerializeField] private ushort hostPort = 7777;
    [SerializeField] private bool autoFallbackPort = true;
    [SerializeField, Min(1)] private int fallbackPortAttempts = 10;

    void Start()
    {
        Debug.Log("🟡 [DEBUGGER] 1. Khởi động game. Bắt đầu tự động Host...");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("🔴 [DEBUGGER] LỖI TRÍ MẠNG: Không tìm thấy NetworkManager trong Scene! Hãy tạo NetworkManager ngay.");
            return;
        }

        StartAutoHost();
    }

    private void StartAutoHost()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("🟡 [DEBUGGER] NetworkManager đã chạy rồi, bỏ qua lệnh Host mới.");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("🔴 [DEBUGGER] Không tìm thấy UnityTransport trên NetworkManager.");
            return;
        }

        // Tự động tìm cổng (port) đang rảnh để tránh lỗi "Address already in use"
        int attempts = autoFallbackPort ? Mathf.Max(1, fallbackPortAttempts) : 1;
        for (int i = 0; i < attempts; i++)
        {
            int portCandidate = hostPort + i;
            if (portCandidate > ushort.MaxValue) break;

            ushort portToTry = (ushort)portCandidate;
            if (!IsUdpPortAvailable(portToTry))
            {
                Debug.LogWarning($"🟡 [DEBUGGER] Cổng UDP {portToTry} đang bận, thử cổng kế tiếp...");
                continue;
            }

            // Gán cổng rảnh cho transport và khởi động
            transport.ConnectionData.Port = portToTry;
            Debug.Log($"🟡 [DEBUGGER] 2. Đang ra lệnh StartHost() trên cổng {portToTry}...");

            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log($"🟢 [DEBUGGER] 3. THÀNH CÔNG! Host đã khởi chạy. Kiểm tra xem Atto đã Spawn chưa?");
                return;
            }

            // Nếu thất bại dù cổng báo rảnh, tắt đi thử lại
            NetworkManager.Singleton.Shutdown();
        }

        int lastPort = Mathf.Min(ushort.MaxValue, hostPort + attempts - 1);
        Debug.LogError($"🔴 [DEBUGGER] 4. THẤT BẠI! Không bind được cổng nào từ {hostPort} đến {lastPort}. Hãy đóng các phần mềm khác đang chiếm cổng.");
    }

    // Hàm kiểm tra xem Port có đang bị ứng dụng khác (hoặc Unity cũ) chiếm không
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