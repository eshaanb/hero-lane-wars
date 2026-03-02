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
        Console.WriteLine("All MatchSimulation tests passed!");
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
