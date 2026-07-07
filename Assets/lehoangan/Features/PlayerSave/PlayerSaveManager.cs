using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class PlayerSaveManager : MonoBehaviour
{
    public static PlayerSaveManager Instance { get; private set; }

    private IPlayerRepository _playerRepository;
    private LoadPlayerUseCase _loadPlayerUseCase;
    private PlayerProfile _currentPlayerProfile;

    [Header("Audio Settings")]
    public AudioClip coinSFX;

    public bool IsReady { get; private set; }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (coinSFX == null) 
        {
            coinSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Shared_Resources/Audio/AudidResources/coin.mp3");
        }
    }
#endif

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keeps this object alive across scenes
    }

    private async void Start()
    {
        
        _playerRepository = new UnityCloudSaveRepository();
        _loadPlayerUseCase = new LoadPlayerUseCase(_playerRepository);

        _currentPlayerProfile = await _loadPlayerUseCase.ExecuteAsync();
        IsReady = true;
        
        Debug.Log("PlayerSaveManager is ready. Profile loaded.");
    }

    /// <summary>
    /// Adds money (coins) to the player's profile and instantly saves it to the cloud.
    /// Can be called from any other script using: PlayerSaveManager.Instance.AddMoney(amount);
    /// </summary>
    public async Task AddMoney(int amount)
    {
        if (!IsReady || _currentPlayerProfile == null)
        {
            Debug.LogWarning("PlayerSaveManager is not ready yet! Cannot add money.");
            return;
        }

        // Add coins to local profile
        _currentPlayerProfile.AddCoins(amount);
        
        // Play coin sound effect
        if (coinSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX_2D(coinSFX);
        }
        
        // Save to cloud
        await _playerRepository.SaveAsync(_currentPlayerProfile);
        
        Debug.Log($"Successfully added {amount} coins to Cloud Save. New total: {_currentPlayerProfile.Coins}");
    }
    
    /// <summary>
    /// Retrieves the current loaded player profile.
    /// </summary>
    public PlayerProfile GetProfile()
    {
        return _currentPlayerProfile;
    }
}
