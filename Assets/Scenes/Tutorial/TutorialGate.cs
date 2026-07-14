using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class TutorialGate : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Tên chính xác của Scene tiếp theo (VD: Level1) trong Build Settings")]
    [SerializeField] private string nextSceneName = "Level1";

    private bool isLoading = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Tránh việc người chơi chạm vào cổng nhiều lần liên tiếp khi đang load
        if (isLoading) return;
        Debug.Log("[TutorialGate] Người chơi đã chạm vào cổng Tutorial");

        // Kiểm tra xem đối tượng va chạm có phải là Player hay không
        // Bạn có thể dùng Tag "Player" hoặc check Component PlayerController của bạn
        if (collision.CompareTag("Player") || collision.GetComponent<PlayerController>() != null)
        {
            Debug.Log($"[TutorialGate] Người chơi đã chạm vào cổng Tutorial! Đang load {nextSceneName}...");
            isLoading = true;
            MoveToNextScene();
        }
    }

    private void MoveToNextScene()
    {
        Debug.Log($"[TutorialGate] Người chơi đã hoàn thành Tutorial! Đang chuyển sang {nextSceneName}...");

        // QUAN TRỌNG CHO NETCODE: 
        // Nếu cảnh FTUE này có khởi chạy Network (Host/Client), ta cần Shutdown trước khi chuyển cảnh
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Tải cảnh tiếp theo thông qua SceneManager thông thường của Unity
        SceneManager.LoadScene(nextSceneName);
    }
}