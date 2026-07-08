using UnityEngine;
using System.Threading.Tasks;
using AttoTheSheep.UI.ShopAndInventory;

/// <summary>
/// Cầu nối cho gameplay gọi AddMoney khi kẻ địch chết.
/// Ưu tiên dùng InventoryManager nếu có (lobby scene).
/// Fallback về GameBootstrapper.CurrentProfile nếu đang ở level scene.
/// Trong mọi trường hợp chỉ ghi vào 1 profile duy nhất (GameBootstrapper.CurrentProfile).
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

    /// <summary>
    /// Thêm vàng cho player khi giết kẻ địch.
    /// Gọi bằng: PlayerSaveManager.Instance.AddMoney(amount)
    /// </summary>
    public Task AddMoney(int amount)
    {
        // --- Ưu tiên 1: InventoryManager có mặt (MapLobby) ---
        // AddGold() tự lo: cộng vào CurrentProfile + sync cloud + fire onInventoryUpdated
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddGold(amount);
            PlayCoinSFX();
            Debug.Log($"[PlayerSaveManager] +{amount} vàng (qua InventoryManager). Tổng: {InventoryManager.Instance.Gold}");
            return Task.CompletedTask;
        }

        // --- Fallback: Level scene (không có InventoryManager) ---
        // Ghi thẳng vào GameBootstrapper.CurrentProfile — vẫn là 1 nguồn sự thật
        if (GameBootstrapper.Instance?.CurrentProfile != null)
        {
            var profile = GameBootstrapper.Instance.CurrentProfile;
            profile.AddCoins(amount);
            // Save cloud ngay (fire-and-forget)
            _ = GameBootstrapper.Instance.PlayerRepository.SaveAsync(profile);
            PlayCoinSFX();
            Debug.Log($"[PlayerSaveManager] +{amount} vàng (qua GameBootstrapper). Tổng: {profile.Coins}");
            return Task.CompletedTask;
        }

        // --- Không có gì cả (hiếm gặp, ví dụ test scene) ---
        Debug.LogWarning("[PlayerSaveManager] Không tìm thấy nguồn lưu gold! Bỏ qua AddMoney.");
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
    /// Trả về profile hiện tại từ GameBootstrapper (nguồn sự thật duy nhất).
    /// </summary>
    public PlayerProfile GetProfile()
    {
        return GameBootstrapper.Instance?.CurrentProfile;
    }
}
