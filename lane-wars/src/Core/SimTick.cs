namespace LaneWars.Core;

public class SimTick
{
    public int CurrentTick { get; private set; }
    public int ElapsedMs => CurrentTick * TickIntervalMs;
    public int TickIntervalMs { get; }

    public SimTick(int tickIntervalMs = 100)
    {
        TickIntervalMs = tickIntervalMs;
    }

    public void Advance() => CurrentTick++;
}
