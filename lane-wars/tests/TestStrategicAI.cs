using System;
using LaneWars.AI;
using LaneWars.Buildings;
using LaneWars.Core;

namespace LaneWars.Tests;

public static class TestStrategicAI
{
    public static void RunAll()
    {
        TestRushOpensPressureBeforeEconomy();
        TestRushCapsOpeningSpawnerCountBeforeTransition();
        TestGreedOpensEconomyBeforeProduction();
        TestTechOpensCoreThenPrerequisiteThenAdvanced();
        TestDefenseOverrideIsEmergencyOnly();
        TestGreedIsVulnerableToRushBeforePayoff();
        TestRushFallsBehindIfHeld();
        TestTechIsVulnerableBeforeItArrives();
        TestTechPunishesPredictableMonoComposition();
        TestStrongAiBeatsNaiveSpam();
        Console.WriteLine("All StrategicAI tests passed!");
    }

    private static void TestStrongAiBeatsNaiveSpam()
    {
        // A strong AI must punish naive spam no matter which personality it is playing.
        AssertAiBeatsNaiveSpam(AiStrategyPlan.TempoRush);
        AssertAiBeatsNaiveSpam(AiStrategyPlan.EconomyGreed);
        AssertAiBeatsNaiveSpam(AiStrategyPlan.TechCounterScaling);
    }

    private static void AssertAiBeatsNaiveSpam(AiStrategyPlan plan)
    {
        // Towers must actually attack for the tower-defense mechanic to matter (NewSim leaves them at 0).
        var sim = new MatchSimulation(
            startingGold: 100, baseIncomePerTick: 10, incomeTickMs: 10000,
            towerHp: 400, buildZoneWidth: 8, buildZoneHeight: 4,
            simTickMs: 100, laneLengthUnits: 600,
            towerAttackDamage: 18, towerAttackCooldownMs: 1000, towerAttackRange: 220);

        // Naive spammer: floods the cheapest light-armor swarm unit, filling the grid.
        var swarm = UnitInfo("Swarm", StrategicRole.Pressure, 25, CompositionHint.Swarm, hp: 60, damage: 8, spawnTimeMs: 2000);
        swarm.UnitArmorType = 0; // Light
        swarm.Bounty = 5;        // realistic: 25g unit yields ~cost/5 to its killer

        // AI has a basic core spawner plus a splash siege counter available to it.
        var core = UnitInfo("Core", StrategicRole.Pressure, 25, CompositionHint.Mixed, hp: 80, damage: 10, spawnTimeMs: 2000);
        var siege = UnitInfo("Siege", StrategicRole.Tech, 60, CompositionHint.Splash, hp: 130, damage: 20, spawnTimeMs: 3000);
        siege.UnitSplashRadius = 45;
        siege.UnitDamageType = 1; // Piercing: 150% vs Light swarm
        var ai = new SimpleAI(1, core, siegeInfo: siege, thinkIntervalTicks: 0, plan: plan);

        int cell = 0;
        for (int tick = 0; tick < 4000 && !sim.IsMatchOver(); tick++)
        {
            cell = SpamCheapest(sim, 0, swarm, cell);
            ai.Think(sim, 1);
            sim.ProcessTick();
        }

        Assert(sim.GetWinner() == 1 || sim.GetTowerHp(1) > sim.GetTowerHp(0),
            $"{plan} AI should beat naive spam. winner={sim.GetWinner()}, spammerTower={sim.GetTowerHp(0)}, aiTower={sim.GetTowerHp(1)}");
    }

    private static int SpamCheapest(MatchSimulation sim, int player, BuildingInfo spawner, int cell)
    {
        const int gridWidth = 8;
        const int gridCells = 32;
        if (cell >= gridCells)
            return cell;
        if (sim.GetGold(player) < spawner.GoldCost)
            return cell;
        int x = cell % gridWidth;
        int y = cell / gridWidth;
        return sim.PlaceBuilding(player, x, y, spawner) ? cell + 1 : cell;
    }

