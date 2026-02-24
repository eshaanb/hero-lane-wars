using System;
using LaneWars.Math;
using LaneWars.Core;

namespace LaneWars.Tests;

public static class TestFixedMath
{
    public static void RunAll()
    {
        TestAddition();
        TestMultiplication();
        TestDivision();
        TestComparisons();
        TestRngDeterminism();
        Console.WriteLine("All FixedMath tests passed!");
    }

    private static void TestAddition()
    {
        var a = Fixed.FromInt(3);
        var b = Fixed.FromInt(4);
        var result = a + b;
        Assert(result.ToInt() == 7, $"3 + 4 should be 7, got {result.ToInt()}");
    }

    private static void TestMultiplication()
    {
        var a = Fixed.FromInt(3);
        var b = Fixed.FromInt(4);
        var result = a * b;
        Assert(result.ToInt() == 12, $"3 * 4 should be 12, got {result.ToInt()}");
    }

    private static void TestDivision()
    {
        var a = Fixed.FromInt(10);
        var b = Fixed.FromInt(3);
        var result = a / b;
        Assert(result.ToInt() == 3, $"10 / 3 should truncate to 3, got {result.ToInt()}");
    }

    private static void TestComparisons()
    {
        var a = Fixed.FromInt(5);
        var b = Fixed.FromInt(10);
        Assert(a < b, "5 should be less than 10");
        Assert(b > a, "10 should be greater than 5");
        Assert(a == Fixed.FromInt(5), "5 should equal 5");
    }

    private static void TestRngDeterminism()
    {
        var rng1 = new SeededRng(12345);
        var rng2 = new SeededRng(12345);
        for (int i = 0; i < 100; i++)
        {
            int a = rng1.Next(0, 1000);
            int b = rng2.Next(0, 1000);
            Assert(a == b, $"RNG should be deterministic: iteration {i}, got {a} vs {b}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"TEST FAILED: {message}");
    }
}
