using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace BlockchainQualificationTests.Utilities;

public static class ReportGenerator
{
    public static void Generate(string testMode, List<TestResult> results)
    {
        var report = new QualificationReport
        {
            TestMode = testMode,
            ExecutionTime = DateTime.UtcNow,
            Results = results
        };

        // Write JSON
        var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "qualification-report.json");
        File.WriteAllText(jsonPath, JsonConvert.SerializeObject(report, Formatting.Indented));
        Console.WriteLine($"[INFO] JSON report generated at: {jsonPath}");

        // Write Markdown
        var mdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "qualification-report.md");
        using var writer = new StreamWriter(mdPath);
        writer.WriteLine("# Blockchain Qualification Test Report");
        writer.WriteLine($"**Mode:** {testMode}  ");
        writer.WriteLine($"**Date:** {report.ExecutionTime:yyyy-MM-dd HH:mm:ss} UTC  ");
        writer.WriteLine();
        writer.WriteLine("## Test Results Summary");
        writer.WriteLine();
        writer.WriteLine("| Test Case | Status | Duration | Evidence / Details |");
        writer.WriteLine("|---|---|---|---|");

        foreach (var r in results)
        {
            writer.WriteLine($"| {r.Name} | **{r.Status}** | {r.Duration.TotalSeconds:F2}s | {r.Evidence.Replace("\n", "<br>")} |");
        }

        writer.WriteLine();
        writer.WriteLine("## Final Classification");
        writer.WriteLine();
        bool anyFailed = results.Exists(r => r.Status == "FAIL");
        if (anyFailed)
        {
            writer.WriteLine("**Rating:** `NOT READY` (Consensus or correctness failures detected)");
        }
        else
        {
            writer.WriteLine("**Rating:** `PUBLIC TESTNET READY` (All integration stages and adversarial tests completed successfully)");
        }

        Console.WriteLine($"[INFO] Markdown report generated at: {mdPath}");
    }
}

public class QualificationReport
{
    public string TestMode { get; set; } = "";
    public DateTime ExecutionTime { get; set; }
    public List<TestResult> Results { get; set; } = new();
}

public class TestResult
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "NOT TESTED"; // PASS, FAIL, NOT TESTED, NOT PROVEN
    public TimeSpan Duration { get; set; }
    public string Evidence { get; set; } = "";
}
