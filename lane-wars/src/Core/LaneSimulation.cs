namespace LaneWars.Core;

using System;
using System.Collections.Generic;
using LaneWars.Units;
using LaneWars.Buildings;

public struct TowerDamage
{
    public int Player;  // which player's tower takes damage
    public int Damage;
}

/// <summary>
/// Heart of the game: owns all units in a single lane and runs the
/// move -> acquire -> attack -> tower pressure -> sweep pipeline each tick.
/// Pure C#, no Godot dependencies. Integer math only.
/// </summary>
public class LaneSimulation
{
    private readonly List<UnitState> _units = new();
    private int _nextUnitId = 0;
    private readonly int _laneLengthUnits;
    private readonly int[] _towerAttackDamage = new int[2];
    private readonly int[] _towerAttackCooldownMs = new int[2];
    private readonly int[] _towerAttackRange = new int[2];
    private readonly int[] _towerAttackTimerMs = new int[2];
    private readonly int[] _towerTargetUnitId = new int[2] { -1, -1 };
    private readonly int[] _towerRecentAttackMs = new int[2];
    private readonly int[] _towerSplashRadius = new int[2];
    private readonly int[] _lastTickBounty = new int[2];
    private const int MeleeRange = 50;

    public IReadOnlyList<UnitState> Units => _units;
    public int LaneLengthUnits => _laneLengthUnits;

    public LaneSimulation(
        int laneLengthUnits,
        int towerAttackDamage = 0,
        int towerAttackCooldownMs = 0,
        int towerAttackRange = 0,
        int[]? towerAttackDamageByPlayer = null,
        int[]? towerAttackCooldownMsByPlayer = null,
        int[]? towerAttackRangeByPlayer = null)
    {
        _laneLengthUnits = laneLengthUnits;
        for (int player = 0; player < 2; player++)
        {
            _towerAttackDamage[player] = towerAttackDamageByPlayer != null && player < towerAttackDamageByPlayer.Length
                ? towerAttackDamageByPlayer[player]
                : towerAttackDamage;
            _towerAttackCooldownMs[player] = towerAttackCooldownMsByPlayer != null && player < towerAttackCooldownMsByPlayer.Length
                ? towerAttackCooldownMsByPlayer[player]
                : towerAttackCooldownMs;
            _towerAttackRange[player] = towerAttackRangeByPlayer != null && player < towerAttackRangeByPlayer.Length
                ? towerAttackRangeByPlayer[player]
                : towerAttackRange;
        }
    }

    public int GetTowerTargetUnitId(int player) => player >= 0 && player <= 1 ? _towerTargetUnitId[player] : -1;
    public int GetTowerRecentAttackMs(int player) => player >= 0 && player <= 1 ? _towerRecentAttackMs[player] : 0;
    public int GetLastTickBounty(int player) => player >= 0 && player <= 1 ? _lastTickBounty[player] : 0;
    public int GetTowerAttackDamage(int player) => (player >= 0 && player <= 1) ? _towerAttackDamage[player] : 0;
    public int GetTowerAttackRange(int player) => (player >= 0 && player <= 1) ? _towerAttackRange[player] : 0;
    public int GetTowerSplashRadius(int player) => (player >= 0 && player <= 1) ? _towerSplashRadius[player] : 0;
    public void AddTowerAttackDamage(int player, int delta) { if (player >= 0 && player <= 1) _towerAttackDamage[player] += delta; }
    public void AddTowerAttackRange(int player, int delta) { if (player >= 0 && player <= 1) _towerAttackRange[player] += delta; }
    public void AddTowerSplashRadius(int player, int delta) { if (player >= 0 && player <= 1) _towerSplashRadius[player] += delta; }