    private static void TestRushOpensPressureBeforeEconomy()
    {
        var core = UnitInfo("Core", StrategicRole.Pressure, 25, CompositionHint.Swarm);
        var economy = Info("Treasury", StrategicRole.Economy, 50, CompositionHint.Unknown, income: 5, buildDelayMs: 10000);
        var sim = NewSim(startingGold: 70);
        var ai = new SimpleAI(1, core, economyInfo: economy, thinkIntervalTicks: 0, plan: AiStrategyPlan.TempoRush);

        ai.Think(sim, 1);

        Assert(sim.GetBuildingCountByName(1, "Core") == 1, "Rush AI should open pressure");
        Assert(sim.GetBuildingCountByName(1, "Treasury") == 0, "Rush AI should delay economy");
    }

    private static void TestRushCapsOpeningSpawnerCountBeforeTransition()
    {
        var core = UnitInfo("Core", StrategicRole.Pressure, 30, CompositionHint.Swarm);
        var ranged = UnitInfo("Ranged", StrategicRole.Pressure, 36, CompositionHint.Swarm);
        var economy = Info("Treasury", StrategicRole.Economy, 50, CompositionHint.Unknown, income: 5, buildDelayMs: 10000);
        var sim = NewSim(startingGold: 300);
        var ai = new SimpleAI(1, core, rangedInfo: ranged, economyInfo: economy, thinkIntervalTicks: 0, plan: AiStrategyPlan.TempoRush);

        for (int i = 0; i < 6; i++)
            ai.Think(sim, 1);

        int pressureBuildings = sim.GetBuildingCountByName(1, "Core") + sim.GetBuildingCountByName(1, "Ranged");
        Assert(pressureBuildings == 2, $"Rush AI should stop at two opening pressure buildings before transition, got {pressureBuildings}");
        Assert(sim.GetBuildingCountByName(1, "Treasury") == 0, "Rush AI should still delay economy during the opening pressure window");
    }

    private static void TestGreedOpensEconomyBeforeProduction()
    {
        var core = UnitInfo("Core", StrategicRole.Pressure, 25, CompositionHint.Swarm);
        var economy = Info("Treasury", StrategicRole.Economy, 50, CompositionHint.Unknown, income: 5, buildDelayMs: 10000);
        var sim = NewSim(startingGold: 70);
        var ai = new SimpleAI(1, core, economyInfo: economy, thinkIntervalTicks: 0, plan: AiStrategyPlan.EconomyGreed);

        ai.Think(sim, 1);

        Assert(sim.GetBuildingCountByName(1, "Treasury") == 1, "Greed AI should open economy");
        Assert(sim.GetBuildingCountByName(1, "Core") == 0, "Greed AI should delay production");
    }

    private static void TestTechOpensCoreThenPrerequisiteThenAdvanced()
    {
        var core = UnitInfo("Core", StrategicRole.Pressure, 25, CompositionHint.Swarm);
        var armory = UnitInfo("Armory", StrategicRole.Defense, 30, CompositionHint.Heavy, required: new[] { "Core" });
        var siege = UnitInfo("Siege", StrategicRole.Tech, 40, CompositionHint.Splash, required: new[] { "Armory" });
        var sim = NewSim(startingGold: 200);
        var ai = new SimpleAI(1, core, tankInfo: armory, siegeInfo: siege, thinkIntervalTicks: 0, plan: AiStrategyPlan.TechCounterScaling);

        ai.Think(sim, 1);
        ai.Think(sim, 1);
        ai.Think(sim, 1);

        Assert(sim.GetBuildingCountByName(1, "Core") == 1, "Tech AI should open core");
        Assert(sim.GetBuildingCountByName(1, "Armory") == 1, "Tech AI should buy prerequisite");
        Assert(sim.GetBuildingCountByName(1, "Siege") == 1, "Tech AI should buy advanced tech");
    }

