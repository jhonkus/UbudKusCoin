using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockchainQualificationTests.Smoke;
using BlockchainQualificationTests.Consensus;
using BlockchainQualificationTests.Networking;
using BlockchainQualificationTests.Utilities;

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

        // Add placeholders for tests not executed (NOT PROVEN / NOT TESTED) as per instructions
        if (testMode.Equals("Full Qualification", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(new TestResult { Name = "Replay Test", Status = "NOT PROVEN", Evidence = "Blockchain replay validation has not been run in this cycle." });
            results.Add(new TestResult { Name = "Crash Recovery Test", Status = "NOT PROVEN", Evidence = "Database partial transaction write crash recovery validation has not been run." });
            results.Add(new TestResult { Name = "SQLite Independence Test", Status = "NOT PROVEN", Evidence = "Index deletion/rebuilding validation has not been run." });
            results.Add(new TestResult { Name = "Security Adversarial Fuzzing", Status = "NOT PROVEN", Evidence = "Malformed transaction signature boundary tests have not been run." });
            results.Add(new TestResult { Name = "10,000 Block Soak Test", Status = "NOT PROVEN", Evidence = "Long running stability profiling has not been executed." });
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
