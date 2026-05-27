using LaneWars.Buildings;
using LaneWars.Core;

namespace LaneWars.AI;

public enum AiStrategyPlan { TempoRush, EconomyGreed, TechCounterScaling }

public class SimpleAI
{
    private const int DefenseOverrideCooldownTicks = 300;
    private const int DefenseOverrideBudgetPct = 40;

    private readonly SeededRng _rng;
    private readonly int _thinkIntervalTicks;
    private int _lastThinkTick;
    private int _lastDefenseOverrideTick = -DefenseOverrideCooldownTicks;
    private readonly BuildingInfo _coreInfo;
    private readonly BuildingInfo? _rangedInfo;
    private readonly BuildingInfo? _tankInfo;
    private readonly BuildingInfo? _siegeInfo;
    private readonly BuildingInfo? _supportInfo;
    private readonly BuildingInfo? _economyInfo;
    private readonly int _buildZoneWidth;
    private readonly int _buildZoneHeight;

    public AiStrategyPlan Plan { get; }

    public SimpleAI(
        ulong seed,
        BuildingInfo coreInfo,
        BuildingInfo? rangedInfo = null,
        BuildingInfo? tankInfo = null,
        BuildingInfo? siegeInfo = null,
        BuildingInfo? supportInfo = null,
        BuildingInfo? economyInfo = null,
        int buildZoneWidth = 8,
        int buildZoneHeight = 4,
        int thinkIntervalTicks = 20,
        AiStrategyPlan plan = AiStrategyPlan.TempoRush)
    {
        _rng = new SeededRng(seed);
        _coreInfo = coreInfo;
        _rangedInfo = rangedInfo;
        _tankInfo = tankInfo;
        _siegeInfo = siegeInfo;
        _supportInfo = supportInfo;
        _economyInfo = economyInfo;
        _buildZoneWidth = buildZoneWidth;
        _buildZoneHeight = buildZoneHeight;
        _thinkIntervalTicks = thinkIntervalTicks;
        _lastThinkTick = -thinkIntervalTicks;
        Plan = plan;
    }

    public string PlanDisplayName => Plan switch
    {
        AiStrategyPlan.TempoRush => "Tempo/Rush",
        AiStrategyPlan.EconomyGreed => "Economy/Greed",
        AiStrategyPlan.TechCounterScaling => "Tech/Counter-scaling",
        _ => "Unknown"
    };

    public void Think(MatchSimulation sim, int playerIndex)
    {
        int currentTick = sim.SimTick.CurrentTick;
        if (currentTick - _lastThinkTick < _thinkIntervalTicks)
            return;
        _lastThinkTick = currentTick;

        if (TryDefenseOverride(sim, playerIndex))
            return;

        switch (Plan)
        {
            case AiStrategyPlan.EconomyGreed:
                ThinkGreed(sim, playerIndex);
                break;
            case AiStrategyPlan.TechCounterScaling:
                ThinkTech(sim, playerIndex);
                break;
            default:
                ThinkRush(sim, playerIndex);
                break;
        }
    }

    private void ThinkRush(MatchSimulation sim, int playerIndex)
    {
        int currentTick = sim.SimTick.CurrentTick;
        if (!HasBuilding(sim, playerIndex, _coreInfo) && TryPlaceWithPrerequisites(sim, playerIndex, _coreInfo))
            return;

        int earlyPressureCount = Count(sim, playerIndex, _coreInfo);
        if (_rangedInfo.HasValue)
            earlyPressureCount += Count(sim, playerIndex, _rangedInfo.Value);

        if (currentTick < 600)
        {
            if (earlyPressureCount < 2)
            {
                if (_rangedInfo.HasValue && Count(sim, playerIndex, _rangedInfo.Value) == 0 &&
                    TryPlaceWithPrerequisites(sim, playerIndex, _rangedInfo.Value))
                    return;

                if (Count(sim, playerIndex, _coreInfo) < 2 &&
                    TryPlaceWithPrerequisites(sim, playerIndex, _coreInfo))
                    return;
            }

            return;
        }

        if (currentTick > 900 && _economyInfo.HasValue && Count(sim, playerIndex, _economyInfo.Value) == 0 &&
            TryPlaceWithPrerequisites(sim, playerIndex, _economyInfo.Value))
            return;

        if (_siegeInfo.HasValue && currentTick > 700 && TryPlaceWithPrerequisites(sim, playerIndex, _siegeInfo.Value))
            return;

        if (earlyPressureCount < 3)
            TryPlaceWithPrerequisites(sim, playerIndex, _coreInfo);
    }

