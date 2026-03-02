namespace LaneWars.Core;

public class EconomyManager
{
    public int Gold { get; private set; }
    public int BaseIncome { get; }

    public EconomyManager(int startingGold, int baseIncome = 10)
    {
        Gold = startingGold;
        BaseIncome = baseIncome;
    }

    /// <summary>
    /// Attempt to spend the given amount of gold.
    /// Returns false if the player cannot afford it; gold is unchanged.
    /// </summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0 || Gold < amount)
            return false;

        Gold -= amount;
        return true;
    }

    /// <summary>Add gold (e.g. from income tick or bounty).</summary>
    public void AddGold(int amount)
    {
        if (amount > 0)
            Gold += amount;
    }

    /// <summary>
    /// Calculate income for this tick.
    /// BuildingIncomeBonus is the summed bonus from all placed buildings.
    /// </summary>
    public int CalculateIncome(int buildingIncomeBonus)
    {
        return BaseIncome + buildingIncomeBonus;
    }
}
