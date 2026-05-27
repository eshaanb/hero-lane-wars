using System;
using System.Reflection;
using LaneWars.AI;
using LaneWars.Buildings;
using LaneWars.Core;

namespace LaneWars.Tests;

public static class TestStrategicDepth
{
    public static void RunAll()
    {
        TestMetadataPropagatesIntoSpendingHistory();
        TestScoutSignalsFollowStrategicRoles();
        TestScoutReadDoesNotExposeExactDetails();
        TestEconomyActivationDelay();
        TestOpeningEconomyBlocksImmediateMajorOption();
        TestUndeadOpeningCreatesSecondBeatBranchChoice();
        TestAffordableBuildingsCanStartTogether();
        TestConstructionProgressIsPerBuilding();
        TestProductionBuildDelayPreventsImmediateSpawn();
        TestPostgameAnalysisPatterns();
        Console.WriteLine("All StrategicDepth tests passed!");
    }

    private static void TestMetadataPropagatesIntoSpendingHistory()
    {
        BuildingInfo info = Info("Hidden Mint", StrategicRole.Economy, 50, CompositionHint.Unknown, income: 5, buildDelayMs: 10000);
        Assert(info.GoldCost == 50, "Gold cost should be carried into pure simulation info");
        Assert(info.BuildDelayMs == 10000, "Build delay should be carried into pure simulation info");
        Assert(info.StrategicRole == StrategicRole.Economy, "Strategic role should propagate into BuildingInfo");
        Assert(info.CompositionHint == CompositionHint.Unknown, "Composition hint should propagate into BuildingInfo");

        var sim = NewSim(startingGold: 70);
        Assert(sim.PlaceBuilding(0, 0, 0, info), "Economy building should place");
        RecentSpendingSummary summary = sim.GetRecentSpendingSummary(0);
        Assert(summary.Economy == ScoutSignalLevel.Medium, "Economy spending should create an economy scout signal");
        Assert(summary.EventCount == 1, "Spending summary should count the placement event");
        Assert(sim.GetFirstInvestmentTime(0, StrategicRole.Economy) == 0, "First economy timing should be recorded");
    }

    private static void TestScoutSignalsFollowStrategicRoles()
    {
        var sim = NewSim(startingGold: 300);
        Assert(sim.PlaceBuilding(0, 0, 0, Info("Mint", StrategicRole.Economy, 50, CompositionHint.Unknown, income: 5)), "Economy should place");
        Assert(sim.PlaceBuilding(0, 1, 0, Info("War Camp", StrategicRole.Pressure, 25, CompositionHint.Swarm)), "Pressure should place");
        Assert(sim.PlaceBuilding(0, 2, 0, Info("Siege Yard", StrategicRole.Tech, 80, CompositionHint.Splash)), "Tech should place");

        ScoutRead read = sim.GetScoutRead(0);
        Assert(read.Economy == ScoutSignalLevel.Medium, "Economy purchase should raise economy signal");
        Assert(read.Pressure == ScoutSignalLevel.Medium, "Pressure purchase should raise pressure signal");
        Assert(read.Tech == ScoutSignalLevel.High, "Large tech purchase should raise tech signal");
        Assert(read.CompositionHint == CompositionHint.Splash || read.CompositionHint == CompositionHint.Mixed,
            "Scout composition should reflect recent combat-oriented spending");
    }

    private static void TestScoutReadDoesNotExposeExactDetails()
    {
        AssertNoLeakyMembers(typeof(ScoutRead));
        AssertNoLeakyMembers(typeof(RecentSpendingSummary));
    }

    private static void TestEconomyActivationDelay()
    {
        var sim = NewSim(startingGold: 70, baseIncome: 10, incomeTickMs: 10000, simTickMs: 10000);
        var economy = Info("Treasury", StrategicRole.Economy, 50, CompositionHint.Unknown, income: 5, buildDelayMs: 10000);

        Assert(sim.PlaceBuilding(0, 0, 0, economy), "Economy building should place");
        Assert(sim.GetGold(0) == 20, $"Gold should be 20 after economy opener, got {sim.GetGold(0)}");

        sim.ProcessTick();
        Assert(sim.GetGold(0) == 30, $"First income tick should not include delayed economy, got {sim.GetGold(0)}");

        sim.ProcessTick();
        Assert(sim.GetGold(0) == 45, $"Second income tick should include active economy, got {sim.GetGold(0)}");
    }

