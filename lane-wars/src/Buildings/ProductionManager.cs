namespace LaneWars.Buildings;

using System.Collections.Generic;

public struct PlacedBuilding
{
    public int BuildingId;
    public string BuildingName;
    public int SpawnTimeMs;         // from BuildingData — time between spawns
    public int AccumulatorMs;       // counts up toward SpawnTimeMs
    public int BuildDelayRemainingMs;
    public int BuildDelayTotalMs;
    public int GridX;
    public int GridY;
    public int GridWidth;
    public int GridHeight;
    public int UnitHp;              // from UnitData
    public int UnitDamage;
    public int UnitAttackCooldownMs;
    public int UnitMoveSpeed;
    public int UnitRange;
    public int UnitArmorType;       // cast from ArmorType enum
    public int UnitDamageType;      // cast from DamageType enum
    public bool IsEconomyBuilding;
    public int IncomeBonus;
    public int SupportDamageBonus;
    public StrategicRole StrategicRole;
    public CompositionHint CompositionHint;
    public string UnitSpritePath;
    public int UnitTowerDamageMultiplierPct;
}

public struct UnitSpawnRequest
{
    public int OwnerPlayer;         // 0 or 1
    public int Hp;
    public int Damage;
    public int AttackCooldownMs;
    public int MoveSpeed;
    public int Range;
    public int ArmorType;
    public int DamageType;
    public int Direction;           // +1 or -1
    public int StartPositionX;
    public int SourceGridX;
    public int SourceGridY;
    public int SourceGridWidth;
    public int SourceGridHeight;
    public string SpritePath;
    public int TowerDamageMultiplierPct;
}

public class ProductionManager
{
    private readonly List<PlacedBuilding> _buildings = new();

    /// <summary>Add a building to be tracked for production ticks.</summary>
    public void AddBuilding(PlacedBuilding building)
    {
        if (building.BuildDelayTotalMs < building.BuildDelayRemainingMs)
            building.BuildDelayTotalMs = building.BuildDelayRemainingMs;
        if (building.GridWidth <= 0)
            building.GridWidth = 1;
        if (building.GridHeight <= 0)
            building.GridHeight = 1;

        _buildings.Add(building);
    }

    public bool TryGetBuilding(int buildingId, out PlacedBuilding building)
    {
        for (int i = 0; i < _buildings.Count; i++)
        {
            if (_buildings[i].BuildingId == buildingId)
            {
                building = _buildings[i];
                return true;
            }
        }

        building = default;
        return false;
    }

    /// <summary>
    /// Remove a building by its ID. Uses mark-and-sweep:
    /// find the index first, then remove after iteration.
    /// </summary>
    public void RemoveBuilding(int buildingId)
    {
        int removeIndex = -1;
        for (int i = 0; i < _buildings.Count; i++)
        {
            if (_buildings[i].BuildingId == buildingId)
            {
                removeIndex = i;
                break;
            }
        }
        if (removeIndex >= 0)
            _buildings.RemoveAt(removeIndex);
    }

    /// <summary>
    /// Advance all production accumulators by deltaMs.
    /// Returns a list of UnitSpawnRequests for buildings that reached their spawn threshold.
    /// Economy buildings do not spawn units.
    /// </summary>
    public List<UnitSpawnRequest> Tick(int deltaMs, int ownerPlayer, int direction, int startPositionX)
    {
        List<UnitSpawnRequest> spawns = new();
        int supportDamageBonus = GetTotalSupportDamageBonus();

        for (int i = 0; i < _buildings.Count; i++)
        {
            PlacedBuilding b = _buildings[i];

            if (b.BuildDelayRemainingMs > 0)
            {
                b.BuildDelayRemainingMs -= deltaMs;
                if (b.BuildDelayRemainingMs < 0)
                    b.BuildDelayRemainingMs = 0;
                _buildings[i] = b;
                continue;
            }

            // Non-unit buildings do not produce units.
            if (b.SpawnTimeMs <= 0)
                continue;

            b.AccumulatorMs += deltaMs;

            // Check if accumulator reached the spawn threshold
            if (b.AccumulatorMs >= b.SpawnTimeMs)
            {
                b.AccumulatorMs -= b.SpawnTimeMs;

                spawns.Add(new UnitSpawnRequest
                {
                    OwnerPlayer = ownerPlayer,
                    Hp = b.UnitHp,
                    Damage = b.UnitDamage + supportDamageBonus,
                    AttackCooldownMs = b.UnitAttackCooldownMs,
                    MoveSpeed = b.UnitMoveSpeed,
                    Range = b.UnitRange,
                    ArmorType = b.UnitArmorType,
                    DamageType = b.UnitDamageType,
                    Direction = direction,
                    StartPositionX = startPositionX,
                    SourceGridX = b.GridX,
                    SourceGridY = b.GridY,
                    SourceGridWidth = b.GridWidth,
                    SourceGridHeight = b.GridHeight,
                    SpritePath = b.UnitSpritePath,
                    TowerDamageMultiplierPct = b.UnitTowerDamageMultiplierPct
                });
            }

            // Write the mutated struct back into the list
            _buildings[i] = b;
        }

        return spawns;
    }

    /// <summary>Count of buildings that produce units.</summary>
    public int GetProductionBuildingCount()
    {
        int count = 0;
        for (int i = 0; i < _buildings.Count; i++)
        {
            if (_buildings[i].SpawnTimeMs > 0)
                count++;
        }
        return count;
    }

    /// <summary>Count of income-generating buildings.</summary>
    public int GetEconomyBuildingCount()
    {
        int count = 0;
        for (int i = 0; i < _buildings.Count; i++)
        {
            if (_buildings[i].IncomeBonus > 0)
                count++;
        }
        return count;
    }

    /// <summary>Total income bonus from all currently placed buildings.</summary>
    public int GetTotalIncomeBonus()
    {
        int bonus = 0;
        for (int i = 0; i < _buildings.Count; i++)
        {
            if (_buildings[i].BuildDelayRemainingMs <= 0)
                bonus += _buildings[i].IncomeBonus;
        }
        return bonus;
    }

    public int GetTotalSupportDamageBonus()
    {
        int bonus = 0;
        for (int i = 0; i < _buildings.Count; i++)
        {
            if (_buildings[i].BuildDelayRemainingMs <= 0)
                bonus += _buildings[i].SupportDamageBonus;
        }
        return bonus;
    }
}
