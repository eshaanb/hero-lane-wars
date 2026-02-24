namespace LaneWars.Units;

/// <summary>
/// Runtime state for a single unit in the lane simulation.
/// Uses a class (not struct) because we mutate it in-place and store references.
/// </summary>
public class UnitState
{
    public int UnitId;
    public int OwnerPlayer;         // 0 or 1
    public int Hp;
    public int MaxHp;
    public int Damage;
    public int AttackCooldownMs;
    public int AttackTimerMs;       // counts down to 0, then unit can attack
    public int MoveSpeed;           // units per second
    public int Range;
    public int ArmorType;
    public int DamageType;
    public int PositionX;           // 1D lane position (integer)
    public int Direction;           // +1 (toward enemy base) or -1
    public int TargetUnitId;        // -1 if no target
    public bool IsAlive;

    public UnitState()
    {
        TargetUnitId = -1;
        IsAlive = true;
    }
}
