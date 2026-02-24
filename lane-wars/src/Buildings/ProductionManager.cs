namespace LaneWars.Buildings;

using System.Collections.Generic;

public struct PlacedBuilding
{
    public int BuildingId;
    public int SpawnTimeMs;         // from BuildingData — time between spawns
    public int AccumulatorMs;       // counts up toward SpawnTimeMs
    public int UnitHp;              // from UnitData
    public int UnitDamage;
    public int UnitAttackCooldownMs;
    public int UnitMoveSpeed;
    public int UnitRange;
    public int UnitArmorType;       // cast from ArmorType enum
    public int UnitDamageType;      // cast from DamageType enum
    public bool IsEconomyBuilding;
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
}

public class ProductionManager
{
    private readonly List<PlacedBuilding> _buildings = new();

    /// <summary>Add a building to be tracked for production ticks.</summary>
    public void AddBuilding(PlacedBuilding building)
    {
        _buildings.Add(building);
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

        for (int i = 0; i < _buildings.Count; i++)
        {
            PlacedBuilding b = _buildings[i];

            // Economy buildings do not produce units
            if (b.IsEconomyBuilding)
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
                    Damage = b.UnitDamage,
                    AttackCooldownMs = b.UnitAttackCooldownMs,
                    MoveSpeed = b.UnitMoveSpeed,
                    Range = b.UnitRange,
                    ArmorType = b.UnitArmorType,
                    DamageType = b.UnitDamageType,
                    Direction = direction,
                    StartPositionX = startPositionX
                });
            }

            // Write the mutated struct back into the list
            _buildings[i] = b;
        }

        return spawns;
    }

    /// <summary>Count of buildings that produce units (non-economy).</summary>
    public int GetProductionBuildingCount()
    {
        int count = 0;
        for (int i = 0; i < _buildings.Count; i++)
        {
            if (!_buildings[i].IsEconomyBuilding)
                count++;
        }
        return count;
    }

    /// <summary>Count of economy buildings.</summary>
    public int GetEconomyBuildingCount()
    {
        int count = 0;
        for (int i = 0; i < _buildings.Count; i++)
        {
            if (_buildings[i].IsEconomyBuilding)
                count++;
        }
        return count;
    }
}
