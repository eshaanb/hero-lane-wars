namespace LaneWars.Units;

/// <summary>
/// Resolves combat damage using the damage-type vs armor-type matrix.
/// All math is integer-only (percentages stored as int, divided by 100).
/// </summary>
public static class CombatResolver
{
    // [damageType, armorType] = percentage (100 = 100%)
    // Rows: Physical=0, Piercing=1, Siege=2, Magic=3
    // Cols: Light=0, Medium=1, Heavy=2, Magical=3
    private static readonly int[,] DamageMatrix = new int[4, 4]
    {
        //          Light  Medium  Heavy  Magical
        /* Physical */ { 100, 100,  75, 100 },
        /* Piercing */ { 150,  75, 100, 100 },
        /* Siege    */ { 100, 100, 150,  75 },
        /* Magic    */ {  75, 100, 100, 150 },
    };

    /// <summary>
    /// Calculate final damage after applying the damage-type vs armor-type multiplier.
    /// Uses integer math: baseDamage * percentage / 100.
    /// </summary>
    public static int CalculateDamage(int baseDamage, int damageType, int armorType)
    {
        int pct = DamageMatrix[damageType, armorType];
        return baseDamage * pct / 100;
    }
}