    private static void TestDefenseOverrideIsEmergencyOnly()
    {
        var core = UnitInfo("Core", StrategicRole.Pressure, 25, CompositionHint.Swarm);
        var tank = UnitInfo("Guard", StrategicRole.Defense, 50, CompositionHint.Heavy);
        var economy = Info("Treasury", StrategicRole.Economy, 50, CompositionHint.Unknown, income: 5, buildDelayMs: 10000);
        var sim = NewSim(startingGold: 200, laneLength: 1, towerHp: 100);
        var ai = new SimpleAI(1, core, tankInfo: tank, economyInfo: economy, thinkIntervalTicks: 0, plan: AiStrategyPlan.EconomyGreed);

        ai.Think(sim, 1);
        Assert(sim.GetBuildingCountByName(1, "Treasury") == 1, "No-emergency greed AI should follow its economy plan");

        sim.PlaceBuilding(0, 0, 0, UnitInfo("Threat", StrategicRole.Pressure, 0, CompositionHint.Swarm, damage: 25, spawnTimeMs: 100));
        sim.ProcessTick();
        ai.Think(sim, 1);
        Assert(sim.GetBuildingCountByName(1, "Guard") == 1, "Defense override should trigger after leaking");

        ai.Think(sim, 1);
        Assert(sim.GetBuildingCountByName(1, "Treasury") >= 1, "Defense override should not replace the original plan");
    }

    private static void TestGreedIsVulnerableToRushBeforePayoff()
    {
        var core = UnitInfo("Core", StrategicRole.Pressure, 25, CompositionHint.Swarm, damage: 12, spawnTimeMs: 1200);
        var economy = Info("Treasury", StrategicRole.Economy, 50, CompositionHint.Unknown, income: 5, buildDelayMs: 10000);
        var sim = NewSim(startingGold: 70, baseIncome: 10, laneLength: 120, towerHp: 300);
        var greed = new SimpleAI(1, core, economyInfo: economy, thinkIntervalTicks: 0, plan: AiStrategyPlan.EconomyGreed);
        var rush = new SimpleAI(2, core, economyInfo: economy, thinkIntervalTicks: 0, plan: AiStrategyPlan.TempoRush);

        RunAiDuel(sim, greed, rush, 220);

        Assert(sim.GetTowerHp(0) < sim.GetTowerHp(1), "Greed should take more damage than Rush before economy payoff");
    }

    private static void TestRushFallsBehindIfHeld()
    {
        var pressure = UnitInfo("Raider", StrategicRole.Pressure, 25, CompositionHint.Swarm, hp: 70, damage: 5, spawnTimeMs: 1600);
        var defense = UnitInfo("Guard", StrategicRole.Defense, 30, CompositionHint.Heavy, hp: 320, damage: 12, spawnTimeMs: 1500);
        var economy = Info("Treasury", StrategicRole.Economy, 50, CompositionHint.Unknown, income: 5, buildDelayMs: 10000);
        var sim = NewSim(startingGold: 200, baseIncome: 10, laneLength: 160, towerHp: 1000);

        sim.PlaceBuilding(0, 0, 0, pressure);
        sim.PlaceBuilding(0, 1, 0, pressure);
        sim.PlaceBuilding(1, 0, 0, defense);
        sim.PlaceBuilding(1, 1, 0, defense);
        sim.PlaceBuilding(1, 2, 0, economy);

        for (int i = 0; i < 900; i++)
            sim.ProcessTick();

        Assert(sim.GetTowerHp(1) > 0, "Defended player should survive held rush");
        Assert(sim.GetIncomePerTick(1) > sim.GetIncomePerTick(0), "Held rush should fall behind active economy");
    }

