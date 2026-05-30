using System;
using LaneWars.Core;

namespace LaneWars.Tests;

public static class TestMatchSimulation
{
    public static void RunAll()
    {
        TestTowerHpControlsWinCondition();
        TestPrerequisiteBlocksPlacement();
        TestSupportBuildingBuffsFutureSpawns();
        TestSiegeUnitDealsBonusTowerDamage();
        TestBountyCreditsKillerGold();
        TestTowerUpgradeIsExclusiveAndLevels();
        TestTowerUpgradeEffectsApply();
        Console.WriteLine("All MatchSimulation tests passed!");
    }

    private static void TestTowerUpgradeEffectsApply()
    {
        // Ballista: +14 damage, +25 range per level.
        var ballistaSim = NewUpgradeSim(towerDamage: 20, towerRange: 200);
        Assert(ballistaSim.UpgradeTower(0, TowerBranch.Ballista), "Ballista L1 should apply");
        Assert(ballistaSim.GetTowerAttackDamage(0) == 34, $"Ballista should add 14 damage, got {ballistaSim.GetTowerAttackDamage(0)}");
        Assert(ballistaSim.GetTowerAttackRange(0) == 225, $"Ballista should add 25 range, got {ballistaSim.GetTowerAttackRange(0)}");

        // Scattershot: +50 splash radius per level.
        var scatterSim = NewUpgradeSim(towerDamage: 20, towerRange: 200);
        Assert(scatterSim.GetTowerSplashRadius(0) == 0, "Tower starts with no splash");
        Assert(scatterSim.UpgradeTower(0, TowerBranch.Scattershot), "Scattershot L1 should apply");
        Assert(scatterSim.GetTowerSplashRadius(0) == 50, $"Scattershot should add 50 splash, got {scatterSim.GetTowerSplashRadius(0)}");

        // Bulwark: +400 max HP, healing current HP up to the new max.
        var bulwarkSim = NewUpgradeSim(towerDamage: 20, towerRange: 200);
        Assert(bulwarkSim.UpgradeTower(0, TowerBranch.Bulwark), "Bulwark L1 should apply");
        Assert(bulwarkSim.GetStartingTowerHp(0) == 1200, $"Bulwark should raise max HP to 1200, got {bulwarkSim.GetStartingTowerHp(0)}");
        Assert(bulwarkSim.GetTowerHp(0) == 1200, $"Bulwark should heal current HP to the new max, got {bulwarkSim.GetTowerHp(0)}");
    }

    private static MatchSimulation NewUpgradeSim(int towerDamage, int towerRange)
    {
        return new MatchSimulation(
            startingGold: 1000, baseIncomePerTick: 0, incomeTickMs: 10000,
            towerHp: 800, buildZoneWidth: 1, buildZoneHeight: 1,
            simTickMs: 100, laneLengthUnits: 100,
            towerAttackDamage: towerDamage, towerAttackCooldownMs: 1000, towerAttackRange: towerRange);
    }

    private static void TestTowerUpgradeIsExclusiveAndLevels()
    {
        var sim = new MatchSimulation(
            startingGold: 1000, baseIncomePerTick: 0, incomeTickMs: 10000,
            towerHp: 800, buildZoneWidth: 1, buildZoneHeight: 1,
            simTickMs: 100, laneLengthUnits: 100);

        Assert(sim.GetTowerBranch(0) == TowerBranch.None, "Tower starts with no branch");
        Assert(sim.GetTowerUpgradeLevel(0) == 0, "Tower starts at level 0");

        Assert(sim.UpgradeTower(0, TowerBranch.Ballista), "First Ballista upgrade should succeed");
        Assert(sim.GetTowerBranch(0) == TowerBranch.Ballista, "Tower should commit to Ballista");
        Assert(sim.GetTowerUpgradeLevel(0) == 1, "Tower should be level 1");
        Assert(sim.GetGold(0) == 945, $"Level 1 should cost 55, got gold {sim.GetGold(0)}");

        Assert(!sim.UpgradeTower(0, TowerBranch.Scattershot), "Committed tower should reject a different branch");
        Assert(sim.GetTowerBranch(0) == TowerBranch.Ballista, "Branch should remain Ballista after a rejected switch");
        Assert(sim.GetGold(0) == 945, "Rejected upgrade should not spend gold");

        Assert(sim.UpgradeTower(0, TowerBranch.Ballista), "Second Ballista level should succeed");
        Assert(sim.GetTowerUpgradeLevel(0) == 2, "Tower should be level 2");
        Assert(sim.GetGold(0) == 845, $"Level 2 should cost 100, got gold {sim.GetGold(0)}");

        Assert(sim.UpgradeTower(0, TowerBranch.Ballista), "Third Ballista level should succeed");
        Assert(sim.GetTowerUpgradeLevel(0) == 3, "Tower should be level 3");
        Assert(sim.GetGold(0) == 685, $"Level 3 should cost 160, got gold {sim.GetGold(0)}");

        Assert(!sim.UpgradeTower(0, TowerBranch.Ballista), "Tower should not exceed max level");
        Assert(sim.GetTowerUpgradeLevel(0) == 3, "Tower should stay at max level 3");
    }

