using System;

namespace LaneWars.Tests;

public static class TestRunner
{
    public static void Main()
    {
        Console.WriteLine("Running Lane Wars tests...\n");

        TestFixedMath.RunAll();
        TestCombatResolver.RunAll();
        TestEconomy.RunAll();
        TestLaneSimulation.RunAll();
        TestMatchSimulation.RunAll();
        TestStrategicDepth.RunAll();
        TestStrategicAI.RunAll();

        Console.WriteLine("\n=== ALL TESTS PASSED ===");
    }
}
