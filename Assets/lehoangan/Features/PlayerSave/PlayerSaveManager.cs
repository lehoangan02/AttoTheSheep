using UnityEngine;
using System.Threading.Tasks;
using AttoTheSheep.UI.ShopAndInventory;

/// <summary>

/// </summary>
public class PlayerSaveManager : MonoBehaviour
{
    public static PlayerSaveManager Instance { get; private set; }

    [Header("Audio Settings")]
    public AudioClip coinSFX;

    public bool IsReady => GameBootstrapper.Instance != null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (coinSFX == null)
            coinSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Shared_Resources/Audio/AudidResources/coin.mp3");
    }
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private bool IsMultiplayerScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return sceneName == "MultiplayerLevel" || sceneName == "SampleScene";
    }

    public int CoinsEarnedThisSession { get; private set; }

    /// <summary>
    /// Reset session coins. Can be called at the start of a level.
    /// </summary>
    public void ResetSessionCoins() => CoinsEarnedThisSession = 0;

    /// <summary>

    /// </summary>
    public Task AddMoney(int amount)
    {
        if (IsMultiplayerScene()) return Task.CompletedTask;

        CoinsEarnedThisSession += amount;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddGold(amount);
            PlayCoinSFX();

            return Task.CompletedTask;
        }

        if (GameBootstrapper.Instance?.CurrentProfile != null)
        {
            var profile = GameBootstrapper.Instance.CurrentProfile;
            profile.AddCoins(amount);
            // Save cloud ngay (fire-and-forget)
            _ = GameBootstrapper.Instance.PlayerRepository.SaveAsync(profile);
            PlayCoinSFX();

            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    private void PlayCoinSFX()
    {
        if (coinSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX_2D(coinSFX);
            AudioManager.Instance.PlaySFX_2D(coinSFX);
            AudioManager.Instance.PlaySFX_2D(coinSFX);
        }
    }

    /// <summary>

    /// </summary>
    public PlayerProfile GetProfile()
    {
        return GameBootstrapper.Instance?.CurrentProfile;
    }
}
