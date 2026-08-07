using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainQualificationTests.Utilities;

public static class ProcessHelper
{
    public static async Task<ProcessResult> RunAsync(string command, string arguments, string workingDirectory = "")
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
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return new ProcessResult(process.ExitCode, output.ToString().Trim(), error.ToString().Trim());
    }
}

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
