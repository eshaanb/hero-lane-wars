using System;
using LaneWars.Buildings;
using LaneWars.Core;

namespace LaneWars.Tests;

/// <summary>
/// Regression benchmark for the "spam the cheapest spawner" problem.
/// With splash damage and kill bounties in place, an equal-resource player who
/// answers a mono-swarm spammer with a splash + type-counter composition should win.
/// If this fails, splash/bounty have regressed or been weakened too far.
/// </summary>
public static class TestSpamBenchmark
{
    private const int GridWidth = 4;
    private const int GridHeight = 2;
    private const int GridCells = GridWidth * GridHeight;

    public static void RunAll()
    {
        TestSplashCounterBeatsCheapSwarmSpam();
        Console.WriteLine("All SpamBenchmark tests passed!");
    }

    private static void TestSplashCounterBeatsCheapSwarmSpam()
    {
        var sim = new MatchSimulation(
            startingGold: 100, baseIncomePerTick: 10, incomeTickMs: 10000,
            towerHp: 400, buildZoneWidth: GridWidth, buildZoneHeight: GridHeight,
            simTickMs: 100, laneLengthUnits: 600,
            towerAttackDamage: 18, towerAttackCooldownMs: 1000, towerAttackRange: 220);

        BuildingInfo swarm = SwarmSpawner();       // player 0 spams this
        BuildingInfo splasher = SplashSpawner();   // player 1 answers with this

        int p0Cell = 0, p1Cell = 0;
        for (int tick = 0; tick < 4000 && !sim.IsMatchOver(); tick++)
        {
            p0Cell = TryPlaceNext(sim, 0, swarm, p0Cell);
            p1Cell = TryPlaceNext(sim, 1, splasher, p1Cell);
            sim.ProcessTick();
        }

        bool counterAhead = sim.GetWinner() == 1 || sim.GetTowerHp(1) > sim.GetTowerHp(0);
        Assert(counterAhead,
            "Splash + bounty counter should beat equal-resource cheap swarm spam. " +
            $"winner={sim.GetWinner()}, p0Tower={sim.GetTowerHp(0)}, p1Tower={sim.GetTowerHp(1)}");
    }

    /// <summary>Place the spawner in the next free grid cell when affordable; returns the updated cursor.</summary>
    private static int TryPlaceNext(MatchSimulation sim, int player, BuildingInfo info, int cell)
    {
        if (cell >= GridCells)
            return cell;
        if (sim.GetGold(player) < info.GoldCost)
            return cell;

        int x = cell % GridWidth;
        int y = cell / GridWidth;
        return sim.PlaceBuilding(player, x, y, info) ? cell + 1 : cell;
    }

    private static BuildingInfo SwarmSpawner() => new BuildingInfo
    {
        BuildingName = "Swarm", GoldCost = 25, GridWidth = 1, GridHeight = 1,
        StrategicRole = StrategicRole.Pressure, CompositionHint = CompositionHint.Swarm,
        UnitHp = 60, UnitDamage = 8, UnitAttackCooldownMs = 1000, UnitMoveSpeed = 80,
        UnitRange = 0, UnitArmorType = 0 /* Light */, UnitDamageType = 0 /* Physical */,
        SpawnTimeMs = 2000, Bounty = 4
    };

    private static BuildingInfo SplashSpawner() => new BuildingInfo
    {
        BuildingName = "Splasher", GoldCost = 60, GridWidth = 1, GridHeight = 1,
        StrategicRole = StrategicRole.Tech, CompositionHint = CompositionHint.Splash,
        UnitHp = 140, UnitDamage = 22, UnitAttackCooldownMs = 1200, UnitMoveSpeed = 70,
        UnitRange = 40, UnitArmorType = 1 /* Medium */, UnitDamageType = 1 /* Piercing: 150% vs Light swarm */,
        SpawnTimeMs = 3000, UnitSplashRadius = 45, Bounty = 12
    };

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"TEST FAILED: {message}");
    }
}
