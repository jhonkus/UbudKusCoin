using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BlockchainQualificationTests.Utilities;

namespace BlockchainQualificationTests.Smoke;

public static class SmokeTest
{
    public static async Task<TestResult> RunAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("[SMOKE TEST] Starting...");

        try
        {
            // 1. Run unit tests
            Console.WriteLine("[SMOKE TEST] Executing dotnet test...");
            var testResult = await ProcessHelper.RunAsync("dotnet", "test UbudKusCoin.sln --nologo");
            if (testResult.ExitCode != 0)
            {
                return new TestResult
                {
                    Name = "Smoke Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Dotnet test failed with exit code {testResult.ExitCode}.\nLogs:\n{testResult.StandardError}\n{testResult.StandardOutput}"
                };
            }

            return new TestResult
            {
                Name = "Smoke Test",
                Status = "PASS",
                Duration = stopwatch.Elapsed,
                Evidence = "Dotnet test completed successfully. All unit tests passed.\n" + testResult.StandardOutput
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = "Smoke Test",
                Status = "FAIL",
                Duration = stopwatch.Elapsed,
                Evidence = $"Exception occurred: {ex.Message}"
            };
        }
    }
}
