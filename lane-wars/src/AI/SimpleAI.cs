using LaneWars.Core;

namespace LaneWars.AI;

public class SimpleAI
{
    private readonly SeededRng _rng;
    private readonly int _thinkIntervalTicks; // How often AI considers building
    private int _lastThinkTick;
    private readonly BuildingInfo _barracksInfo;
    private readonly int _buildZoneWidth;
    private readonly int _buildZoneHeight;

    public SimpleAI(ulong seed, BuildingInfo barracksInfo, int buildZoneWidth = 8, int buildZoneHeight = 4, int thinkIntervalTicks = 20)
    {
        _rng = new SeededRng(seed);
        _barracksInfo = barracksInfo;
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

        // Try to place a barracks if affordable
        if (sim.GetGold(playerIndex) < _barracksInfo.GoldCost)
            return;

        // Try random positions until one works (max 10 attempts)
        for (int attempt = 0; attempt < 10; attempt++)
        {
            int x = _rng.Next(0, _buildZoneWidth);
            int y = _rng.Next(0, _buildZoneHeight);
            if (sim.PlaceBuilding(playerIndex, x, y, _barracksInfo))
                return;
        }
    }
}
