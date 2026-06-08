using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using QFSW.QC;

public class UnityCloudSaveRepository : IPlayerRepository
{
    private const string KEY_COIN = "coin_amount";
    private const string KEY_EXP = "exp_amount";

    public async Task SaveAsync(PlayerProfile profile)
    {
        var dataToSave = new Dictionary<string, object>
        {
            { KEY_COIN, profile.Coins },
            { KEY_EXP, profile.Exp }
        };

        await CloudSaveService.Instance.Data.Player.SaveAsync(dataToSave);
    }
    public async Task<PlayerProfile> LoadAsync()
    {
        var keysToLoad = new HashSet<string> { KEY_COIN, KEY_EXP };
        var loadedData = await CloudSaveService.Instance.Data.Player.LoadAsync(keysToLoad);
        
        if (loadedData.Count == 0) return null;

        int coins = loadedData.TryGetValue(KEY_COIN, out var c) ? c.Value.GetAs<int>() : 0;
        int exp = loadedData.TryGetValue(KEY_EXP, out var e) ? e.Value.GetAs<int>() : 0;

        var profile = new PlayerProfile();
        profile.RestoreState(coins, exp);
        
        return profile;
    }
}