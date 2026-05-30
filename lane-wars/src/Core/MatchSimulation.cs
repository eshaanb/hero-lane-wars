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
    public string BuildingName;
    public string[] RequiredBuildingNames;
    public string[] UnlocksBuildingNames;
    public int GoldCost;
    public int BuildDelayMs;
    public int GridWidth;
    public int GridHeight;
    public StrategicRole StrategicRole;
    public CompositionHint CompositionHint;
    public bool IsEconomyBuilding;
    public int IncomeBonus;
    public int SupportDamageBonus;
    public string BuildingSpritePath;
    // Unit stats (only meaningful for production buildings)
    public int UnitHp;
    public int UnitDamage;
    public int UnitAttackCooldownMs;
    public int UnitMoveSpeed;
    public int UnitRange;
    public int UnitArmorType;
    public int UnitDamageType;
    public int SpawnTimeMs;
    public string UnitSpritePath;
    public int UnitTowerDamageMultiplierPct;
    public int UnitSplashRadius;
    public int Bounty;
}

internal struct SpendingEvent
{
    public int Player;
    public StrategicRole Role;
    public int Cost;
    public int Tick;
    public CompositionHint CompositionHint;
    public int SourceId;
}

public struct BuildingRuntimeInfo
{
    public int BuildingId;
    public string BuildingName;
    public string SpritePath;
    public int BuildDelayRemainingMs;
    public int BuildDelayTotalMs;
    public int GridX;
    public int GridY;
    public int GridWidth;
    public int GridHeight;
    public int SpawnTimeMs;
    public int SpawnAccumulatorMs;
    public bool IsEconomyBuilding;

    public bool IsUnderConstruction => BuildDelayTotalMs > 0 && BuildDelayRemainingMs > 0;

    public float ConstructionProgress
    {
        get
        {
            if (BuildDelayTotalMs <= 0)
                return 1.0f;

            float elapsed = BuildDelayTotalMs - BuildDelayRemainingMs;
            return elapsed <= 0.0f ? 0.0f : elapsed / BuildDelayTotalMs;
        }
    }
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
    private readonly int[] _towerHp = new int[2];
    private readonly int[] _startingTowerHp = new int[2];
    private readonly TowerBranch[] _towerBranch = new TowerBranch[2];
    private readonly int[] _towerUpgradeLevel = new int[2];
    private readonly Dictionary<int, string>[] _buildingSprites = new Dictionary<int, string>[2];
    private readonly Dictionary<string, int>[] _buildingCountsByName = new Dictionary<string, int>[2];
    private readonly List<SpendingEvent>[] _spendingHistory = new List<SpendingEvent>[2];
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
    public int GetTowerHp(int player) => _towerHp[player];
    public int GetStartingTowerHp(int player) => _startingTowerHp[player];
    public TowerBranch GetTowerBranch(int player) => (player >= 0 && player <= 1) ? _towerBranch[player] : TowerBranch.None;
    public int GetTowerUpgradeLevel(int player) => (player >= 0 && player <= 1) ? _towerUpgradeLevel[player] : 0;
    public int GetEconomyBuildingCount(int player) => _production[player].GetEconomyBuildingCount();
    public int GetProductionBuildingCount(int player) => _production[player].GetProductionBuildingCount();
    public int GetIncomePerTick(int player) => _economy[player].CalculateIncome(_production[player].GetTotalIncomeBonus());
    public int GetSupportDamageBonus(int player) => _production[player].GetTotalSupportDamageBonus();
    public int IncomeAccumulatorMs => _incomeAccumulatorMs;
    public int IncomeTickMs => _incomeTickMs;
    public bool IsCellOccupied(int player, int x, int y) => _buildZones[player].GetCell(x, y) != null;
    public bool CanPlaceBuilding(int player, int x, int y, int width, int height) => _buildZones[player].CanPlace(x, y, width, height);
    public int GetTowerTargetUnitId(int player) => _lane.GetTowerTargetUnitId(player);
    public int GetTowerRecentAttackMs(int player) => _lane.GetTowerRecentAttackMs(player);
    public string GetBuildingSpritePath(int player, int x, int y)
    {
        int? buildingId = _buildZones[player].GetCell(x, y);
        if (!buildingId.HasValue)
            return "";
        return _buildingSprites[player].TryGetValue(buildingId.Value, out var spritePath) ? spritePath : "";
    }
    public bool TryGetBuildingAt(int player, int x, int y, out BuildingRuntimeInfo info)
    {
        info = default;
        if (player < 0 || player > 1)
            return false;

        int? buildingId = _buildZones[player].GetCell(x, y);
        if (!buildingId.HasValue)
            return false;

        if (!_production[player].TryGetBuilding(buildingId.Value, out var building))
            return false;

        _buildingSprites[player].TryGetValue(buildingId.Value, out var spritePath);
        info = new BuildingRuntimeInfo
        {
            BuildingId = buildingId.Value,
            BuildingName = building.BuildingName,
            SpritePath = spritePath ?? "",
            BuildDelayRemainingMs = building.BuildDelayRemainingMs,
            BuildDelayTotalMs = building.BuildDelayTotalMs,
            GridX = building.GridX,
            GridY = building.GridY,
            GridWidth = building.GridWidth,
            GridHeight = building.GridHeight,
            SpawnTimeMs = building.SpawnTimeMs,
            SpawnAccumulatorMs = building.AccumulatorMs,
            IsEconomyBuilding = building.IsEconomyBuilding
        };
        return true;
    }

