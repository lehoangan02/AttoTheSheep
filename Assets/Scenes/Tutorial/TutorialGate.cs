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

        if (isLoading) return;

        if (collision.CompareTag("Player") || collision.GetComponent<PlayerController>() != null)
        {

            isLoading = true;
            MoveToNextScene();
        }
    }

    private void MoveToNextScene()
    {

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(nextSceneName);
    }
}