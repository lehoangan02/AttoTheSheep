using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System;
public class UnityCloudSaveRepository : IPlayerRepository
{
    private const string KEY_COIN = "coin_amount";
    private const string KEY_EXP = "exp_amount";
    private const string KEY_UNLOCKED_STAGE = "unlocked_stage_index";
    private const string KEY_DAMAGE_LEVEL = "damage_level";
    private const string KEY_HP_LEVEL = "hp_level";
    private const string KEY_HERD_HP_LEVEL = "heard_hp_level"; 
    private const string KEY_HAS_AMOR = "has_amor"; 
    private const string KEY_HAS_HORN = "has_horn";
    private const string KEY_FLOCK_SHIELD_COUNT = "flock_shield_count";
    private const string KEY_SPAWN_MAX_LAMBS_COUNT = "spawn_max_lambs_count";
    private const string KEY_SKILL_DAMAGE_BOOST_COUNT = "skill_damage_boost_count";
    private const string KEY_SPEED_BOOST_COUNT = "speed_boost_count";
    private const string KEY_LAST_UPDATED = "last_updated_time";

    private static PlayerProfile _cachedProfile;
    private static Task _initializationTask;
    private string LocalSavePath => Application.persistentDataPath + "/local_save.json";

    [System.Serializable]
    private class LocalSaveData
    {
        public int Coins;
        public int Exp;
        public int UnlockedStage;
        public int DamageLevel;
        public int HpLevel;
        public int HerdHpLevel;
        public bool HasArmor;
        public bool HasHorn;
        public int FlockShieldCount;
        public int SpawnMaxLambsCount;
        public int SkillDamageBoostCount;
        public int SpeedBoostCount;
        public long LastUpdated;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initializationTask == null)
        {
            _initializationTask = InitializeServicesInternalAsync();
        }
        await _initializationTask;
    }

    private async Task InitializeServicesInternalAsync()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services in Repository: {e.Message}");
            // Reset task so it can be retried if it failed completely
            _initializationTask = null; 
        }
    }

    public async Task SaveAsync(PlayerProfile profile)
    {
        await EnsureInitializedAsync();

        // Update local cache immediately to guarantee correctness
        _cachedProfile = profile;

        var dataToSave = new Dictionary<string, object>
        {
            { KEY_COIN, profile.Coins },
            { KEY_EXP, profile.Exp },
            { KEY_UNLOCKED_STAGE, profile.UnlockedStage },
            { KEY_DAMAGE_LEVEL, profile.DamageLevel },
            { KEY_HP_LEVEL, profile.HpLevel },
            { KEY_HERD_HP_LEVEL, profile.HerdHpLevel },
            { KEY_HAS_AMOR, profile.HasArmor },
            { KEY_HAS_HORN, profile.HasHorn },
            { KEY_FLOCK_SHIELD_COUNT, profile.FlockShieldCount },
            { KEY_SPAWN_MAX_LAMBS_COUNT, profile.SpawnMaxLambsCount },
            { KEY_SKILL_DAMAGE_BOOST_COUNT, profile.SkillDamageBoostCount },
            { KEY_SPEED_BOOST_COUNT, profile.SpeedBoostCount },
            { KEY_LAST_UPDATED, DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        try
        {
            var localSave = new LocalSaveData
            {
                Coins = profile.Coins,
                Exp = profile.Exp,
                UnlockedStage = profile.UnlockedStage,
                DamageLevel = profile.DamageLevel,
                HpLevel = profile.HpLevel,
                HerdHpLevel = profile.HerdHpLevel,
                HasArmor = profile.HasArmor,
                HasHorn = profile.HasHorn,
                FlockShieldCount = profile.FlockShieldCount,
                SpawnMaxLambsCount = profile.SpawnMaxLambsCount,
                SkillDamageBoostCount = profile.SkillDamageBoostCount,
                SpeedBoostCount = profile.SpeedBoostCount,
                LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            File.WriteAllText(LocalSavePath, JsonUtility.ToJson(localSave));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LocalSave] Failed to save locally: {ex.Message}");
        }

        try
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(dataToSave);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CloudSave] Failed to save to Cloud Save: {e.Message}. Data is cached locally and will be saved later.");
        }
    }

    public async Task<PlayerProfile> LoadAsync()
    {
        await EnsureInitializedAsync();

        // Return cached profile if it exists to prevent unnecessary network requests
        if (_cachedProfile != null)
        {
            return _cachedProfile;
        }

        IDictionary<string, Unity.Services.CloudSave.Models.Item> loadedData = null;
        try
        {
            loadedData = await CloudSaveService.Instance.Data.Player.LoadAllAsync();
        }
        catch (System.Exception cloudEx)
        {
            Debug.LogError($"[CloudSave] Failed to load from Cloud Save: {cloudEx.Message}. Attempting to load local fallback.");
            try 
            {
                if (File.Exists(LocalSavePath))
                {
                    var json = File.ReadAllText(LocalSavePath);
                    var localSave = JsonUtility.FromJson<LocalSaveData>(json);
                    _cachedProfile = new PlayerProfile();
                    _cachedProfile.RestoreState(
                        localSave.Coins, localSave.Exp, localSave.UnlockedStage, 
                        localSave.DamageLevel, localSave.HpLevel, localSave.HerdHpLevel, 
                        localSave.HasArmor, localSave.HasHorn, localSave.FlockShieldCount, 
                        localSave.SpawnMaxLambsCount, localSave.SkillDamageBoostCount, localSave.SpeedBoostCount
                    );
                    Debug.Log("[LocalSave] Loaded profile from local storage.");
                    return _cachedProfile;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalSave] Failed to load local fallback: {ex.Message}");
            }

            Debug.Log("[LocalSave] No local fallback found. Proceeding with a default profile.");
            _cachedProfile = new PlayerProfile();
            return _cachedProfile;
        }
        
        if (loadedData == null || loadedData.Count == 0) 
        {
            _cachedProfile = new PlayerProfile();
            return _cachedProfile;
        }

        long cloudTimestamp = loadedData.TryGetValue(KEY_LAST_UPDATED, out var t) ? t.Value.GetAs<long>() : 0;
        
        LocalSaveData existingLocalSave = null;
        if (File.Exists(LocalSavePath))
        {
            try { existingLocalSave = JsonUtility.FromJson<LocalSaveData>(File.ReadAllText(LocalSavePath)); } catch { }
        }

        if (existingLocalSave != null && existingLocalSave.LastUpdated > cloudTimestamp)
        {
            Debug.Log("[CloudSave] Local cache is newer than Cloud Save. Preferring local data and syncing to Cloud.");
            var localProfile = new PlayerProfile();
            localProfile.RestoreState(
                existingLocalSave.Coins, existingLocalSave.Exp, existingLocalSave.UnlockedStage, 
                existingLocalSave.DamageLevel, existingLocalSave.HpLevel, existingLocalSave.HerdHpLevel, 
                existingLocalSave.HasArmor, existingLocalSave.HasHorn, existingLocalSave.FlockShieldCount, 
                existingLocalSave.SpawnMaxLambsCount, existingLocalSave.SkillDamageBoostCount, existingLocalSave.SpeedBoostCount
            );
            _cachedProfile = localProfile;
            
            // Upload to cloud asynchronously
            _ = SaveAsync(localProfile);
            return localProfile;
        }

        int coins = loadedData.TryGetValue(KEY_COIN, out var c) ? c.Value.GetAs<int>() : 0;
        int exp = loadedData.TryGetValue(KEY_EXP, out var e) ? e.Value.GetAs<int>() : 0;
        int unlockedStage = loadedData.TryGetValue(KEY_UNLOCKED_STAGE, out var us) ? us.Value.GetAs<int>() : 0;
        int damageLevel = loadedData.TryGetValue(KEY_DAMAGE_LEVEL, out var dl) ? dl.Value.GetAs<int>() : 0;
        int hpLevel = loadedData.TryGetValue(KEY_HP_LEVEL, out var hl) ? hl.Value.GetAs<int>() : 0;
        int herdHpLevel = loadedData.TryGetValue(KEY_HERD_HP_LEVEL, out var hhl) ? hhl.Value.GetAs<int>() : 0;
        bool hasArmor = loadedData.TryGetValue(KEY_HAS_AMOR, out var ha) ? ha.Value.GetAs<bool>() : false;
        bool hasHorn = loadedData.TryGetValue(KEY_HAS_HORN, out var hh) ? hh.Value.GetAs<bool>() : false;
        int flockShieldCount = loadedData.TryGetValue(KEY_FLOCK_SHIELD_COUNT, out var fsc) ? fsc.Value.GetAs<int>() : 0;
        int spawnMaxLambsCount = loadedData.TryGetValue(KEY_SPAWN_MAX_LAMBS_COUNT, out var smlc) ? smlc.Value.GetAs<int>() : 0;
        int skillDamageBoostCount = loadedData.TryGetValue(KEY_SKILL_DAMAGE_BOOST_COUNT, out var sdbc) ? sdbc.Value.GetAs<int>() : 0;
        int speedBoostCount = loadedData.TryGetValue(KEY_SPEED_BOOST_COUNT, out var sbc) ? sbc.Value.GetAs<int>() : 0;

        var profile = new PlayerProfile();
        profile.RestoreState(coins, exp, unlockedStage, damageLevel, hpLevel, herdHpLevel, hasArmor, hasHorn, 
            flockShieldCount, spawnMaxLambsCount, skillDamageBoostCount, speedBoostCount);
        
        // Save the freshly loaded cloud data to local disk to keep them in sync
        try
        {
            var localSave = new LocalSaveData
            {
                Coins = coins, Exp = exp, UnlockedStage = unlockedStage,
                DamageLevel = damageLevel, HpLevel = hpLevel, HerdHpLevel = herdHpLevel,
                HasArmor = hasArmor, HasHorn = hasHorn, FlockShieldCount = flockShieldCount,
                SpawnMaxLambsCount = spawnMaxLambsCount, SkillDamageBoostCount = skillDamageBoostCount,
                SpeedBoostCount = speedBoostCount,
                LastUpdated = cloudTimestamp
            };
            File.WriteAllText(LocalSavePath, JsonUtility.ToJson(localSave));
        }
        catch { }

        _cachedProfile = profile;
        return profile;
    }
}