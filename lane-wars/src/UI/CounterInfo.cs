using System.Collections.Generic;
using LaneWars.Units;

namespace LaneWars.UI;

/// <summary>
/// Human-readable counter text derived from the live damage matrix in
/// <see cref="CombatResolver"/>, so tooltips/overlays never drift from the real numbers.
/// </summary>
internal static class CounterInfo
{
    private static readonly string[] ArmorNames = { "Light", "Medium", "Heavy", "Magical" };
    private static readonly string[] DamageNames = { "Physical", "Piercing", "Siege", "Magic" };

    public static string ArmorName(int a) => a >= 0 && a < ArmorNames.Length ? ArmorNames[a] : "?";
    public static string DamageName(int d) => d >= 0 && d < DamageNames.Length ? DamageNames[d] : "?";

    /// <summary>Percent multiplier (e.g. 150) for a damage type against an armor type.</summary>
    public static int Multiplier(int damageType, int armorType) =>
        CombatResolver.CalculateDamage(100, damageType, armorType);

    /// <summary>"strong vs Light, weak vs Medium" — what this damage type does to each armor.</summary>
    public static string DamageSummary(int damageType)
    {
        var strong = new List<string>();
        var weak = new List<string>();
        for (int a = 0; a < ArmorNames.Length; a++)
        {
            int pct = Multiplier(damageType, a);
            if (pct > 100) strong.Add(ArmorNames[a]);
            else if (pct < 100) weak.Add(ArmorNames[a]);
        }
        return Join(
            strong.Count > 0 ? $"strong vs {string.Join("/", strong)}" : "",
            weak.Count > 0 ? $"weak vs {string.Join("/", weak)}" : "");
    }

    /// <summary>"weak to Siege, resists Physical" — what hurts/bounces off this armor type.</summary>
    public static string ArmorSummary(int armorType)
    {
        var vuln = new List<string>();
        var resist = new List<string>();
        for (int d = 0; d < DamageNames.Length; d++)
        {
            int pct = Multiplier(d, armorType);
            if (pct > 100) vuln.Add(DamageNames[d]);
            else if (pct < 100) resist.Add(DamageNames[d]);
        }
        return Join(
            vuln.Count > 0 ? $"weak to {string.Join("/", vuln)}" : "",
            resist.Count > 0 ? $"resists {string.Join("/", resist)}" : "");
    }

    private static string Join(string a, string b) =>
        a.Length > 0 && b.Length > 0 ? $"{a}, {b}" : a + b;
}