    /// <summary>
    /// Spawn a unit from a UnitSpawnRequest. Returns the assigned unit ID.
    /// </summary>
    public int SpawnUnit(UnitSpawnRequest req)
    {
        int id = _nextUnitId++;
        bool hasSourceBuilding = req.SourceGridWidth > 0 && req.SourceGridHeight > 0;
        UnitState unit = new()
        {
            UnitId = id,
            OwnerPlayer = req.OwnerPlayer,
            Hp = req.Hp,
            MaxHp = req.Hp,
            Damage = req.Damage,
            AttackCooldownMs = req.AttackCooldownMs,
            AttackTimerMs = 0,
            MoveSpeed = req.MoveSpeed,
            Range = req.Range,
            ArmorType = req.ArmorType,
            DamageType = req.DamageType,
            SplashRadius = req.SplashRadius,
            Bounty = req.Bounty,
            TowerDamageMultiplierPct = req.TowerDamageMultiplierPct > 0 ? req.TowerDamageMultiplierPct : 100,
            PositionX = req.StartPositionX,
            Direction = req.Direction,
            AgeMs = 0,
            SpawnGridX = hasSourceBuilding ? req.SourceGridX : -1,
            SpawnGridY = hasSourceBuilding ? req.SourceGridY : -1,
            SpawnGridWidth = hasSourceBuilding ? req.SourceGridWidth : 0,
            SpawnGridHeight = hasSourceBuilding ? req.SourceGridHeight : 0,
            TargetUnitId = -1,
            RecentAttackMs = 0,
            SpritePath = req.SpritePath,
            IsAlive = true
        };
        _units.Add(unit);
        return id;
    }

