using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BlockchainQualificationTests.Utilities;

namespace BlockchainQualificationTests.Networking;

public static class NetworkingTest
{
    public static async Task<TestResult> RunAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("[NETWORKING TEST] Starting...");

        try
        {
            var scriptPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../deploy/cometbft/test-multinode-state-sync.ps1"));
            var workingDir = Path.GetDirectoryName(scriptPath) ?? "";

            Console.WriteLine($"[NETWORKING TEST] Executing script: {scriptPath}");
            
            // Set WALLET_ENCRYPTION_KEY in the environment variables
            Environment.SetEnvironmentVariable("WALLET_ENCRYPTION_KEY", "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");

            var result = await ProcessHelper.RunAsync("powershell", $"-ExecutionPolicy Bypass -File \"{scriptPath}\"", workingDir);
            
            if (result.ExitCode != 0)
            {
                return new TestResult
                {
                    Name = "Networking & State Sync Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"State sync script failed with exit code {result.ExitCode}.\nErrors:\n{result.StandardError}\nLogs:\n{result.StandardOutput}"
                };
            }

            return new TestResult
            {
                Name = "Networking & State Sync Test",
                Status = "PASS",
                Duration = stopwatch.Elapsed,
                Evidence = "Bootstrap state sync succeeded. Joining validator downloaded and verified snapshot state machine data, converging heights successfully.\n" + result.StandardOutput
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = "Networking & State Sync Test",
                Status = "FAIL",
                Duration = stopwatch.Elapsed,
                Evidence = $"Exception occurred: {ex.Message}"
            };
        }
    }
}
