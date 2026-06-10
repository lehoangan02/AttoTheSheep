using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    public static GameBootstrapper Instance { get; private set; }

    public IPlayerRepository PlayerRepository { get; private set; }
    public PlayerProfile CurrentProfile { get; private set; }

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

        await InitializeGameAsync();
    }

    private async Task InitializeGameAsync()
    {
        try
        {
            // 1. Initialize Unity Services (Required for Cloud Save)
            await UnityServices.InitializeAsync();

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
        }
    }
}