    /// <summary>
    /// Execute one simulation tick. Returns any tower damage that occurred.
    /// Steps: Move -> Acquire targets -> Attack -> Tower attack -> Mark-and-sweep removal.
    /// </summary>
    public List<TowerDamage> Tick(int tickMs)
    {
        _lastTickBounty[0] = 0;
        _lastTickBounty[1] = 0;

        // ── Step 1: Move ──
        // Units with no target move along the lane.
        for (int i = 0; i < _units.Count; i++)
        {
            UnitState unit = _units[i];
            if (!unit.IsAlive)
                continue;

            unit.AgeMs += tickMs;

            if (unit.RecentAttackMs > 0)
            {
                unit.RecentAttackMs -= tickMs;
                if (unit.RecentAttackMs < 0)
                    unit.RecentAttackMs = 0;
            }

            if (unit.TargetUnitId == -1)
            {
                int delta = unit.MoveSpeed * tickMs / 1000;
                unit.PositionX += delta * unit.Direction;
                if (unit.PositionX < 0)
                    unit.PositionX = 0;
                if (unit.PositionX > _laneLengthUnits)
                    unit.PositionX = _laneLengthUnits;
            }
        }

        for (int player = 0; player < 2; player++)
        {
            if (_towerRecentAttackMs[player] > 0)
            {
                _towerRecentAttackMs[player] -= tickMs;
                if (_towerRecentAttackMs[player] < 0)
                    _towerRecentAttackMs[player] = 0;
            }
        }

        // ── Step 2: Acquire targets ──
        // For each unit without a valid target, find the nearest enemy within range.
        for (int i = 0; i < _units.Count; i++)
        {
            UnitState unit = _units[i];
            if (!unit.IsAlive)
                continue;

            // Validate existing target: must still be alive and in range
            if (unit.TargetUnitId != -1)
            {
                UnitState? target = FindUnitById(unit.TargetUnitId);
                if (target == null || !target.IsAlive)
                {
                    unit.TargetUnitId = -1;
                }
                else
                {
                    int dist = Math.Abs(unit.PositionX - target.PositionX);
                    int effectiveRange = unit.Range > 0 ? unit.Range : MeleeRange;
                    if (dist > effectiveRange)
                    {
                        unit.TargetUnitId = -1;
                    }
                }
            }

            // If still no target, search for one
            if (unit.TargetUnitId == -1)
            {
                int effectiveRange = unit.Range > 0 ? unit.Range : MeleeRange;
                int bestDist = int.MaxValue;
                int bestId = -1;

                for (int j = 0; j < _units.Count; j++)
                {
                    UnitState candidate = _units[j];
                    if (!candidate.IsAlive)
                        continue;
                    if (candidate.OwnerPlayer == unit.OwnerPlayer)
                        continue;

                    int dist = Math.Abs(unit.PositionX - candidate.PositionX);
                    if (dist <= effectiveRange && dist < bestDist)
                    {
                        bestDist = dist;
                        bestId = candidate.UnitId;
                    }
                }

                unit.TargetUnitId = bestId;
            }
        }

        // ── Step 3: Attack ──
        for (int i = 0; i < _units.Count; i++)
        {
            UnitState unit = _units[i];
            if (!unit.IsAlive)
                continue;

            if (unit.TargetUnitId == -1)
                continue;

            // Decrement attack timer
            unit.AttackTimerMs -= tickMs;

            if (unit.AttackTimerMs <= 0)
            {
                UnitState? target = FindUnitById(unit.TargetUnitId);
                if (target != null && target.IsAlive)
                {
                    int damage = CombatResolver.CalculateDamage(
                        unit.Damage, unit.DamageType, target.ArmorType);
                    target.Hp -= damage;

                    if (target.Hp <= 0)
                    {
                        target.IsAlive = false;
                        _lastTickBounty[unit.OwnerPlayer] += target.Bounty;
                    }

                    if (unit.SplashRadius > 0)
                        ApplySplashDamage(unit, target, unit.SplashRadius);
                }

                // Reset the cooldown timer
                unit.AttackTimerMs = unit.AttackCooldownMs;
                unit.RecentAttackMs = 120;
            }
        }

        // ── Step 4: Tower takes damage from units at lane endpoints ──
        List<TowerDamage> towerHits = new();
        List<int> removeIds = new();

        for (int i = 0; i < _units.Count; i++)
        {
            UnitState unit = _units[i];
            if (!unit.IsAlive)
            {
                removeIds.Add(unit.UnitId);
                continue;
            }

            bool atEnemyTower =
                (unit.Direction > 0 && unit.PositionX >= _laneLengthUnits) ||
                (unit.Direction < 0 && unit.PositionX <= 0);

            if (!atEnemyTower || unit.TargetUnitId != -1)
                continue;

            unit.AttackTimerMs -= tickMs;
            if (unit.AttackTimerMs > 0)
                continue;

            int targetPlayer = unit.Direction > 0 ? 1 : 0;
            int towerDamage = unit.Damage * unit.TowerDamageMultiplierPct / 100;
            if (towerDamage < 1)
                towerDamage = 1;

            towerHits.Add(new TowerDamage { Player = targetPlayer, Damage = towerDamage });
            unit.AttackTimerMs = unit.AttackCooldownMs;
            unit.RecentAttackMs = 120;
        }

        // ── Step 5: Towers retaliate against nearby enemy units ──
        ProcessTowerAttack(0, 1, 0, tickMs);
        ProcessTowerAttack(1, 0, _laneLengthUnits, tickMs);

        // ── Step 6: Mark-and-sweep removal ──
        // Collect dead units (from combat) that weren't already added
        for (int i = 0; i < _units.Count; i++)
        {
            if (!_units[i].IsAlive)
            {
                int id = _units[i].UnitId;
                bool alreadyMarked = false;
                for (int j = 0; j < removeIds.Count; j++)
                {
                    if (removeIds[j] == id)
                    {
                        alreadyMarked = true;
                        break;
                    }
                }
                if (!alreadyMarked)
                    removeIds.Add(id);
            }
        }

        // Sweep: remove from back to front to preserve indices
        for (int r = 0; r < removeIds.Count; r++)
        {
            int removeId = removeIds[r];
            for (int i = _units.Count - 1; i >= 0; i--)
            {
                if (_units[i].UnitId == removeId)
                {
                    _units.RemoveAt(i);
                    break;
                }
            }
        }

        return towerHits;
    }

