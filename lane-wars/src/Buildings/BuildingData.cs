using Godot;
using LaneWars.Units;

namespace LaneWars.Buildings;

[GlobalClass]
public partial class BuildingData : Resource
{
    [Export] public string BuildingName { get; set; } = "";
    [Export] public string[] RequiredBuildingNames { get; set; } = System.Array.Empty<string>();
    [Export] public string[] UnlocksBuildingNames { get; set; } = System.Array.Empty<string>();
    [Export] public int GoldCost { get; set; }
    [Export] public int GridWidth { get; set; } = 1;
    [Export] public int GridHeight { get; set; } = 1;
    [Export] public UnitData? SpawnedUnit { get; set; }
    [Export] public bool IsEconomyBuilding { get; set; } = false;
    [Export] public int IncomeBonus { get; set; } = 0;
    [Export] public int SupportDamageBonus { get; set; } = 0;
    [Export] public string SpritePath { get; set; } = "";
}