    public int GetBuildingCountByName(int player, string buildingName)
    {
        if (string.IsNullOrEmpty(buildingName))
            return 0;
        return _buildingCountsByName[player].TryGetValue(buildingName, out var count) ? count : 0;
    }
    public bool MeetsRequirements(int player, string[]? requiredBuildingNames)
    {
        if (requiredBuildingNames == null || requiredBuildingNames.Length == 0)
            return true;
        for (int i = 0; i < requiredBuildingNames.Length; i++)
        {
            string requiredName = requiredBuildingNames[i];
            if (!string.IsNullOrEmpty(requiredName) && GetBuildingCountByName(player, requiredName) <= 0)
                return false;
        }
        return true;
    }
    public string GetBuildingSummary(int player)
    {
        if (_buildingCountsByName[player].Count == 0)
            return "None";

        List<string> parts = new();
        foreach (var kvp in _buildingCountsByName[player])
            parts.Add($"{kvp.Key} x{kvp.Value}");
        parts.Sort();
        return string.Join(", ", parts);
    }
    public ScoutRead GetScoutRead(int targetPlayer)
    {
        int[] roleScores = CalculateWeightedRoleScores(targetPlayer, out int[] hintScores, out _);
        return new ScoutRead
        {
            Economy = ToSignalLevel(roleScores[(int)StrategicRole.Economy]),
            Pressure = ToSignalLevel(roleScores[(int)StrategicRole.Pressure]),
            Defense = ToSignalLevel(roleScores[(int)StrategicRole.Defense]),
            Tech = ToSignalLevel(roleScores[(int)StrategicRole.Tech]),
            CompositionHint = ChooseCompositionHint(hintScores)
        };
    }
    public RecentSpendingSummary GetRecentSpendingSummary(int player)
    {
        int[] roleScores = CalculateWeightedRoleScores(player, out int[] hintScores, out int eventCount);
        return new RecentSpendingSummary
        {
            Economy = ToSignalLevel(roleScores[(int)StrategicRole.Economy]),
            Pressure = ToSignalLevel(roleScores[(int)StrategicRole.Pressure]),
            Defense = ToSignalLevel(roleScores[(int)StrategicRole.Defense]),
            Tech = ToSignalLevel(roleScores[(int)StrategicRole.Tech]),
            CompositionHint = ChooseCompositionHint(hintScores),
            EventCount = eventCount
        };
    }
    public int GetFirstInvestmentTime(int player, StrategicRole role)
    {
        if (player < 0 || player > 1)
            return -1;

        for (int i = 0; i < _spendingHistory[player].Count; i++)
        {
            var ev = _spendingHistory[player][i];
            if (ev.Role == role)
                return ev.Tick * _simTick.TickIntervalMs;
        }

        return -1;
    }

