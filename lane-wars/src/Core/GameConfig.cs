using Godot;

namespace LaneWars.Core;

[GlobalClass]
public partial class GameConfig : Resource
{
    [Export] public int StartingGold { get; set; } = 100;
    [Export] public int BaseIncomePerTick { get; set; } = 10;
    [Export] public int IncomeTickMs { get; set; } = 10000;
    [Export] public int BaseHpPerLane { get; set; } = 50;
    [Export] public int BuildZoneWidth { get; set; } = 8;
    [Export] public int BuildZoneHeight { get; set; } = 4;
    [Export] public int SimTickMs { get; set; } = 100;
    [Export] public int LaneLengthUnits { get; set; } = 1000;
}
