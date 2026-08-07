using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlockchainQualificationTests.Utilities;

public static class ProcessHelper
{
    /// <summary>
    /// Runs an external process and captures stdout/stderr.
    /// If the process does not exit within <paramref name="timeoutSeconds"/>,
    /// it is forcibly killed and an exit code of -1 is returned.
    /// </summary>
    public static async Task<ProcessResult> RunAsync(
        string command,
        string arguments,
        string workingDirectory = "",
        int timeoutSeconds = 600)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        var output = new StringBuilder();
        var error  = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
        process.ErrorDataReceived  += (_, e) => { if (e.Data != null) error.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Timeout — kill the process tree
            try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            return new ProcessResult(
                -1,
                output.ToString().Trim(),
                $"[TIMEOUT] Process did not exit within {timeoutSeconds}s.\n" + error.ToString().Trim());
        }

        return new ProcessResult(process.ExitCode, output.ToString().Trim(), error.ToString().Trim());
    }
}

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