    /// <summary>
    /// Create a new match simulation with the given configuration values.
    /// All parameters mirror GameConfig fields so this class stays Godot-free.
    /// </summary>
    public MatchSimulation(
        int startingGold,
        int baseIncomePerTick,
        int incomeTickMs,
        int towerHp,
        int buildZoneWidth,
        int buildZoneHeight,
        int simTickMs,
        int laneLengthUnits,
        int towerAttackDamage = 0,
        int towerAttackCooldownMs = 0,
        int towerAttackRange = 0,
        int[]? towerHpByPlayer = null,
        int[]? towerAttackDamageByPlayer = null,
        int[]? towerAttackCooldownMsByPlayer = null,
        int[]? towerAttackRangeByPlayer = null)
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
            _startingTowerHp[p] = towerHpByPlayer != null && p < towerHpByPlayer.Length ? towerHpByPlayer[p] : towerHp;
            _towerHp[p] = _startingTowerHp[p];
            _buildingSprites[p] = new Dictionary<int, string>();
            _buildingCountsByName[p] = new Dictionary<string, int>();
            _spendingHistory[p] = new List<SpendingEvent>();
        }

        _lane = new LaneSimulation(
            laneLengthUnits,
            towerAttackDamage,
            towerAttackCooldownMs,
            towerAttackRange,
            towerAttackDamageByPlayer,
            towerAttackCooldownMsByPlayer,
            towerAttackRangeByPlayer);
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

        if (!MeetsRequirements(player, info.RequiredBuildingNames))
            return false;

        // Check grid space
        if (!_buildZones[player].CanPlace(x, y, info.GridWidth, info.GridHeight))
            return false;

        int buildingId = _nextBuildingId++;

        // Place on grid
        _buildZones[player].Place(x, y, info.GridWidth, info.GridHeight, buildingId);
        _buildingSprites[player][buildingId] = info.BuildingSpritePath ?? "";
        if (!string.IsNullOrEmpty(info.BuildingName))
        {
            _buildingCountsByName[player].TryGetValue(info.BuildingName, out int currentCount);
            _buildingCountsByName[player][info.BuildingName] = currentCount + 1;
        }

        // Spend gold
        _economy[player].TrySpend(info.GoldCost);
        RecordSpending(player, buildingId, info);

        // Register with production manager
        PlacedBuilding placed = new()
        {
            BuildingId = buildingId,
            BuildingName = info.BuildingName,
            SpawnTimeMs = info.SpawnTimeMs,
            AccumulatorMs = 0,
            BuildDelayRemainingMs = info.BuildDelayMs,
            BuildDelayTotalMs = info.BuildDelayMs,
            GridX = x,
            GridY = y,
            GridWidth = info.GridWidth,
            GridHeight = info.GridHeight,
            UnitHp = info.UnitHp,
            UnitDamage = info.UnitDamage,
            UnitAttackCooldownMs = info.UnitAttackCooldownMs,
            UnitMoveSpeed = info.UnitMoveSpeed,
            UnitRange = info.UnitRange,
            UnitArmorType = info.UnitArmorType,
            UnitDamageType = info.UnitDamageType,
            IsEconomyBuilding = info.IsEconomyBuilding,
            IncomeBonus = info.IncomeBonus,
            SupportDamageBonus = info.SupportDamageBonus,
            StrategicRole = info.StrategicRole,
            CompositionHint = info.CompositionHint,
            UnitSpritePath = info.UnitSpritePath,
            UnitTowerDamageMultiplierPct = info.UnitTowerDamageMultiplierPct,
            UnitSplashRadius = info.UnitSplashRadius,
            Bounty = info.Bounty
        };
        _production[player].AddBuilding(placed);

        return true;
    }

    /// <summary>
    /// Attempt to upgrade the player's tower along <paramref name="branch"/>.
    /// Exclusive specialization: once a branch is chosen it cannot be switched.
    /// Returns true if a level was purchased.
    /// </summary>
    public bool UpgradeTower(int player, TowerBranch branch)
    {
        if (player < 0 || player > 1 || branch == TowerBranch.None)
            return false;
        if (_towerBranch[player] != TowerBranch.None && _towerBranch[player] != branch)
            return false;

        int nextLevel = _towerUpgradeLevel[player] + 1;
        TowerUpgradeEffect? effect = TowerUpgrades.GetEffect(branch, nextLevel);
        if (effect == null)
            return false;

        if (!_economy[player].TrySpend(effect.Value.Cost))
            return false;

        _towerBranch[player] = branch;
        _towerUpgradeLevel[player] = nextLevel;
        ApplyTowerUpgradeEffect(player, effect.Value);
        return true;
    }

    /// <summary>Cost of the player's next level in <paramref name="branch"/>, or -1 if unavailable.</summary>
    public int GetTowerUpgradeCost(int player, TowerBranch branch)
    {
        if (player < 0 || player > 1 || branch == TowerBranch.None)
            return -1;
        if (_towerBranch[player] != TowerBranch.None && _towerBranch[player] != branch)
            return -1;
        TowerUpgradeEffect? effect = TowerUpgrades.GetEffect(branch, _towerUpgradeLevel[player] + 1);
        return effect?.Cost ?? -1;
    }

    public int GetTowerAttackDamage(int player) => _lane.GetTowerAttackDamage(player);
    public int GetTowerAttackRange(int player) => _lane.GetTowerAttackRange(player);
    public int GetTowerSplashRadius(int player) => _lane.GetTowerSplashRadius(player);

    private void ApplyTowerUpgradeEffect(int player, TowerUpgradeEffect effect)
    {
        if (effect.DamageDelta != 0)
            _lane.AddTowerAttackDamage(player, effect.DamageDelta);
        if (effect.RangeDelta != 0)
            _lane.AddTowerAttackRange(player, effect.RangeDelta);
        if (effect.SplashDelta != 0)
            _lane.AddTowerSplashRadius(player, effect.SplashDelta);
        if (effect.MaxHpDelta != 0)
        {
            _startingTowerHp[player] += effect.MaxHpDelta;
            _towerHp[player] += effect.MaxHpDelta;
        }
    }

    /// <summary>
    /// Process one simulation tick. This is the main loop entry point.
    /// Steps: advance tick -> income -> production -> spawn -> lane tick -> tower damage -> win check.
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
                int income = _economy[p].CalculateIncome(_production[p].GetTotalIncomeBonus());
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
        List<TowerDamage> towerHits = _lane.Tick(tickMs);

        // 6. Apply damage to tower HP
        for (int i = 0; i < towerHits.Count; i++)
        {
            _towerHp[towerHits[i].Player] -= towerHits[i].Damage;
            if (_towerHp[towerHits[i].Player] < 0)
                _towerHp[towerHits[i].Player] = 0;
        }

        // 7. Credit kill bounties earned this tick.
        for (int p = 0; p < 2; p++)
            _economy[p].AddGold(_lane.GetLastTickBounty(p));
    }

    /// <summary>Check if the match has ended (either tower HP reaches 0).</summary>
    public bool IsMatchOver()
    {
        return _towerHp[0] <= 0 || _towerHp[1] <= 0;
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
        if (_towerHp[0] <= 0 && _towerHp[1] <= 0)
            return 0;

        return _towerHp[0] <= 0 ? 1 : 0;
    }

    private void RecordSpending(int player, int buildingId, BuildingInfo info)
    {
        _spendingHistory[player].Add(new SpendingEvent
        {
            Player = player,
            Role = info.StrategicRole,
            Cost = info.GoldCost,
            Tick = _simTick.CurrentTick,
            CompositionHint = info.CompositionHint,
            SourceId = buildingId
        });
    }

    private int[] CalculateWeightedRoleScores(int player, out int[] hintScores, out int eventCount)
    {
        int[] roleScores = new int[4];
        hintScores = new int[5];
        eventCount = 0;

        if (player < 0 || player > 1)
            return roleScores;

        int currentMs = _simTick.ElapsedMs;
        const int fullWeightWindowMs = 45000;
        const int halfWeightWindowMs = 90000;

        for (int i = 0; i < _spendingHistory[player].Count; i++)
        {
            SpendingEvent ev = _spendingHistory[player][i];
            int ageMs = currentMs - ev.Tick * _simTick.TickIntervalMs;
            if (ageMs < 0 || ageMs > halfWeightWindowMs)
                continue;

            int weight = ageMs <= fullWeightWindowMs ? 2 : 1;
            int weightedCost = ev.Cost * weight;
            roleScores[(int)ev.Role] += weightedCost;
            eventCount++;

            if (ev.CompositionHint != CompositionHint.Unknown)
                hintScores[(int)ev.CompositionHint] += weightedCost;
        }

        return roleScores;
    }

    private static ScoutSignalLevel ToSignalLevel(int weightedScore)
    {
        if (weightedScore >= 140)
            return ScoutSignalLevel.High;
        if (weightedScore >= 40)
            return ScoutSignalLevel.Medium;
        return ScoutSignalLevel.Low;
    }

    private static CompositionHint ChooseCompositionHint(int[] hintScores)
    {
        int bestIndex = (int)CompositionHint.Unknown;
        int bestScore = 0;
        int secondScore = 0;

        for (int i = 1; i < hintScores.Length; i++)
        {
            int score = hintScores[i];
            if (score > bestScore)
            {
                secondScore = bestScore;
                bestScore = score;
                bestIndex = i;
            }
            else if (score > secondScore)
            {
                secondScore = score;
            }
        }

        if (bestScore == 0)
            return CompositionHint.Unknown;
        if (secondScore > 0 && bestScore - secondScore <= bestScore / 3)
            return CompositionHint.Mixed;
        return (CompositionHint)bestIndex;
    }
}