    private void ThinkGreed(MatchSimulation sim, int playerIndex)
    {
        int currentTick = sim.SimTick.CurrentTick;

        if (_economyInfo.HasValue && Count(sim, playerIndex, _economyInfo.Value) == 0 &&
            TryPlaceWithPrerequisites(sim, playerIndex, _economyInfo.Value))
            return;

        if (!HasBuilding(sim, playerIndex, _coreInfo) && TryPlaceWithPrerequisites(sim, playerIndex, _coreInfo))
            return;

        if (currentTick < 800 && _economyInfo.HasValue && Count(sim, playerIndex, _economyInfo.Value) < 2 &&
            TryPlaceWithPrerequisites(sim, playerIndex, _economyInfo.Value))
            return;

        if (_supportInfo.HasValue && currentTick > 600 && TryPlaceWithPrerequisites(sim, playerIndex, _supportInfo.Value))
            return;

        if (_siegeInfo.HasValue && currentTick > 900 && TryPlaceWithPrerequisites(sim, playerIndex, _siegeInfo.Value))
            return;

        TryPlaceWithPrerequisites(sim, playerIndex, _coreInfo);
    }

    private void ThinkTech(MatchSimulation sim, int playerIndex)
    {
        if (!HasBuilding(sim, playerIndex, _coreInfo) && TryPlaceWithPrerequisites(sim, playerIndex, _coreInfo))
            return;

        BuildingInfo? counter = ChooseCounterTech(sim.GetScoutRead(1 - playerIndex));
        if (counter.HasValue && TryPlaceWithPrerequisites(sim, playerIndex, counter.Value))
            return;

        if (_siegeInfo.HasValue && TryPlaceWithPrerequisites(sim, playerIndex, _siegeInfo.Value))
            return;

        if (_supportInfo.HasValue && TryPlaceWithPrerequisites(sim, playerIndex, _supportInfo.Value))
            return;

        if (_tankInfo.HasValue && TryPlaceWithPrerequisites(sim, playerIndex, _tankInfo.Value))
            return;

        // Tech plans intentionally save after the opening core instead of spending every coin on tempo.
    }

    private BuildingInfo? ChooseCounterTech(ScoutRead enemyRead)
    {
        return enemyRead.CompositionHint switch
        {
            CompositionHint.Swarm => _siegeInfo ?? _supportInfo ?? _tankInfo,
            CompositionHint.Splash => _tankInfo ?? _supportInfo ?? _siegeInfo,
            CompositionHint.Heavy => _rangedInfo ?? _supportInfo ?? _siegeInfo,
            _ => _siegeInfo ?? _supportInfo ?? _tankInfo
        };
    }

    private bool TryDefenseOverride(MatchSimulation sim, int playerIndex)
    {
        int currentTick = sim.SimTick.CurrentTick;
        if (currentTick - _lastDefenseOverrideTick < DefenseOverrideCooldownTicks)
            return false;
        if (!NeedsEmergencyDefense(sim, playerIndex))
            return false;

        int budget = sim.GetGold(playerIndex) * DefenseOverrideBudgetPct / 100;
        if (_tankInfo.HasValue && TryPlaceWithPrerequisites(sim, playerIndex, _tankInfo.Value, budget))
        {
            _lastDefenseOverrideTick = currentTick;
            return true;
        }

        if (!HasBuilding(sim, playerIndex, _coreInfo) && TryPlaceWithPrerequisites(sim, playerIndex, _coreInfo, budget))
        {
            _lastDefenseOverrideTick = currentTick;
            return true;
        }

        return false;
    }

