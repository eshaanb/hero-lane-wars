using Godot;

namespace LaneWars.Units;

public enum ArmorType { Light, Medium, Heavy, Magical }
public enum DamageType { Physical, Piercing, Siege, Magic }

[GlobalClass]
public partial class UnitData : Resource
{
    [Export] public string UnitName { get; set; } = "";
    [Export] public string UnitRole { get; set; } = "Fighter";
    [Export] public int Hp { get; set; }
    [Export] public int Damage { get; set; }
    [Export] public int AttackCooldownMs { get; set; } = 1000;
    [Export] public int MoveSpeedUnits { get; set; } = 80;
    [Export] public int Range { get; set; } = 0;
    [Export] public ArmorType ArmorType { get; set; } = ArmorType.Medium;
    [Export] public DamageType DamageType { get; set; } = DamageType.Physical;
    [Export] public int SpawnTimeMs { get; set; } = 4000;
    [Export] public int TowerDamageMultiplierPct { get; set; } = 100;
    [Export] public string SpritePath { get; set; } = "";
}
