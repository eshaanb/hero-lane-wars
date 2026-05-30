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
        TestSplashDamageHitsStackedEnemies();
        TestSingleTargetDoesNotSplash();
        TestKillAwardsBountyToKiller();
        TestTowerSplashHitsMultipleEnemies();
        Console.WriteLine("All LaneSimulation tests passed!");
    }

    private static void TestTowerSplashHitsMultipleEnemies()
    {
        // Player 0 tower at position 0 with splash; three stacked enemies in range.
        var lane = new LaneSimulation(1000, towerAttackDamage: 30, towerAttackCooldownMs: 100, towerAttackRange: 120);
        lane.AddTowerSplashRadius(0, 60);

        int a = lane.SpawnUnit(NoDamageEnemy(40));
        int b = lane.SpawnUnit(NoDamageEnemy(60));
        int c = lane.SpawnUnit(NoDamageEnemy(80));

        lane.Tick(100);

        Assert(UnitHp(lane, a) == 70, $"Tower primary should take 30, got {UnitHp(lane, a)}");
        Assert(UnitHp(lane, b) == 70, $"Tower splash should hit enemy at 60, got {UnitHp(lane, b)}");
        Assert(UnitHp(lane, c) == 70, $"Tower splash should hit enemy at 80, got {UnitHp(lane, c)}");
    }

    private static void TestKillAwardsBountyToKiller()
    {
        var lane = new LaneSimulation(1000);
        // Player 0 attacker one-shots the victim; carries no bounty itself.
        lane.SpawnUnit(new UnitSpawnRequest
        {
            OwnerPlayer = 0, Hp = 1000, Damage = 1000, AttackCooldownMs = 100,
            MoveSpeed = 0, Range = 0, ArmorType = 1, DamageType = 0,
            Direction = 1, StartPositionX = 480, Bounty = 0
        });
        // Player 1 victim worth 7 bounty, deals no damage back.
        lane.SpawnUnit(new UnitSpawnRequest
        {
            OwnerPlayer = 1, Hp = 10, Damage = 0, AttackCooldownMs = 1000,
            MoveSpeed = 0, Range = 0, ArmorType = 1, DamageType = 0,
            Direction = -1, StartPositionX = 500, Bounty = 7
        });

        int p0Bounty = 0, p1Bounty = 0;
        for (int i = 0; i < 5; i++)
        {
            lane.Tick(100);
            p0Bounty += lane.GetLastTickBounty(0);
            p1Bounty += lane.GetLastTickBounty(1);
        }

        Assert(p0Bounty == 7, $"Killer (player 0) should earn the victim's 7 bounty, got {p0Bounty}");
        Assert(p1Bounty == 0, $"Victim's owner should earn no bounty, got {p1Bounty}");
    }

    private static void TestSplashDamageHitsStackedEnemies()
    {
        var lane = new LaneSimulation(1000);
        // Player 0 splash attacker at 480 (melee). Big HP so it survives; one hit per tick.
        lane.SpawnUnit(new UnitSpawnRequest
        {
            OwnerPlayer = 0, Hp = 1000, Damage = 50, AttackCooldownMs = 1000,
            MoveSpeed = 0, Range = 0, ArmorType = 1, DamageType = 0,
            Direction = 1, StartPositionX = 480, SplashRadius = 60
        });
        // Three stacked player-1 enemies that deal no damage back.
        int primaryId = lane.SpawnUnit(NoDamageEnemy(500));
        int splash1Id = lane.SpawnUnit(NoDamageEnemy(510));
        int splash2Id = lane.SpawnUnit(NoDamageEnemy(520));

        lane.Tick(100);

        // Primary takes one direct hit; splash targets each take one hit (no double-hit on primary).
        Assert(UnitHp(lane, primaryId) == 50, $"Primary target should take one 50 hit, got {UnitHp(lane, primaryId)}");
        Assert(UnitHp(lane, splash1Id) == 50, $"Splash should hit enemy at 510, got {UnitHp(lane, splash1Id)}");
        Assert(UnitHp(lane, splash2Id) == 50, $"Splash should hit enemy at 520, got {UnitHp(lane, splash2Id)}");
    }

    private static void TestSingleTargetDoesNotSplash()
    {
        var lane = new LaneSimulation(1000);
        // SplashRadius 0 (default) attacker should only hit its primary target.
        lane.SpawnUnit(new UnitSpawnRequest
        {
            OwnerPlayer = 0, Hp = 1000, Damage = 50, AttackCooldownMs = 1000,
            MoveSpeed = 0, Range = 0, ArmorType = 1, DamageType = 0,
            Direction = 1, StartPositionX = 480
        });
        int primaryId = lane.SpawnUnit(NoDamageEnemy(500));
        int neighborId = lane.SpawnUnit(NoDamageEnemy(510));

        lane.Tick(100);

        Assert(UnitHp(lane, primaryId) == 50, $"Primary target should take 50, got {UnitHp(lane, primaryId)}");
        Assert(UnitHp(lane, neighborId) == 100, $"Non-splash attacker must not hit the neighbor, got {UnitHp(lane, neighborId)}");
    }

    private static UnitSpawnRequest NoDamageEnemy(int positionX)
    {
        return new UnitSpawnRequest
        {
            OwnerPlayer = 1, Hp = 100, Damage = 0, AttackCooldownMs = 1000,
            MoveSpeed = 0, Range = 0, ArmorType = 1, DamageType = 0,
            Direction = -1, StartPositionX = positionX
        };
    }

    private static int UnitHp(LaneSimulation lane, int unitId)
    {
        foreach (var u in lane.Units)
            if (u.UnitId == unitId)
                return u.Hp;
        return -1;
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
