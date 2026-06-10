using UnityEngine;
using UnityEngine.SceneManagement;

namespace AttoTheSheep.Core
{
    /// <summary>
    /// Handles scene transitions gracefully.
    /// This follows the Single Responsibility Principle by decoupling scene loading logic from UI scripts.
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        private void Awake()
        {
            // Simple Singleton pattern to ensure we only have one manager, but we don't persist it across scenes for now.
            // If needed globally, we can add DontDestroyOnLoad(gameObject).
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Loads the specified scene by name. Can be expanded later to include fade in/out animations.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("Scene name to load is empty!");
                return;
            }

            Debug.Log($"[SceneTransitionManager] Loading scene: {sceneName}");
            // In the future, we could start a coroutine here to fade the screen to black before loading.
            SceneManager.LoadScene(sceneName);
        }
    }
}
