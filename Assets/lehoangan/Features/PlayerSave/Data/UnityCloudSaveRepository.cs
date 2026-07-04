using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine.InputSystem;

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
    private const string KEY_POTION_COUNT = "potion_count";

    public async Task SaveAsync(PlayerProfile profile)
    {
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
            { KEY_POTION_COUNT, profile.PotionCount }
        };

        await CloudSaveService.Instance.Data.Player.SaveAsync(dataToSave);
    }

    public async Task<PlayerProfile> LoadAsync()
    {
        var loadedData = await CloudSaveService.Instance.Data.Player.LoadAllAsync();
        
        if (loadedData.Count == 0) return new PlayerProfile();

        int coins = loadedData.TryGetValue(KEY_COIN, out var c) ? c.Value.GetAs<int>() : 0;
        int exp = loadedData.TryGetValue(KEY_EXP, out var e) ? e.Value.GetAs<int>() : 0;
        int unlockedStage = loadedData.TryGetValue(KEY_UNLOCKED_STAGE, out var us) ? us.Value.GetAs<int>() : 0;
        int damageLevel = loadedData.TryGetValue(KEY_DAMAGE_LEVEL, out var dl) ? dl.Value.GetAs<int>() : 0;
        int hpLevel = loadedData.TryGetValue(KEY_HP_LEVEL, out var hl) ? hl.Value.GetAs<int>() : 0;
        int herdHpLevel = loadedData.TryGetValue(KEY_HERD_HP_LEVEL, out var hhl) ? hhl.Value.GetAs<int>() : 0;
        bool hasArmor = loadedData.TryGetValue(KEY_HAS_AMOR, out var ha) ? ha.Value.GetAs<bool>() : false;
        bool hasHorn = loadedData.TryGetValue(KEY_HAS_HORN, out var hh) ? hh.Value.GetAs<bool>() : false;
        int potionCount = loadedData.TryGetValue(KEY_POTION_COUNT, out var pc) ? pc.Value.GetAs<int>() : 0;

        var profile = new PlayerProfile();
        profile.RestoreState(coins, exp, unlockedStage, damageLevel, hpLevel, herdHpLevel, hasArmor, hasHorn, potionCount);
        
        return profile;
    }
}