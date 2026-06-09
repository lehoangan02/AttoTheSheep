using System;
using System.Collections.Generic;

public enum StageId
{
    Farm,
    DeepForest,
    DemonLair
}

public enum EnemyKind
{
    GreenSoldier,
    BlackSoldier,
    PoisonSnake,
    PoisonSpider,
    Skeleton,
    Goblin,
    Wizard,
    GreatDemon
}

public enum EnemySpeedTier
{
    Slow,
    Basic,
    Fast
}

public static class EnemySpeedTierExtensions
{
    public static float ToMoveSpeed(this EnemySpeedTier tier)
    {
        return tier switch
        {
            EnemySpeedTier.Slow => 3.5f,
            EnemySpeedTier.Basic => 5f,
            EnemySpeedTier.Fast => 7f,
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null)
        };
    }
}

[Serializable]
public class EnemySpawnRule
{
    public EnemyKind enemyKind;
    public StageId stage;
    public int startWave = 1;
    public int endWave = 0;
    public int countPerWave = 1;
    public bool spawnOnce;

    public bool Matches(StageId currentStage, int wave)
    {
        if (stage != currentStage) return false;
        if (wave < startWave) return false;
        return endWave <= 0 || wave <= endWave;
    }
}

public static class EnemySpawnRuleCatalog
{
    public static List<EnemySpawnRule> CreateDefaultRules()
    {
        return new List<EnemySpawnRule>
        {
            Create(EnemyKind.GreenSoldier, StageId.Farm, 1, 0, 10),
            Create(EnemyKind.BlackSoldier, StageId.Farm, 3, 0, 10),

            Create(EnemyKind.PoisonSnake, StageId.DeepForest, 1, 0, 10),
            Create(EnemyKind.PoisonSpider, StageId.DeepForest, 4, 0, 10),
            Create(EnemyKind.Skeleton, StageId.DeepForest, 6, 0, 8),
            Create(EnemyKind.Goblin, StageId.DeepForest, 7, 0, 8),

            Create(EnemyKind.PoisonSnake, StageId.DemonLair, 1, 0, 10),
            Create(EnemyKind.PoisonSpider, StageId.DemonLair, 1, 0, 10),
            Create(EnemyKind.Skeleton, StageId.DemonLair, 3, 0, 8),
            Create(EnemyKind.Goblin, StageId.DemonLair, 3, 0, 8),
            Create(EnemyKind.Wizard, StageId.DemonLair, 9, 0, 2),
            Create(EnemyKind.GreatDemon, StageId.DemonLair, 15, 15, 1, true)
        };
    }

    private static EnemySpawnRule Create(
        EnemyKind enemyKind,
        StageId stage,
        int startWave,
        int endWave,
        int countPerWave,
        bool spawnOnce = false)
    {
        return new EnemySpawnRule
        {
            enemyKind = enemyKind,
            stage = stage,
            startWave = startWave,
            endWave = endWave,
            countPerWave = countPerWave,
            spawnOnce = spawnOnce
        };
    }
}