    private void ProcessTowerAttack(int towerOwner, int enemyOwner, int towerPosition, int tickMs)
    {
        _towerTargetUnitId[towerOwner] = -1;

        if (_towerAttackDamage[towerOwner] <= 0 || _towerAttackCooldownMs[towerOwner] <= 0 || _towerAttackRange[towerOwner] <= 0)
            return;

        if (_towerAttackTimerMs[towerOwner] > 0)
        {
            _towerAttackTimerMs[towerOwner] -= tickMs;
            if (_towerAttackTimerMs[towerOwner] < 0)
                _towerAttackTimerMs[towerOwner] = 0;
        }

        UnitState? target = null;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < _units.Count; i++)
        {
            UnitState candidate = _units[i];
            if (!candidate.IsAlive || candidate.OwnerPlayer != enemyOwner)
                continue;

            int dist = Math.Abs(candidate.PositionX - towerPosition);
            if (dist > _towerAttackRange[towerOwner])
                continue;

            if (dist < bestDistance)
            {
                bestDistance = dist;
                target = candidate;
            }
        }

        if (target == null)
            return;

        _towerTargetUnitId[towerOwner] = target.UnitId;

        if (_towerAttackTimerMs[towerOwner] > 0)
            return;

        target.Hp -= _towerAttackDamage[towerOwner];
        if (target.Hp <= 0)
        {
            target.IsAlive = false;
            _lastTickBounty[towerOwner] += target.Bounty;
        }

        if (_towerSplashRadius[towerOwner] > 0)
            ApplyTowerSplash(towerOwner, enemyOwner, target, _towerSplashRadius[towerOwner]);

        _towerAttackTimerMs[towerOwner] = _towerAttackCooldownMs[towerOwner];
        _towerRecentAttackMs[towerOwner] = 120;
    }

    /// <summary>
    /// Apply splash damage to all living enemy units within <paramref name="radius"/>
    /// of the primary target's position, excluding the primary target itself.
    /// </summary>
    private void ApplySplashDamage(UnitState attacker, UnitState primaryTarget, int radius)
    {
        for (int j = 0; j < _units.Count; j++)
        {
            UnitState other = _units[j];
            if (!other.IsAlive)
                continue;
            if (other.OwnerPlayer == attacker.OwnerPlayer)
                continue;
            if (other.UnitId == primaryTarget.UnitId)
                continue;

            int dist = Math.Abs(other.PositionX - primaryTarget.PositionX);
            if (dist > radius)
                continue;

            int damage = CombatResolver.CalculateDamage(attacker.Damage, attacker.DamageType, other.ArmorType);
            other.Hp -= damage;
            if (other.Hp <= 0)
            {
                other.IsAlive = false;
                _lastTickBounty[attacker.OwnerPlayer] += other.Bounty;
            }
        }
    }

    /// <summary>
    /// Tower splash: damage living enemy units within <paramref name="radius"/> of the tower's
    /// primary target (excluding the primary), using the tower's flat attack damage.
    /// </summary>
    private void ApplyTowerSplash(int towerOwner, int enemyOwner, UnitState primaryTarget, int radius)
    {
        int damage = _towerAttackDamage[towerOwner];
        for (int i = 0; i < _units.Count; i++)
        {
            UnitState other = _units[i];
            if (!other.IsAlive || other.OwnerPlayer != enemyOwner)
                continue;
            if (other.UnitId == primaryTarget.UnitId)
                continue;
            if (Math.Abs(other.PositionX - primaryTarget.PositionX) > radius)
                continue;

            other.Hp -= damage;
            if (other.Hp <= 0)
            {
                other.IsAlive = false;
                _lastTickBounty[towerOwner] += other.Bounty;
            }
        }
    }

    /// <summary>Find a unit by ID using ordered linear search.</summary>
    private UnitState? FindUnitById(int unitId)
    {
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i].UnitId == unitId)
                return _units[i];
        }
        return null;
    }
}
