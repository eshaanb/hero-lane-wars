using System;
using LaneWars.Units;

namespace LaneWars.Tests;

public static class TestCombatResolver
{
    public static void RunAll()
    {
        TestPhysicalVsMedium();
        TestPiercingVsLight();
        TestPhysicalVsHeavy();
        TestMagicVsMagical();
        Console.WriteLine("All CombatResolver tests passed!");
    }

    private static void TestPhysicalVsMedium()
    {
        int dmg = CombatResolver.CalculateDamage(100, (int)DamageType.Physical, (int)ArmorType.Medium);
        Assert(dmg == 100, $"Physical vs Medium: expected 100, got {dmg}");
    }

    private static void TestPiercingVsLight()
    {
        int dmg = CombatResolver.CalculateDamage(100, (int)DamageType.Piercing, (int)ArmorType.Light);
        Assert(dmg == 150, $"Piercing vs Light: expected 150, got {dmg}");
    }

    private static void TestPhysicalVsHeavy()
    {
        int dmg = CombatResolver.CalculateDamage(100, (int)DamageType.Physical, (int)ArmorType.Heavy);
        Assert(dmg == 75, $"Physical vs Heavy: expected 75, got {dmg}");
    }

    private static void TestMagicVsMagical()
    {
        int dmg = CombatResolver.CalculateDamage(100, (int)DamageType.Magic, (int)ArmorType.Magical);
        Assert(dmg == 150, $"Magic vs Magical: expected 150, got {dmg}");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"TEST FAILED: {message}");
    }
}
