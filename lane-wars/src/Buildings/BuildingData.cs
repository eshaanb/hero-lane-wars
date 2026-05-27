using Godot;
using LaneWars.Units;

namespace LaneWars.Buildings;

public enum StrategicRole { Pressure, Economy, Defense, Tech }
public enum CompositionHint { Unknown, Swarm, Heavy, Splash, Mixed }

[GlobalClass]
public partial class BuildingData : Resource
{
    [Export] public string BuildingName { get; set; } = "";
    [Export] public string[] RequiredBuildingNames { get; set; } = System.Array.Empty<string>();
    [Export] public string[] UnlocksBuildingNames { get; set; } = System.Array.Empty<string>();
    [Export] public int GoldCost { get; set; }
    [Export] public int BuildDelayMs { get; set; } = 0;
    [Export] public int GridWidth { get; set; } = 1;
    [Export] public int GridHeight { get; set; } = 1;
    [Export] public StrategicRole StrategicRole { get; set; } = StrategicRole.Pressure;
    [Export] public CompositionHint CompositionHint { get; set; } = CompositionHint.Unknown;
    [Export] public UnitData? SpawnedUnit { get; set; }
    [Export] public bool IsEconomyBuilding { get; set; } = false;
    [Export] public int IncomeBonus { get; set; } = 0;
    [Export] public int SupportDamageBonus { get; set; } = 0;
    [Export] public string SpritePath { get; set; } = "";
}
