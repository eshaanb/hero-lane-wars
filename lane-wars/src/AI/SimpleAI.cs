using LaneWars.Core;

namespace LaneWars.AI;

public class SimpleAI
{
    private readonly SeededRng _rng;
    private readonly int _thinkIntervalTicks; // How often AI considers building
    private int _lastThinkTick;
    private readonly BuildingInfo _barracksInfo;
    private readonly BuildingInfo? _rangedInfo;
    private readonly BuildingInfo? _tankInfo;
    private readonly BuildingInfo? _siegeInfo;
    private readonly BuildingInfo? _supportInfo;
    private readonly BuildingInfo? _economyInfo;
    private readonly int _buildZoneWidth;
    private readonly int _buildZoneHeight;

    public SimpleAI(
        ulong seed,
        BuildingInfo barracksInfo,
        BuildingInfo? rangedInfo = null,
        BuildingInfo? tankInfo = null,
        BuildingInfo? siegeInfo = null,
        BuildingInfo? supportInfo = null,
        BuildingInfo? economyInfo = null,
        int buildZoneWidth = 8,
        int buildZoneHeight = 4,
        int thinkIntervalTicks = 20)
    {
        _rng = new SeededRng(seed);
        _barracksInfo = barracksInfo;
        _rangedInfo = rangedInfo;
        _tankInfo = tankInfo;
        _siegeInfo = siegeInfo;
        _supportInfo = supportInfo;
        _economyInfo = economyInfo;
        _buildZoneWidth = buildZoneWidth;
        _buildZoneHeight = buildZoneHeight;
        _thinkIntervalTicks = thinkIntervalTicks;
        _lastThinkTick = 0;
    }

    public void Think(MatchSimulation sim, int playerIndex)
    {
        int currentTick = sim.SimTick.CurrentTick;
        if (currentTick - _lastThinkTick < _thinkIntervalTicks)
            return;
        _lastThinkTick = currentTick;

        // Early game: occasionally invest in economy first if available.
        if (_economyInfo.HasValue &&
            currentTick < 900 &&
            sim.GetBuildingCountByName(playerIndex, _economyInfo.Value.BuildingName) < 2 &&
            sim.GetGold(playerIndex) >= _economyInfo.Value.GoldCost &&
            _rng.Next(0, 100) < 35)
        {
            if (TryPlaceRandom(sim, playerIndex, _economyInfo.Value))
                return;
        }

        if (sim.GetBuildingCountByName(playerIndex, _barracksInfo.BuildingName) == 0 &&
            sim.GetGold(playerIndex) >= _barracksInfo.GoldCost)
        {
            if (TryPlaceRandom(sim, playerIndex, _barracksInfo))
                return;
        }

        if (_supportInfo.HasValue &&
            sim.GetBuildingCountByName(playerIndex, _supportInfo.Value.BuildingName) == 0 &&
            sim.GetGold(playerIndex) >= _supportInfo.Value.GoldCost &&
            _rng.Next(0, 100) < 30)
        {
            if (TryPlaceRandom(sim, playerIndex, _supportInfo.Value))
                return;
        }

        if (_tankInfo.HasValue &&
            sim.GetGold(playerIndex) >= _tankInfo.Value.GoldCost &&
            sim.GetBuildingCountByName(playerIndex, _tankInfo.Value.BuildingName) < 2 &&
            _rng.Next(0, 100) < 35)
        {
            if (TryPlaceRandom(sim, playerIndex, _tankInfo.Value))
                return;
        }

        if (_rangedInfo.HasValue &&
            sim.GetGold(playerIndex) >= _rangedInfo.Value.GoldCost &&
            sim.GetBuildingCountByName(playerIndex, _rangedInfo.Value.BuildingName) < 2 &&
            _rng.Next(0, 100) < 30)
        {
            if (TryPlaceRandom(sim, playerIndex, _rangedInfo.Value))
                return;
        }

        if (_siegeInfo.HasValue &&
            currentTick > 600 &&
            sim.GetGold(playerIndex) >= _siegeInfo.Value.GoldCost &&
            sim.GetBuildingCountByName(playerIndex, _siegeInfo.Value.BuildingName) < 1 &&
            _rng.Next(0, 100) < 25)
        {
            if (TryPlaceRandom(sim, playerIndex, _siegeInfo.Value))
                return;
        }

        if (sim.GetGold(playerIndex) >= _barracksInfo.GoldCost)
            TryPlaceRandom(sim, playerIndex, _barracksInfo);
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
        return false;
    }
}