    private static bool NeedsEmergencyDefense(MatchSimulation sim, int playerIndex)
    {
        int enemyIndex = 1 - playerIndex;
        bool leaking = sim.GetTowerHp(playerIndex) <= sim.GetStartingTowerHp(playerIndex) - 20;
        bool meaningfullyBehind = sim.GetTowerHp(playerIndex) + 60 < sim.GetTowerHp(enemyIndex);
        bool clearLanePressure = CountThreateningEnemyUnits(sim, playerIndex) >= 2;
        bool scoutPressure = sim.GetScoutRead(enemyIndex).Pressure == ScoutSignalLevel.High;
        return leaking || meaningfullyBehind || clearLanePressure || scoutPressure;
    }

    private static int CountThreateningEnemyUnits(MatchSimulation sim, int playerIndex)
    {
        int enemyIndex = 1 - playerIndex;
        int towerPosition = playerIndex == 0 ? 0 : sim.LaneLengthUnits;
        int count = 0;

        var units = sim.Units;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (!unit.IsAlive || unit.OwnerPlayer != enemyIndex)
                continue;

            int distance = unit.PositionX > towerPosition
                ? unit.PositionX - towerPosition
                : towerPosition - unit.PositionX;
            if (distance <= 220)
                count++;
        }

        return count;
    }

    private bool TryPlaceWithPrerequisites(MatchSimulation sim, int playerIndex, BuildingInfo info, int maxCost = int.MaxValue)
    {
        if (!sim.MeetsRequirements(playerIndex, info.RequiredBuildingNames))
        {
            for (int i = 0; i < info.RequiredBuildingNames.Length; i++)
            {
                BuildingInfo? prerequisite = FindKnownBuilding(info.RequiredBuildingNames[i]);
                if (prerequisite.HasValue &&
                    !HasBuilding(sim, playerIndex, prerequisite.Value) &&
                    TryPlaceWithPrerequisites(sim, playerIndex, prerequisite.Value, maxCost))
                    return true;
            }

            return false;
        }

        if (info.GoldCost > maxCost || sim.GetGold(playerIndex) < info.GoldCost)
            return false;

        return TryPlaceRandom(sim, playerIndex, info);
    }

    private BuildingInfo? FindKnownBuilding(string buildingName)
    {
        if (_coreInfo.BuildingName == buildingName)
            return _coreInfo;
        if (_rangedInfo.HasValue && _rangedInfo.Value.BuildingName == buildingName)
            return _rangedInfo.Value;
        if (_tankInfo.HasValue && _tankInfo.Value.BuildingName == buildingName)
            return _tankInfo.Value;
        if (_siegeInfo.HasValue && _siegeInfo.Value.BuildingName == buildingName)
            return _siegeInfo.Value;
        if (_supportInfo.HasValue && _supportInfo.Value.BuildingName == buildingName)
            return _supportInfo.Value;
        if (_economyInfo.HasValue && _economyInfo.Value.BuildingName == buildingName)
            return _economyInfo.Value;
        return null;
    }

    private static bool HasBuilding(MatchSimulation sim, int playerIndex, BuildingInfo info)
    {
        return Count(sim, playerIndex, info) > 0;
    }

    private static int Count(MatchSimulation sim, int playerIndex, BuildingInfo info)
    {
        return sim.GetBuildingCountByName(playerIndex, info.BuildingName);
    }

    private bool TryPlaceRandom(MatchSimulation sim, int playerIndex, BuildingInfo info)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            int x = _rng.Next(0, _buildZoneWidth);
            int y = _rng.Next(0, _buildZoneHeight);
            if (sim.PlaceBuilding(playerIndex, x, y, info))
                return true;
        }

        for (int y = 0; y < _buildZoneHeight; y++)
        {
            for (int x = 0; x < _buildZoneWidth; x++)
            {
                if (sim.PlaceBuilding(playerIndex, x, y, info))
                    return true;
            }
        }

        return false;
    }
}
