using System;
using System.Collections.Generic;
using LaneWars.Core;
using LaneWars.Buildings;
using LaneWars.Units;

namespace LaneWars.Tests;

public static class TestLaneSimulation
{
    public static void RunAll()
    {
        TestUnitWalks();
        TestUnitsEngageAndFight();
        TestUnitAttacksTowerAtLaneEnd();
        TestTowerRetaliatesAgainstNearbyEnemy();
        Console.WriteLine("All LaneSimulation tests passed!");
    }

    private static void TestUnitWalks()
    {
        var lane = new LaneSimulation(1000);
        var req = new UnitSpawnRequest
        {
            OwnerPlayer = 0, Hp = 100, Damage = 10, AttackCooldownMs = 1000,
            MoveSpeed = 100, Range = 0, ArmorType = 0, DamageType = 0,
            Direction = 1, StartPositionX = 0
        };
        lane.SpawnUnit(req);

        // Tick 10 times at 100ms = 1 second. Speed=100 units/sec => should move 100 units.
        for (int i = 0; i < 10; i++)
            lane.Tick(100);

        Assert(lane.Units[0].PositionX == 100, $"Unit should be at 100, got {lane.Units[0].PositionX}");
    }

    private static void TestUnitsEngageAndFight()
    {
        var lane = new LaneSimulation(1000);
        // Player 0 unit at position 480 moving right
        lane.SpawnUnit(new UnitSpawnRequest
        {
            OwnerPlayer = 0, Hp = 100, Damage = 50, AttackCooldownMs = 100,
            MoveSpeed = 0, Range = 0, ArmorType = 1, DamageType = 0,
            Direction = 1, StartPositionX = 480
        });
        // Player 1 unit at position 500 moving left
        lane.SpawnUnit(new UnitSpawnRequest
        {
            OwnerPlayer = 1, Hp = 100, Damage = 50, AttackCooldownMs = 100,
            MoveSpeed = 0, Range = 0, ArmorType = 1, DamageType = 0,
            Direction = -1, StartPositionX = 500
        });

        // Tick many times -- they should fight and one (or both) should die
        for (int i = 0; i < 50; i++)
            lane.Tick(100);

        // At least one should be dead
        bool anyDead = lane.Units.Count < 2;
        Assert(anyDead, $"At least one unit should have died, but {lane.Units.Count} remain");
    }

    private static void TestUnitAttacksTowerAtLaneEnd()
    {
        var lane = new LaneSimulation(1000);
        // Fast unit at position 990 moving right -- should reach the tower next tick and hit it.
        lane.SpawnUnit(new UnitSpawnRequest
        {
            OwnerPlayer = 0, Hp = 100, Damage = 10, AttackCooldownMs = 1000,
            MoveSpeed = 200, Range = 0, ArmorType = 0, DamageType = 0,
            Direction = 1, StartPositionX = 990
        });

        var towerHits = lane.Tick(100);
        Assert(lane.Units.Count == 1, "Unit should remain alive at the enemy tower");
        Assert(lane.Units[0].PositionX == 1000, $"Unit should stop at the tower, got {lane.Units[0].PositionX}");
        Assert(towerHits.Count > 0, "Unit should have hit the enemy tower");
        Assert(towerHits[0].Player == 1, "Tower hit should damage player 1");
        Assert(towerHits[0].Damage == 10, $"Tower hit should use unit damage, got {towerHits[0].Damage}");
    }

    private static void TestTowerRetaliatesAgainstNearbyEnemy()
    {
        var lane = new LaneSimulation(1000, towerAttackDamage: 25, towerAttackCooldownMs: 100, towerAttackRange: 120);
        lane.SpawnUnit(new UnitSpawnRequest
        {
            OwnerPlayer = 1, Hp = 100, Damage = 10, AttackCooldownMs = 1000,
            MoveSpeed = 0, Range = 0, ArmorType = 0, DamageType = 0,
            Direction = -1, StartPositionX = 60
        });

        lane.Tick(100);

        Assert(lane.Units.Count == 1, "Tower should damage but not kill the unit on the first shot");
        Assert(lane.Units[0].Hp == 75, $"Tower should deal 25 damage, got unit HP {lane.Units[0].Hp}");
        Assert(lane.GetTowerTargetUnitId(0) == lane.Units[0].UnitId, "Player 0 tower should target the nearby enemy unit");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"TEST FAILED: {message}");
    }
}
