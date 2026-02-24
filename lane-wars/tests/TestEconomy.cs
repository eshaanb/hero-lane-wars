using System;
using LaneWars.Core;

namespace LaneWars.Tests;

public static class TestEconomy
{
    public static void RunAll()
    {
        TestStartingGold();
        TestSpendGold();
        TestCannotOverspend();
        TestIncome();
        Console.WriteLine("All Economy tests passed!");
    }

    private static void TestStartingGold()
    {
        var econ = new EconomyManager(100, 10);
        Assert(econ.Gold == 100, $"Starting gold should be 100, got {econ.Gold}");
    }

    private static void TestSpendGold()
    {
        var econ = new EconomyManager(100, 10);
        bool result = econ.TrySpend(25);
        Assert(result, "Should succeed spending 25 from 100");
        Assert(econ.Gold == 75, $"Should have 75 gold, got {econ.Gold}");
    }

    private static void TestCannotOverspend()
    {
        var econ = new EconomyManager(100, 10);
        bool result = econ.TrySpend(150);
        Assert(!result, "Should fail spending 150 from 100");
        Assert(econ.Gold == 100, $"Gold should remain 100, got {econ.Gold}");
    }

    private static void TestIncome()
    {
        var econ = new EconomyManager(100, 10);
        int income = econ.CalculateIncome(2); // 2 economy buildings
        Assert(income == 12, $"Income should be 12 (10 + 2), got {income}");
        econ.AddGold(income);
        Assert(econ.Gold == 112, $"Gold should be 112, got {econ.Gold}");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"TEST FAILED: {message}");
    }
}
