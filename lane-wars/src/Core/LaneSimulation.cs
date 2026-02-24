namespace LaneWars.Core;

using System;
using System.Collections.Generic;
using LaneWars.Units;
using LaneWars.Buildings;

public struct LeakDamage
{
    public int Player;  // which player's base takes damage
    public int Damage;
}

/// <summary>
/// Heart of the game: owns all units in a single lane and runs the
/// move -> acquire -> attack -> leak -> sweep pipeline each tick.
/// Pure C#, no Godot dependencies. Integer math only.
/// </summary>
public class LaneSimulation
{
    private readonly List<UnitState> _units = new();
    private int _nextUnitId = 0;
    private readonly int _laneLengthUnits;
    private const int MeleeRange = 50;

    public IReadOnlyList<UnitState> Units => _units;
    public int LaneLengthUnits => _laneLengthUnits;

    public LaneSimulation(int laneLengthUnits)
    {
        _laneLengthUnits = laneLengthUnits;
    }

    /// <summary>
    /// Spawn a unit from a UnitSpawnRequest. Returns the assigned unit ID.
    /// </summary>
    public int SpawnUnit(UnitSpawnRequest req)
    {
        int id = _nextUnitId++;
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
            PositionX = req.StartPositionX,
            Direction = req.Direction,
            TargetUnitId = -1,
            IsAlive = true
        };
        _units.Add(unit);
        return id;
    }

    /// <summary>
    /// Execute one simulation tick. Returns any leak damage that occurred.
    /// Steps: Move -> Acquire targets -> Attack -> Detect leaks -> Mark-and-sweep removal.
    /// </summary>
    public List<LeakDamage> Tick(int tickMs)
    {
        // ── Step 1: Move ──
        // Units with no target move along the lane.
        for (int i = 0; i < _units.Count; i++)
        {
            UnitState unit = _units[i];
            if (!unit.IsAlive)
                continue;

            if (unit.TargetUnitId == -1)
            {
                int delta = unit.MoveSpeed * tickMs / 1000;
                unit.PositionX += delta * unit.Direction;
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
                    }
                }

                // Reset the cooldown timer
                unit.AttackTimerMs = unit.AttackCooldownMs;
            }
        }

        // ── Step 4: Detect leaks ──
        List<LeakDamage> leaks = new();
        List<int> removeIds = new();

        for (int i = 0; i < _units.Count; i++)
        {
            UnitState unit = _units[i];
            if (!unit.IsAlive)
            {
                removeIds.Add(unit.UnitId);
                continue;
            }

            bool leaked = false;

            // Player 0 units (direction +1) leak at >= laneLengthUnits → damage player 1
            if (unit.Direction > 0 && unit.PositionX >= _laneLengthUnits)
            {
                int dmg = unit.Hp * 100 / unit.MaxHp;
                if (dmg < 1) dmg = 1;
                leaks.Add(new LeakDamage { Player = 1, Damage = dmg });
                leaked = true;
            }
            // Player 1 units (direction -1) leak at <= 0 → damage player 0
            else if (unit.Direction < 0 && unit.PositionX <= 0)
            {
                int dmg = unit.Hp * 100 / unit.MaxHp;
                if (dmg < 1) dmg = 1;
                leaks.Add(new LeakDamage { Player = 0, Damage = dmg });
                leaked = true;
            }

            if (leaked)
            {
                unit.IsAlive = false;
                removeIds.Add(unit.UnitId);
            }
        }

        // ── Step 5: Mark-and-sweep removal ──
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

        return leaks;
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
