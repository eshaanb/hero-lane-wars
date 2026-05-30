namespace LaneWars.Core;

/// <summary>The tower's defensive specialization. None = un-upgraded.</summary>
public enum TowerBranch { None, Scattershot, Ballista, Bulwark }

/// <summary>Effect applied by advancing one tower upgrade level.</summary>
public readonly struct TowerUpgradeEffect
{
    public int Cost { get; init; }
    public int DamageDelta { get; init; }
    public int RangeDelta { get; init; }
    public int SplashDelta { get; init; }
    public int MaxHpDelta { get; init; }
}

/// <summary>
/// Pure-C# table of tower upgrade costs and effects. Players pick ONE branch
/// (exclusive specialization) and level it up; cost rises per level.
/// Values are starting points for balance and live here (not Godot) so the
/// simulation stays deterministic and headless-testable.
/// </summary>
public static class TowerUpgrades
{
    public const int MaxLevel = 3;

    // Cost to reach level N (index = level - 1).
    private static readonly int[] LevelCost = { 55, 100, 160 };

    /// <summary>
    /// Effect for advancing <paramref name="branch"/> to <paramref name="nextLevel"/> (1..MaxLevel).
    /// Returns null for None, out-of-range levels, or unknown branches.
    /// </summary>
    public static TowerUpgradeEffect? GetEffect(TowerBranch branch, int nextLevel)
    {
        if (branch == TowerBranch.None || nextLevel < 1 || nextLevel > MaxLevel)
            return null;

        int cost = LevelCost[nextLevel - 1];
        return branch switch
        {
            // Anti-swarm: turns the single-target tower into an AoE defender.
            TowerBranch.Scattershot => new TowerUpgradeEffect { Cost = cost, SplashDelta = 50 },
            // Anti-elite: bigger, longer-reaching shots.
            TowerBranch.Ballista => new TowerUpgradeEffect { Cost = cost, DamageDelta = 14, RangeDelta = 25 },
            // Durability: survive aggression and buy time.
            TowerBranch.Bulwark => new TowerUpgradeEffect { Cost = cost, MaxHpDelta = 400 },
            _ => null
        };
    }
}
