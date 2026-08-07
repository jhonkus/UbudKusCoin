using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockchainQualificationTests.Smoke;
using BlockchainQualificationTests.Consensus;
using BlockchainQualificationTests.Networking;
using BlockchainQualificationTests.Utilities;
using BlockchainQualificationTests.Security;
using BlockchainQualificationTests.LongRun;

namespace BlockchainQualificationTests;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var testMode = "Full Qualification";
        if (args.Length > 0)
        {
            testMode = args[0];
        }

        Console.WriteLine($"=================================================");
        Console.WriteLine($"UbudKusCoin Blockchain Qualification Test Runner");
        Console.WriteLine($"Selected Mode: {testMode}");
        Console.WriteLine($"=================================================");

        var results = new List<TestResult>();

        if (testMode.Equals("Smoke", StringComparison.OrdinalIgnoreCase) || testMode.Equals("Full Qualification", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(await SmokeTest.RunAsync());
        }

        if (testMode.Equals("Consensus", StringComparison.OrdinalIgnoreCase) || testMode.Equals("Full Qualification", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(await ConsensusTest.RunAsync());
        }

        if (testMode.Equals("Integration", StringComparison.OrdinalIgnoreCase) || testMode.Equals("Full Qualification", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(await NetworkingTest.RunAsync());
        }

        if (testMode.Equals("Upgrade", StringComparison.OrdinalIgnoreCase) || testMode.Equals("Full Qualification", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(Upgrade.UpgradeTest.Run());
        }

        if (testMode.Equals("Recovery", StringComparison.OrdinalIgnoreCase) || testMode.Equals("Full Qualification", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(await Recovery.RecoveryTest.RunAsync());
        }

        if (testMode.Equals("Replay", StringComparison.OrdinalIgnoreCase) || testMode.Equals("Full Qualification", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(Replay.ReplayTest.Run());
        }

        if (testMode.Equals("Crash", StringComparison.OrdinalIgnoreCase) || testMode.Equals("Full Qualification", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(Crash.CrashTest.Run());
        }

        if (testMode.Equals("Security", StringComparison.OrdinalIgnoreCase) || testMode.Equals("Full Qualification", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(SecurityTest.Run());
        }

        if (testMode.Equals("LongRun", StringComparison.OrdinalIgnoreCase) || testMode.Equals("Full Qualification", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(LongRunTest.Run());
        }

        // Add placeholders for tests not executed (NOT PROVEN / NOT TESTED) as per instructions
        if (testMode.Equals("Full Qualification", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(new TestResult { Name = "SQLite Independence Test", Status = "NOT PROVEN", Evidence = "Index deletion/rebuilding validation has not been run." });
        }

        ReportGenerator.Generate(testMode, results);

        Console.WriteLine("=================================================");
        Console.WriteLine("Qualification Run Completed Successfully.");
        Console.WriteLine("=================================================");

        // Exit code is 1 if any executed test failed, 0 otherwise
        bool anyFailed = results.Exists(r => r.Status == "FAIL");
        return anyFailed ? 1 : 0;
    }
}
