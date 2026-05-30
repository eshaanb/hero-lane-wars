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
    public int TowerDamageMultiplierPct;
    public int SplashRadius;        // 0 = single-target; >0 deals AoE around the primary target
    public int Bounty;              // gold awarded to whoever lands the killing blow
    public int PositionX;           // 1D lane position (integer)
    public int Direction;           // +1 (toward enemy base) or -1
    public int AgeMs;
    public int SpawnGridX;
    public int SpawnGridY;
    public int SpawnGridWidth;
    public int SpawnGridHeight;
    public int TargetUnitId;        // -1 if no target
    public int RecentAttackMs;      // short-lived visual flag for hit flashes
    public string SpritePath;       // resource path carried through sim for rendering
    public bool IsAlive;

    public UnitState()
    {
        SpawnGridX = -1;
        SpawnGridY = -1;
        SpawnGridWidth = 0;
        SpawnGridHeight = 0;
        TargetUnitId = -1;
        RecentAttackMs = 0;
        SpritePath = "";
        IsAlive = true;
    }
}
