using Unity.Netcode;
using UnityEngine;

public class NetworkDebugger : MonoBehaviour
{
    [Header("Cài đặt Test Nhanh")]
    public bool autoStartHost = true;

    void Start()
    {
        Debug.Log("🟡 [DEBUGGER] 1. Script Debugger đã kích hoạt hàm Start().");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("🔴 [DEBUGGER] LỖI TRÍ MẠNG: Không tìm thấy NetworkManager trong Scene! Hãy tạo NetworkManager ngay.");
            return;
        }

        Debug.Log("🟢 [DEBUGGER] 2. Đã tìm thấy NetworkManager. Tình trạng AutoHost đang là: " + autoStartHost);

        if (autoStartHost)
        {
            Debug.Log("🟡 [DEBUGGER] 3. Đang ra lệnh StartHost()...");
            
            // StartHost() thực chất trả về một giá trị true/false. Ta sẽ bắt lấy nó!
            bool isSuccess = NetworkManager.Singleton.StartHost();
            
            if (isSuccess)
            {
                Debug.Log("🟢 [DEBUGGER] 4. THÀNH CÔNG! Host đã khởi chạy. Kiểm tra xem Atto đã Spawn chưa?");
            }
            else
            {
                Debug.LogError("🔴 [DEBUGGER] 4. THẤT BẠI! Lệnh StartHost bị từ chối. (Nguyên nhân thường do thiếu Unity Transport hoặc Player Prefab).");
            }
        }
    }

    void OnGUI()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 300));
            if (GUILayout.Button("🕹️ BẤM VÀO ĐÂY ĐỂ HOST", GUILayout.Height(60)))
            {
                Debug.Log("🟡 [DEBUGGER] Đang gọi StartHost() từ nút bấm UI...");
                bool isSuccess = NetworkManager.Singleton.StartHost();
                if (!isSuccess) Debug.LogError("🔴 [DEBUGGER] Nút bấm báo: StartHost thất bại!");
            }
            GUILayout.EndArea();
        }
    }
}