using System;
using LaneWars.Buildings;
using LaneWars.Core;

namespace LaneWars.Tests;

/// <summary>
/// Headless strategy tournament for balance iteration (analysis, not a pass/fail test).
/// Both players draw from the SAME symmetric building set, so results reflect the
/// STRATEGY, not faction asymmetry. Reveals whether one strategy dominates (flat) or
/// strategies counter each other (dynamic).
/// </summary>
public static class BalanceHarness
{
    private const int MaxTicks = 3000;
    private const int MaxBuildings = 16; // realistic cap; also bounds unit counts so matches run fast

    public static void Run()
    {
        string[] names = { "Spam", "Econ", "Turtle", "Splash", "Bruiser" };
        Console.WriteLine("=== BALANCE TOURNAMENT (row strategy vs column strategy, row's result) ===");
        Console.Write("          ");
        foreach (var n in names) Console.Write($"{n,-10}");
        Console.WriteLine();

        int[] wins = new int[names.Length];
        int[] games = new int[names.Length];
        long totalTicks = 0;
        int decisive = 0, matches = 0;

        for (int a = 0; a < names.Length; a++)
        {
            Console.Write($"{names[a],-10}");
            for (int b = 0; b < names.Length; b++)
            {
                if (a == b) { Console.Write("--        "); continue; }
                var (winner, ticks) = Play(names[a], names[b]);
                matches++;
                totalTicks += ticks;
                games[a]++; games[b]++;
                if (winner == 0) { wins[a]++; }
                else if (winner == 1) { wins[b]++; }
                if (winner != -1) decisive++;
                string cell = winner == 0 ? $"W {ticks}" : winner == 1 ? $"L {ticks}" : $"draw {ticks}";
                Console.Write($"{cell,-10}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("\nWin rate vs the field:");
        for (int i = 0; i < names.Length; i++)
            Console.WriteLine($"  {names[i],-8} {wins[i]}/{games[i]}  ({(games[i] == 0 ? 0 : 100 * wins[i] / games[i])}%)");
        Console.WriteLine($"Decisive {decisive}/{matches}, avg length {(matches == 0 ? 0 : totalTicks / matches / 10)}s");
    }

    private static (int winner, int ticks) Play(string aName, string bName)
    {
        var sim = new MatchSimulation(
            startingGold: 100, baseIncomePerTick: 10, incomeTickMs: 10000,
            towerHp: 600, buildZoneWidth: 8, buildZoneHeight: 4,
            simTickMs: 100, laneLengthUnits: 600,
            towerAttackDamage: 18, towerAttackCooldownMs: 1000, towerAttackRange: 220);
        var a = new Strategy(aName);
        var b = new Strategy(bName);

        int tick = 0;
        for (; tick < MaxTicks && !sim.IsMatchOver(); tick++)
        {
            a.Act(sim, 0);
            b.Act(sim, 1);
            sim.ProcessTick();
        }
        return (sim.GetWinner(), tick);
    }

    /// <summary>A scripted player strategy that takes one action per tick.</summary>
    private sealed class Strategy
    {
        private readonly string _name;
        private int _cell;

        public Strategy(string name) => _name = name;

        public void Act(MatchSimulation sim, int p)
        {
            switch (_name)
            {
                case "Spam":
                    Place(sim, p, Spawner());
                    break;
                case "Econ":
                    if (sim.GetEconomyBuildingCount(p) < 3) Place(sim, p, Economy());
                    else Place(sim, p, Spawner());
                    break;
                case "Turtle":
                    int cost = sim.GetTowerUpgradeCost(p, TowerBranch.Scattershot);
                    if (cost >= 0 && sim.GetGold(p) - cost >= 30) sim.UpgradeTower(p, TowerBranch.Scattershot);
                    else Place(sim, p, Spawner());
                    break;
                case "Splash":
                    Place(sim, p, Siege());
                    break;
                case "Bruiser":
                    Place(sim, p, Bruiser());
                    break;
            }
        }

        private void Place(MatchSimulation sim, int p, BuildingInfo b)
        {
            if (_cell >= MaxBuildings || sim.GetGold(p) < b.GoldCost) return;
            int x = _cell % 8, y = _cell / 8;
            if (sim.PlaceBuilding(p, x, y, b)) _cell++;
        }
    }

    private static BuildingInfo Spawner() => new BuildingInfo
    {
        BuildingName = "Spawner", GoldCost = 25, GridWidth = 1, GridHeight = 1,
        StrategicRole = StrategicRole.Pressure, CompositionHint = CompositionHint.Swarm,
        UnitHp = 70, UnitDamage = 9, UnitAttackCooldownMs = 1000, UnitMoveSpeed = 80,
        UnitRange = 0, UnitArmorType = 0, UnitDamageType = 0, SpawnTimeMs = 2000, Bounty = 5
    };

    private static BuildingInfo Siege() => new BuildingInfo
    {
        BuildingName = "Siege", GoldCost = 70, GridWidth = 1, GridHeight = 1,
        StrategicRole = StrategicRole.Tech, CompositionHint = CompositionHint.Splash,
        UnitHp = 110, UnitDamage = 22, UnitAttackCooldownMs = 1300, UnitMoveSpeed = 70,
        UnitRange = 40, UnitArmorType = 0, UnitDamageType = 2, SpawnTimeMs = 3500,
        UnitSplashRadius = 50, Bounty = 14
    };

    // Tanky anti-splash bruiser: Magical armor resists Siege splash; few but durable, so a swarm out-masses it.
    private static BuildingInfo Bruiser() => new BuildingInfo
    {
        BuildingName = "Bruiser", GoldCost = 60, GridWidth = 1, GridHeight = 1,
        StrategicRole = StrategicRole.Defense, CompositionHint = CompositionHint.Heavy,
        UnitHp = 260, UnitDamage = 20, UnitAttackCooldownMs = 1100, UnitMoveSpeed = 75,
        UnitRange = 0, UnitArmorType = 3, UnitDamageType = 0, SpawnTimeMs = 3500, Bounty = 12
    };

    private static BuildingInfo Economy() => new BuildingInfo
    {
        BuildingName = "Economy", GoldCost = 50, GridWidth = 1, GridHeight = 1,
        StrategicRole = StrategicRole.Economy, CompositionHint = CompositionHint.Unknown,
        IsEconomyBuilding = true, IncomeBonus = 8, Bounty = 0
    };
}
