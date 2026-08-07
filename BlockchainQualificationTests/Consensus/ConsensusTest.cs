using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BlockchainQualificationTests.Utilities;

namespace BlockchainQualificationTests.Consensus;

public static class ConsensusTest
{
    public static async Task<TestResult> RunAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("[CONSENSUS TEST] Starting...");

        try
        {
            var scriptPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../deploy/cometbft/test-multinode-validator-update.ps1"));
            var workingDir = Path.GetDirectoryName(scriptPath) ?? "";

            Console.WriteLine($"[CONSENSUS TEST] Executing script: {scriptPath}");
            
            // Set WALLET_ENCRYPTION_KEY in the environment variables
            Environment.SetEnvironmentVariable("WALLET_ENCRYPTION_KEY", "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");

            var result = await ProcessHelper.RunAsync("powershell", $"-ExecutionPolicy Bypass -File \"{scriptPath}\"", workingDir);
            
            if (result.ExitCode != 0)
            {
                return new TestResult
                {
                    Name = "Consensus & Validator Key Rotation Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Consensus script failed with exit code {result.ExitCode}.\nErrors:\n{result.StandardError}\nLogs:\n{result.StandardOutput}"
                };
            }

            return new TestResult
            {
                Name = "Consensus & Validator Key Rotation Test",
                Status = "PASS",
                Duration = stopwatch.Elapsed,
                Evidence = "4-Node CometBFT cluster achieved consensus, successfully validated and committed block heights, rotated consensus validator key, and shut down without divergence.\n" + result.StandardOutput
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = "Consensus & Validator Key Rotation Test",
                Status = "FAIL",
                Duration = stopwatch.Elapsed,
                Evidence = $"Exception occurred: {ex.Message}"
            };
        }
    }
}
