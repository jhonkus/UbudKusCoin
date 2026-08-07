using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BlockchainQualificationTests.Utilities;

namespace BlockchainQualificationTests.Recovery;

public static class RecoveryTest
{
    public static async Task<TestResult> RunAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("[RECOVERY TEST] Starting...");

        try
        {
            var scriptPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../deploy/cometbft/test-multinode-recovery.ps1"));
            var workingDir = Path.GetDirectoryName(scriptPath) ?? "";

            Console.WriteLine($"[RECOVERY TEST] Executing script: {scriptPath}");
            
            // Set WALLET_ENCRYPTION_KEY in the environment variables
            Environment.SetEnvironmentVariable("WALLET_ENCRYPTION_KEY", "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");

            var result = await ProcessHelper.RunAsync("powershell", $"-ExecutionPolicy Bypass -File \"{scriptPath}\"", workingDir);
            
            if (result.ExitCode != 0)
            {
                return new TestResult
                {
                    Name = "Process Failure & Recovery Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Recovery script failed with exit code {result.ExitCode}.\nErrors:\n{result.StandardError}\nLogs:\n{result.StandardOutput}"
                };
            }

            return new TestResult
            {
                Name = "Process Failure & Recovery Test",
                Status = "PASS",
                Duration = stopwatch.Elapsed,
                Evidence = "Successfully verified validator recovery and catch-up after killing the application process only, CometBFT process only, and both processes.\n" + result.StandardOutput
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = "Process Failure & Recovery Test",
                Status = "FAIL",
                Duration = stopwatch.Elapsed,
                Evidence = $"Exception occurred: {ex.Message}"
            };
        }
    }
}
