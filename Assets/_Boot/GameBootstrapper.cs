using System;
using System.Collections;
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

        // Ensure ScreenshotManager is always globally available in every scene
        if (ScreenshotManager.Instance == null)
        {
            GameObject screenshotObj = new GameObject("ScreenshotManager");
            screenshotObj.AddComponent<ScreenshotManager>();
            // ScreenshotManager's own Awake will call DontDestroyOnLoad
        }

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

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            }

            PlayerRepository = new UnityCloudSaveRepository();

            CurrentProfile = await PlayerRepository.LoadAsync();

        }
        catch (Exception ex)
        {

            CurrentProfile = new PlayerProfile();
            CurrentProfile.RestoreState(
                0, 0, 1,
                1, 1, 1,
                false, false,
                15, 15, 15, 15
            );
        }

        // Validate items regardless of online or offline mode
        try
        {
            bool itemCountsModified = false;
            int flockShieldCount = CurrentProfile.FlockShieldCount;
            int spawnMaxLambsCount = CurrentProfile.SpawnMaxLambsCount;
            int skillDamageBoostCount = CurrentProfile.SkillDamageBoostCount;
            int speedBoostCount = CurrentProfile.SpeedBoostCount;

            if (flockShieldCount < 1) { flockShieldCount = 1; itemCountsModified = true; }
            if (spawnMaxLambsCount < 1) { spawnMaxLambsCount = 1; itemCountsModified = true; }
            if (skillDamageBoostCount < 1) { skillDamageBoostCount = 1; itemCountsModified = true; }
            if (speedBoostCount < 1) { speedBoostCount = 1; itemCountsModified = true; }

            if (itemCountsModified)
            {
                CurrentProfile.RestoreState(
                    CurrentProfile.Coins, CurrentProfile.Exp, CurrentProfile.UnlockedStage,
                    CurrentProfile.DamageLevel, CurrentProfile.HpLevel, CurrentProfile.HerdHpLevel,
                    CurrentProfile.HasArmor, CurrentProfile.HasHorn,
                    flockShieldCount, spawnMaxLambsCount, skillDamageBoostCount, speedBoostCount
                );

                if (PlayerRepository != null)
                {
                    await PlayerRepository.SaveAsync(CurrentProfile);

                }
            }
        }
        catch (Exception ex)
        {

        }

        OnBootstrapped?.Invoke();

        // Start the 30-minute auto-sync timer
        StartCoroutine(AutoSyncRoutine());
    }

    private IEnumerator AutoSyncRoutine()
    {
        while (true)
        {

            yield return new WaitForSecondsRealtime(1800f);

            if (CurrentProfile != null && PlayerRepository != null)
            {

                _ = PlayerRepository.SaveAsync(CurrentProfile);
            }
        }
    }
}