    private static void TestBountyCreditsKillerGold()
    {
        var sim = new MatchSimulation(
            startingGold: 0, baseIncomePerTick: 0, incomeTickMs: 10000,
            towerHp: 100000, buildZoneWidth: 1, buildZoneHeight: 1,
            simTickMs: 100, laneLengthUnits: 200);

        // Player 0: a stationary, unkillable killer that one-shots whatever arrives.
        var killer = new BuildingInfo
        {
            BuildingName = "Killer", GoldCost = 0, GridWidth = 1, GridHeight = 1,
            UnitHp = 100000, UnitDamage = 1000, UnitAttackCooldownMs = 100,
            UnitMoveSpeed = 0, UnitRange = 0, UnitArmorType = 1, UnitDamageType = 0,
            SpawnTimeMs = 100, Bounty = 0
        };
        // Player 1: weak victims worth 5 gold each, dealing no damage back.
        var victim = new BuildingInfo
        {
            BuildingName = "Victim", GoldCost = 0, GridWidth = 1, GridHeight = 1,
            UnitHp = 10, UnitDamage = 0, UnitAttackCooldownMs = 1000,
            UnitMoveSpeed = 100, UnitRange = 0, UnitArmorType = 1, UnitDamageType = 0,
            SpawnTimeMs = 100, Bounty = 5
        };

        Assert(sim.PlaceBuilding(0, 0, 0, killer), "Killer building should place");
        Assert(sim.PlaceBuilding(1, 0, 0, victim), "Victim building should place");

        for (int i = 0; i < 80; i++)
            sim.ProcessTick();

        Assert(sim.GetGold(0) > 0, $"Killer's owner should earn bounty gold, got {sim.GetGold(0)}");
        Assert(sim.GetGold(0) % 5 == 0, $"Bounty gold should be a multiple of 5, got {sim.GetGold(0)}");
        Assert(sim.GetGold(1) == 0, $"Victim's owner should earn no bounty, got {sim.GetGold(1)}");
    }

    private static void TestTowerHpControlsWinCondition()
    {
        var sim = new MatchSimulation(
            startingGold: 0,
            baseIncomePerTick: 0,
            incomeTickMs: 10000,
            towerHp: 20,
            buildZoneWidth: 1,
            buildZoneHeight: 1,
            simTickMs: 100,
            laneLengthUnits: 1);

        var info = new BuildingInfo
        {
            GoldCost = 0,
            GridWidth = 1,
            GridHeight = 1,
            IsEconomyBuilding = false,
            IncomeBonus = 0,
            UnitHp = 50,
            UnitDamage = 5,
            UnitAttackCooldownMs = 100,
            UnitMoveSpeed = 20,
            UnitRange = 0,
            UnitArmorType = 0,
            UnitDamageType = 0,
            SpawnTimeMs = 100,
            UnitSpritePath = ""
        };

        bool placed = sim.PlaceBuilding(0, 0, 0, info);
        Assert(placed, "Should place the building successfully");

        for (int i = 0; i < 4; i++)
            sim.ProcessTick();

        Assert(sim.GetTowerHp(1) == 0, $"Enemy tower should be destroyed, got {sim.GetTowerHp(1)}");
        Assert(sim.IsMatchOver(), "Match should end when a tower reaches 0 HP");
        Assert(sim.GetWinner() == 0, $"Player 0 should win, got {sim.GetWinner()}");
    }

    private static void TestPrerequisiteBlocksPlacement()
    {
        var sim = new MatchSimulation(100, 0, 10000, 100, 2, 1, 100, 10);
        var advanced = new BuildingInfo
        {
            BuildingName = "Armory",
            RequiredBuildingNames = new[] { "Barracks" },
            GoldCost = 0,
            GridWidth = 1,
            GridHeight = 1,
            UnitHp = 100,
            UnitDamage = 10,
            UnitAttackCooldownMs = 100,
            UnitMoveSpeed = 10,
            SpawnTimeMs = 100
        };

        bool placed = sim.PlaceBuilding(0, 0, 0, advanced);
        Assert(!placed, "Placement should fail when the prerequisite building is missing");
    }

    private static void TestSupportBuildingBuffsFutureSpawns()
    {
        var sim = new MatchSimulation(100, 0, 10000, 500, 2, 1, 100, 1000);

        var barracks = new BuildingInfo
        {
            BuildingName = "Barracks",
            GoldCost = 0,
            GridWidth = 1,
            GridHeight = 1,
            UnitHp = 50,
            UnitDamage = 10,
            UnitAttackCooldownMs = 1000,
            UnitMoveSpeed = 50,
            SpawnTimeMs = 100
        };
        var forge = new BuildingInfo
        {
            BuildingName = "Forge",
            RequiredBuildingNames = new[] { "Barracks" },
            GoldCost = 0,
            GridWidth = 1,
            GridHeight = 1,
            SupportDamageBonus = 4
        };

        Assert(sim.PlaceBuilding(0, 0, 0, barracks), "Barracks should place");
        Assert(sim.PlaceBuilding(0, 1, 0, forge), "Forge should place after Barracks");

        sim.ProcessTick();

        Assert(sim.Units.Count > 0, "Barracks should have spawned a unit");
        Assert(sim.Units[0].Damage == 14, $"Forge should increase spawned unit damage to 14, got {sim.Units[0].Damage}");
    }

    private static void TestSiegeUnitDealsBonusTowerDamage()
    {
        var sim = new MatchSimulation(100, 0, 10000, 100, 1, 1, 100, 1);
        var siege = new BuildingInfo
        {
            BuildingName = "Siege Workshop",
            GoldCost = 0,
            GridWidth = 1,
            GridHeight = 1,
            UnitHp = 50,
            UnitDamage = 10,
            UnitAttackCooldownMs = 100,
            UnitMoveSpeed = 20,
            SpawnTimeMs = 100,
            UnitTowerDamageMultiplierPct = 300
        };

        Assert(sim.PlaceBuilding(0, 0, 0, siege), "Siege building should place");
        sim.ProcessTick();

        Assert(sim.GetTowerHp(1) == 70, $"Siege unit should deal 30 tower damage, got tower HP {sim.GetTowerHp(1)}");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"TEST FAILED: {message}");
    }
}