    private static void TestOpeningEconomyBlocksImmediateMajorOption()
    {
        var sim = NewSim(startingGold: 70);
        var economy = Info("Treasury", StrategicRole.Economy, 50, CompositionHint.Unknown, income: 5, buildDelayMs: 10000);
        var pressure = Info("Barracks", StrategicRole.Pressure, 25, CompositionHint.Mixed);

        Assert(sim.PlaceBuilding(0, 0, 0, economy), "Economy opener should place");
        Assert(!sim.PlaceBuilding(0, 1, 0, pressure), "Opening economy should leave too little gold for another major option");
    }

    private static void TestUndeadOpeningCreatesSecondBeatBranchChoice()
    {
        var crypt = Info("Crypt", StrategicRole.Pressure, 30, CompositionHint.Swarm);
        var secondCrypt = Info("Crypt", StrategicRole.Pressure, 30, CompositionHint.Swarm);
        var boneReliquary = Info("Bone Reliquary", StrategicRole.Defense, 44, CompositionHint.Heavy, required: new[] { "Crypt" });
        var soulWell = Info("Soul Well", StrategicRole.Tech, 40, CompositionHint.Mixed, required: new[] { "Crypt" });
        var blackMint = Info("Black Mint", StrategicRole.Economy, 50, CompositionHint.Unknown, income: 5, buildDelayMs: 10000);

        AssertSecondBeatBranchIsExclusive(crypt, secondCrypt, "second Crypt");
        AssertSecondBeatBranchIsExclusive(crypt, boneReliquary, "Bone Reliquary");
        AssertSecondBeatBranchIsExclusive(crypt, soulWell, "Soul Well");
        AssertSecondBeatBranchIsExclusive(crypt, blackMint, "Black Mint");
    }

    private static void AssertSecondBeatBranchIsExclusive(BuildingInfo opener, BuildingInfo branch, string branchName)
    {
        var sim = NewSim(startingGold: 70, baseIncome: 10, incomeTickMs: 10000, simTickMs: 1000);

        Assert(sim.PlaceBuilding(0, 0, 0, opener), "Crypt opener should place");
        for (int i = 0; i < 10; i++)
            sim.ProcessTick();

        Assert(sim.GetGold(0) == 50, $"After Crypt plus first income, gold should be 50, got {sim.GetGold(0)}");
        Assert(sim.PlaceBuilding(0, 1, 0, branch), $"{branchName} should be a valid second-beat branch");
        Assert(!sim.PlaceBuilding(0, 2, 0, opener), $"{branchName} branch should consume the second decision beat instead of allowing immediate Crypt spam");
    }

    private static void TestAffordableBuildingsCanStartTogether()
    {
        var sim = NewSim(startingGold: 200, simTickMs: 1000);
        var pressure = Info("Barracks", StrategicRole.Pressure, 25, CompositionHint.Mixed);

        Assert(sim.PlaceBuilding(0, 0, 0, pressure), "First spawner should place");
        Assert(sim.PlaceBuilding(0, 1, 0, pressure), "Second affordable spawner should place immediately");
        Assert(sim.PlaceBuilding(0, 2, 0, pressure), "Third affordable spawner should place immediately");
        Assert(sim.GetGold(0) == 125, $"Three 25g buildings should leave 125 gold, got {sim.GetGold(0)}");
    }

    private static void TestConstructionProgressIsPerBuilding()
    {
        var sim = NewSim(startingGold: 200, simTickMs: 1000);
        var fast = Info("Fast Yard", StrategicRole.Pressure, 25, CompositionHint.Mixed, buildDelayMs: 3000);
        var slow = Info("Slow Yard", StrategicRole.Tech, 25, CompositionHint.Splash, buildDelayMs: 7000);

        Assert(sim.PlaceBuilding(0, 0, 0, fast), "Fast building should place");
        Assert(sim.PlaceBuilding(0, 1, 0, slow), "Slow building should place at the same time");

        Assert(sim.TryGetBuildingAt(0, 0, 0, out var fastState), "Fast building state should be inspectable");
        Assert(sim.TryGetBuildingAt(0, 1, 0, out var slowState), "Slow building state should be inspectable");
        Assert(fastState.BuildDelayRemainingMs == 3000, "Fast building should start with its full construction time");
        Assert(slowState.BuildDelayRemainingMs == 7000, "Slow building should start with its full construction time");

        for (int i = 0; i < 3; i++)
            sim.ProcessTick();

        Assert(sim.TryGetBuildingAt(0, 0, 0, out fastState), "Fast building state should remain available");
        Assert(sim.TryGetBuildingAt(0, 1, 0, out slowState), "Slow building state should remain available");
        Assert(fastState.BuildDelayRemainingMs == 0, "Fast building should complete independently");
        Assert(slowState.BuildDelayRemainingMs == 4000, $"Slow building should keep building, got {slowState.BuildDelayRemainingMs}");
    }