    private static void TestTechIsVulnerableBeforeItArrives()
    {
        var core = UnitInfo("Core", StrategicRole.Pressure, 25, CompositionHint.Swarm, damage: 10, spawnTimeMs: 1200);
        var armory = UnitInfo("Armory", StrategicRole.Defense, 55, CompositionHint.Heavy, required: new[] { "Core" });
        var siege = UnitInfo("Siege", StrategicRole.Tech, 80, CompositionHint.Splash, required: new[] { "Armory" });
        var sim = NewSim(startingGold: 70, baseIncome: 10, laneLength: 120, towerHp: 300);
        var tech = new SimpleAI(1, core, tankInfo: armory, siegeInfo: siege, thinkIntervalTicks: 0, plan: AiStrategyPlan.TechCounterScaling);
        var rush = new SimpleAI(2, core, thinkIntervalTicks: 0, plan: AiStrategyPlan.TempoRush);

        RunAiDuel(sim, tech, rush, 220);

        Assert(sim.GetTowerHp(0) < sim.GetTowerHp(1), "Tech should be vulnerable before advanced tech arrives");
    }

    private static void TestTechPunishesPredictableMonoComposition()
    {
        var core = UnitInfo("Core", StrategicRole.Pressure, 25, CompositionHint.Swarm);
        var siege = UnitInfo("Siege", StrategicRole.Tech, 40, CompositionHint.Splash);
        var sim = NewSim(startingGold: 200);
        var tech = new SimpleAI(1, core, siegeInfo: siege, thinkIntervalTicks: 0, plan: AiStrategyPlan.TechCounterScaling);

        sim.PlaceBuilding(0, 0, 0, UnitInfo("Swarm A", StrategicRole.Pressure, 25, CompositionHint.Swarm));
        sim.PlaceBuilding(0, 1, 0, UnitInfo("Swarm B", StrategicRole.Pressure, 25, CompositionHint.Swarm));
        tech.Think(sim, 1);
        tech.Think(sim, 1);

        Assert(sim.GetBuildingCountByName(1, "Siege") == 1, "Tech AI should answer predictable swarm with splash tech");
        Assert(sim.GetScoutRead(1).Tech != ScoutSignalLevel.Low, "Tech purchase should be visible through scout read");
    }

    private static void RunAiDuel(MatchSimulation sim, SimpleAI p0, SimpleAI p1, int ticks)
    {
        for (int i = 0; i < ticks && !sim.IsMatchOver(); i++)
        {
            p0.Think(sim, 0);
            p1.Think(sim, 1);
            sim.ProcessTick();
        }
    }

    private static MatchSimulation NewSim(
        int startingGold = 100,
        int baseIncome = 0,
        int incomeTickMs = 10000,
        int simTickMs = 100,
        int laneLength = 1000,
        int towerHp = 400)
    {
        return new MatchSimulation(startingGold, baseIncome, incomeTickMs, towerHp, 8, 4, simTickMs, laneLength);
    }

    private static BuildingInfo UnitInfo(
        string name,
        StrategicRole role,
        int cost,
        CompositionHint hint,
        string[]? required = null,
        int hp = 100,
        int damage = 10,
        int spawnTimeMs = 1000)
    {
        var info = Info(name, role, cost, hint, required: required);
        info.UnitHp = hp;
        info.UnitDamage = damage;
        info.UnitAttackCooldownMs = 1000;
        info.UnitMoveSpeed = 120;
        info.UnitRange = 0;
        info.UnitArmorType = 1;
        info.UnitDamageType = 0;
        info.SpawnTimeMs = spawnTimeMs;
        info.UnitTowerDamageMultiplierPct = 100;
        return info;
    }

    private static BuildingInfo Info(
        string name,
        StrategicRole role,
        int cost,
        CompositionHint hint,
        int income = 0,
        int buildDelayMs = 0,
        string[]? required = null)
    {
        return new BuildingInfo
        {
            BuildingName = name,
            RequiredBuildingNames = required ?? Array.Empty<string>(),
            UnlocksBuildingNames = Array.Empty<string>(),
            GoldCost = cost,
            BuildDelayMs = buildDelayMs,
            GridWidth = 1,
            GridHeight = 1,
            StrategicRole = role,
            CompositionHint = hint,
            IsEconomyBuilding = income > 0,
            IncomeBonus = income
        };
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"TEST FAILED: {message}");
    }
}
