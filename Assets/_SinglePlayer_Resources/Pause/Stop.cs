using UnityEngine;

public class Stop : MonoBehaviour
{
    public static Stop Instance { get; private set; }

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Optional: Uncomment this if you want the pause manager to persist across scene loads
        // DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Pauses the game by setting Time.timeScale to 0.
    /// </summary>
    public void PauseGame()
    {
        Time.timeScale = 0f;
        IsPaused = true;
        // Note: You can also pause audio here if needed using AudioListener.pause = true;
    }

    /// <summary>
    /// Resumes the game by setting Time.timeScale to 1.
    /// </summary>
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        // Note: You can also resume audio here if needed using AudioListener.pause = false;
    }

    /// <summary>
    /// Toggles the current pause state.
    /// </summary>
    public void TogglePause()
    {
        if (IsPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
}
