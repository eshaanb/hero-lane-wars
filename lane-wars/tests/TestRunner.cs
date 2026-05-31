using System;

namespace LaneWars.Tests;

public static class TestRunner
{
    public static void Main()
    {
        Console.WriteLine("Running Lane Wars tests...\n");

        // Opt-in balance tournament (analysis tool): set LANEWARS_BALANCE=1 to run.
        if (Environment.GetEnvironmentVariable("LANEWARS_BALANCE") != null)
            BalanceHarness.Run();

        TestFixedMath.RunAll();
        TestCombatResolver.RunAll();
        TestEconomy.RunAll();
        TestLaneSimulation.RunAll();
        TestMatchSimulation.RunAll();
        TestStrategicDepth.RunAll();
        TestStrategicAI.RunAll();
        TestSpamBenchmark.RunAll();

        Console.WriteLine("\n=== ALL TESTS PASSED ===");
    }
}
