namespace LaneWars.Core;

using System.Collections.Generic;
using LaneWars.Buildings;
using LaneWars.Units;

/// <summary>
/// Plain struct carrying building info from Godot Resource into the pure simulation.
/// This decouples MatchSimulation from any Godot Resource types.
/// </summary>
public struct BuildingInfo
{
    public int GoldCost;
    public int GridWidth;
    public int GridHeight;
    public bool IsEconomyBuilding;
    public int IncomeBonus;
    // Unit stats (only meaningful for production buildings)
    public int UnitHp;
    public int UnitDamage;
    public int UnitAttackCooldownMs;
    public int UnitMoveSpeed;
    public int UnitRange;
    public int UnitArmorType;
    public int UnitDamageType;
    public int SpawnTimeMs;
}

/// <summary>
/// Top-level match orchestrator. Owns all simulation state for a single match.
/// Pure C#, no Godot dependencies. Integer math only.
/// </summary>
public class MatchSimulation
{
    private readonly SimTick _simTick;
    private readonly EconomyManager[] _economy = new EconomyManager[2];
    private readonly BuildZone[] _buildZones = new BuildZone[2];
    private readonly ProductionManager[] _production = new ProductionManager[2];
    private readonly LaneSimulation _lane;
    private readonly int[] _baseHp = new int[2];
    private int _nextBuildingId = 0;

    // Config values cached from constructor
    private readonly int _incomeTickMs;
    private readonly int _laneLengthUnits;

    // Track accumulated time for income ticks
    private int _incomeAccumulatorMs;

    // ── Public read-only accessors ──

    public SimTick SimTick => _simTick;
    public IReadOnlyList<UnitState> Units => _lane.Units;
    public int LaneLengthUnits => _laneLengthUnits;

    public int GetGold(int player) => _economy[player].Gold;
    public int GetBaseHp(int player) => _baseHp[player];
    public int GetEconomyBuildingCount(int player) => _production[player].GetEconomyBuildingCount();
    public int GetProductionBuildingCount(int player) => _production[player].GetProductionBuildingCount();

    /// <summary>
    /// Create a new match simulation with the given configuration values.
    /// All parameters mirror GameConfig fields so this class stays Godot-free.
    /// </summary>
    public MatchSimulation(
        int startingGold,
        int baseIncomePerTick,
        int incomeTickMs,
        int baseHpPerLane,
        int buildZoneWidth,
        int buildZoneHeight,
        int simTickMs,
        int laneLengthUnits)
    {
        _simTick = new SimTick(simTickMs);
        _incomeTickMs = incomeTickMs;
        _laneLengthUnits = laneLengthUnits;
        _incomeAccumulatorMs = 0;

        for (int p = 0; p < 2; p++)
        {
            _economy[p] = new EconomyManager(startingGold, baseIncomePerTick);
            _buildZones[p] = new BuildZone(buildZoneWidth, buildZoneHeight);
            _production[p] = new ProductionManager();
            _baseHp[p] = baseHpPerLane;
        }

        _lane = new LaneSimulation(laneLengthUnits);
    }

    /// <summary>
    /// Attempt to place a building for the given player.
    /// Validates gold, grid space, then places and registers the building.
    /// Returns true on success.
    /// </summary>
    public bool PlaceBuilding(int player, int x, int y, BuildingInfo info)
    {
        if (player < 0 || player > 1)
            return false;

        // Check gold
        if (_economy[player].Gold < info.GoldCost)
            return false;

        // Check grid space
        if (!_buildZones[player].CanPlace(x, y, info.GridWidth, info.GridHeight))
            return false;

        int buildingId = _nextBuildingId++;

        // Place on grid
        _buildZones[player].Place(x, y, info.GridWidth, info.GridHeight, buildingId);

        // Spend gold
        _economy[player].TrySpend(info.GoldCost);

        // Register with production manager
        PlacedBuilding placed = new()
        {
            BuildingId = buildingId,
            SpawnTimeMs = info.SpawnTimeMs,
            AccumulatorMs = 0,
            UnitHp = info.UnitHp,
            UnitDamage = info.UnitDamage,
            UnitAttackCooldownMs = info.UnitAttackCooldownMs,
            UnitMoveSpeed = info.UnitMoveSpeed,
            UnitRange = info.UnitRange,
            UnitArmorType = info.UnitArmorType,
            UnitDamageType = info.UnitDamageType,
            IsEconomyBuilding = info.IsEconomyBuilding
        };
        _production[player].AddBuilding(placed);

        return true;
    }

    /// <summary>
    /// Process one simulation tick. This is the main loop entry point.
    /// Steps: advance tick -> income -> production -> spawn -> lane tick -> leak -> win check.
    /// </summary>
    public void ProcessTick()
    {
        // 1. Advance tick counter
        _simTick.Advance();
        int tickMs = _simTick.TickIntervalMs;

        // 2. Income tick: check if enough time has accumulated
        _incomeAccumulatorMs += tickMs;
        if (_incomeAccumulatorMs >= _incomeTickMs)
        {
            _incomeAccumulatorMs -= _incomeTickMs;

            for (int p = 0; p < 2; p++)
            {
                int income = _economy[p].CalculateIncome(_production[p].GetEconomyBuildingCount());
                _economy[p].AddGold(income);
            }
        }

        // 3. Tick production managers and collect spawn requests
        // Player 0: direction +1, starts at position 0
        // Player 1: direction -1, starts at laneLengthUnits
        List<UnitSpawnRequest> spawns0 = _production[0].Tick(tickMs, 0, 1, 0);
        List<UnitSpawnRequest> spawns1 = _production[1].Tick(tickMs, 1, -1, _laneLengthUnits);

        // 4. Spawn units into the lane
        for (int i = 0; i < spawns0.Count; i++)
            _lane.SpawnUnit(spawns0[i]);

        for (int i = 0; i < spawns1.Count; i++)
            _lane.SpawnUnit(spawns1[i]);

        // 5. Tick the lane simulation
        List<LeakDamage> leaks = _lane.Tick(tickMs);

        // 6. Apply leak damage to base HP
        for (int i = 0; i < leaks.Count; i++)
        {
            _baseHp[leaks[i].Player] -= leaks[i].Damage;
            if (_baseHp[leaks[i].Player] < 0)
                _baseHp[leaks[i].Player] = 0;
        }
    }

    /// <summary>Check if the match has ended (either base HP reaches 0).</summary>
    public bool IsMatchOver()
    {
        return _baseHp[0] <= 0 || _baseHp[1] <= 0;
    }

    /// <summary>
    /// Get the winner. Returns 0 or 1 for the winning player, -1 if match is not over.
    /// If both bases hit 0 simultaneously, player 0 wins (first-indexed advantage).
    /// </summary>
    public int GetWinner()
    {
        if (!IsMatchOver())
            return -1;

        // If both are dead, player 0 wins by convention
        if (_baseHp[0] <= 0 && _baseHp[1] <= 0)
            return 0;

        return _baseHp[0] <= 0 ? 1 : 0;
    }
}
