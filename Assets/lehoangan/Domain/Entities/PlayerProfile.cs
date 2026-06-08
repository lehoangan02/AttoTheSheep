public class PlayerProfile
{
    public int Coins { get; private set; }
    public int Exp { get; private set; }
    public int UnlockedStage { get; private set; }
    public int DamageLevel { get; private set; }
    public int HpLevel { get; private set; }
    public int HerdHpLevel { get; private set; }
    public bool HasArmor { get; private set; }
    public bool HasHorn { get; private set; }

    public void AddExp(int amount)
    {
        if (amount < 0) return;
        Exp += amount;
    }

    public void SpendCoins(int amount)
    {
        if (Coins >= amount) Coins -= amount;
    }

    public void RestoreState(int coins, int exp, int unlockedStage, 
        int damageLevel, int hpLevel, int herdHpLevel, bool hasArmor, bool hasHorn)
    {
        Coins = coins;
        Exp = exp;
        UnlockedStage = unlockedStage;
        DamageLevel = damageLevel;
        HpLevel = hpLevel;
        HerdHpLevel = herdHpLevel;
        HasArmor = hasArmor;
        HasHorn = hasHorn;
    }
}