    private static void TestProductionBuildDelayPreventsImmediateSpawn()
    {
        var sim = NewSim(startingGold: 100, simTickMs: 1000);
        var pressure = UnitInfo("Barracks", StrategicRole.Pressure, 25, CompositionHint.Mixed, buildDelayMs: 5000, spawnTimeMs: 1000);

        Assert(sim.PlaceBuilding(0, 0, 0, pressure), "Delayed production building should place");
        for (int i = 0; i < 5; i++)
            sim.ProcessTick();

        Assert(sim.Units.Count == 0, "Production build delay should prevent immediate unit output");
        sim.ProcessTick();
        Assert(sim.Units.Count == 1, "Production should begin after build delay plus one spawn cycle");
    }

    private static void TestPostgameAnalysisPatterns()
    {
        var core = Info("Core", StrategicRole.Pressure, 25, CompositionHint.Swarm);

        var rushSim = NewSim(startingGold: 200);
        rushSim.PlaceBuilding(0, 0, 0, Info("Treasury", StrategicRole.Economy, 50, CompositionHint.Unknown));
        rushSim.PlaceBuilding(1, 0, 0, core);
        var rushAi = new SimpleAI(1, core, plan: AiStrategyPlan.TempoRush);
        Assert(GameManager.BuildPostgameAnalysis(rushSim, 0, 1, rushAi).LikelyMistake.Contains("built income"),
            "Rush analysis should explain greed into pressure");

        var greedSim = NewSim(startingGold: 200);
        greedSim.PlaceBuilding(1, 0, 0, Info("Treasury", StrategicRole.Economy, 50, CompositionHint.Unknown));
        var greedAi = new SimpleAI(1, core, plan: AiStrategyPlan.EconomyGreed);
        Assert(GameManager.BuildPostgameAnalysis(greedSim, 0, 1, greedAi).LikelyMistake.Contains("did not punish"),
            "Greed analysis should explain unpunished economy");

        var techSim = NewSim(startingGold: 200);
        techSim.PlaceBuilding(1, 0, 0, Info("Workshop", StrategicRole.Tech, 80, CompositionHint.Splash));
        var techAi = new SimpleAI(1, core, plan: AiStrategyPlan.TechCounterScaling);
        Assert(GameManager.BuildPostgameAnalysis(techSim, 0, 1, techAi).LikelyMistake.Contains("teched early"),
            "Tech analysis should explain giving tech enough time");

        var failedPressureSim = NewSim(startingGold: 200);
        failedPressureSim.PlaceBuilding(0, 0, 0, core);
        Assert(GameManager.BuildPostgameAnalysis(failedPressureSim, 0, 1, greedAi).LikelyMistake.Contains("early pressure failed"),
            "Failed pressure analysis should suggest a transition");
    }

    private static void AssertNoLeakyMembers(Type type)
    {
        foreach (MemberInfo member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public))
        {
            string name = member.Name;
            Assert(!name.Contains("Building"), $"{type.Name} should not expose exact building details");
            Assert(!name.Contains("Gold"), $"{type.Name} should not expose gold");
            Assert(!name.Contains("Income"), $"{type.Name} should not expose income");
            Assert(!name.Contains("Queue"), $"{type.Name} should not expose queues");
            Assert(!name.Contains("Order"), $"{type.Name} should not expose build order");
        }
    }

    private static MatchSimulation NewSim(
        int startingGold = 100,
        int baseIncome = 0,
        int incomeTickMs = 10000,
        int simTickMs = 100)
    {
        return new MatchSimulation(startingGold, baseIncome, incomeTickMs, 400, 8, 4, simTickMs, 1000);
    }

    private static BuildingInfo UnitInfo(
        string name,
        StrategicRole role,
        int cost,
        CompositionHint hint,
        int buildDelayMs = 0,
        int spawnTimeMs = 1000)
    {
        var info = Info(name, role, cost, hint, buildDelayMs: buildDelayMs);
        info.UnitHp = 100;
        info.UnitDamage = 10;
        info.UnitAttackCooldownMs = 1000;
        info.UnitMoveSpeed = 100;
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
