using Godot;
using LaneWars.Buildings;

namespace LaneWars.Core;

[GlobalClass]
public partial class RaceData : Resource
{
    [Export] public string RaceName { get; set; } = "Unknown";
    [Export] public string Description { get; set; } = "";
    [Export] public string TowerName { get; set; } = "Tower";
    [Export] public int TowerHp { get; set; } = 400;
    [Export] public int TowerAttackDamage { get; set; } = 18;
    [Export] public int TowerAttackCooldownMs { get; set; } = 1000;
    [Export] public int TowerAttackRange { get; set; } = 220;
    [Export] public string TowerSpritePath { get; set; } = "";
    [Export] public BuildingData[] AvailableBuildings { get; set; } = System.Array.Empty<BuildingData>();
}
