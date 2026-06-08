public class PlayerProfile
{
    public int Coins { get; private set; }
    public int Exp { get; private set; }

    public void AddExp(int amount)
    {
        if (amount < 0) return;
        Exp += amount;
    }

    public void SpendCoins(int amount)
    {
        if (Coins >= amount) Coins -= amount;
    }
    public void RestoreState(int coins, int exp)
    {
        Coins = coins;
        Exp = exp;
    }
}