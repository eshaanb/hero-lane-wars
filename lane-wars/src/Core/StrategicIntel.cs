namespace LaneWars.Core;

using LaneWars.Buildings;

public enum ScoutSignalLevel { Low, Medium, High }

public readonly struct ScoutRead
{
    public ScoutSignalLevel Economy { get; init; }
    public ScoutSignalLevel Pressure { get; init; }
    public ScoutSignalLevel Defense { get; init; }
    public ScoutSignalLevel Tech { get; init; }
    public CompositionHint CompositionHint { get; init; }
}

public readonly struct RecentSpendingSummary
{
    public ScoutSignalLevel Economy { get; init; }
    public ScoutSignalLevel Pressure { get; init; }
    public ScoutSignalLevel Defense { get; init; }
    public ScoutSignalLevel Tech { get; init; }
    public CompositionHint CompositionHint { get; init; }
    public int EventCount { get; init; }
}

public readonly struct PostgameAnalysis
{
    public string AiPlan { get; init; }
    public string KeyEnemyTiming { get; init; }
    public string LikelyMistake { get; init; }
    public string SuggestedAdaptation { get; init; }
}
