using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
#if UNITY_EDITOR
using ParrelSync;
#endif

public class GameBootstrapper : MonoBehaviour
{
    public static GameBootstrapper Instance { get; private set; }

    public IPlayerRepository PlayerRepository { get; private set; }
    public PlayerProfile CurrentProfile { get; private set; }

    private TaskCompletionSource<bool> _initializeTaskSource = new TaskCompletionSource<bool>();
    public Task InitializationTask => _initializeTaskSource.Task;

    public event Action OnBootstrapped;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            await InitializeGameAsync();
            _initializeTaskSource.SetResult(true);
        }
        catch (Exception ex)
        {
            _initializeTaskSource.SetException(ex);
        }
    }

    private async Task InitializeGameAsync()
    {
        try
        {
            // 1. Initialize Unity Services with Profile (Required for ParrelSync Clones)
            InitializationOptions options = new InitializationOptions();
#if UNITY_EDITOR
            if (ClonesManager.IsClone())
            {
                string customArgument = ClonesManager.GetArgument();
                options.SetProfile($"Clone{customArgument}");
            }
#endif
            await UnityServices.InitializeAsync(options);

            // 2. Authenticate Player (Cloud Save requires a signed-in player)
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[Bootstrapper] Signed in anonymously. Player ID: {AuthenticationService.Instance.PlayerId}");
            }

            // 3. Setup Dependencies
            PlayerRepository = new UnityCloudSaveRepository();

            // 4. Load the player's profile data
            CurrentProfile = await PlayerRepository.LoadAsync();
            Debug.Log("[Bootstrapper] Player Profile successfully loaded from Cloud Save.");
            
            OnBootstrapped?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Bootstrapper] Failed to initialize game services: {ex.Message}");
            throw; // Re-throw to be caught in Awake and set exception on Task
        }
    }
}