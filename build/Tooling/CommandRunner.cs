using System.Diagnostics;
using System.Globalization;
using Serilog;

namespace ModularBase.Build.Tooling;

internal sealed class CommandRunner(string workingDirectory)
{
    private const int MaximumCapturedCharacters = 1_000_000;
    private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromMinutes(10);
    private readonly string _workingDirectory = Path.GetFullPath(
        workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory)));

    public async Task<CommandResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan? timeout = null,
        IReadOnlySet<int>? allowedExitCodes = null,
        IReadOnlySet<int>? secretArgumentIndexes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentNullException.ThrowIfNull(arguments);
        TimeSpan effectiveTimeout = timeout ?? s_defaultTimeout;
        if (effectiveTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "The command timeout must be positive.");
        }

        LogInvocation(executable, arguments, secretArgumentIndexes);
        ProcessStartInfo startInfo = CreateStartInfo(executable, arguments);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start executable '{executable}'.");
        }

        Task<string[]> outputTask = ReadLinesAsync(process.StandardOutput);
        Task<string[]> errorTask = ReadLinesAsync(process.StandardError);
        using var timeoutCancellation = new CancellationTokenSource(effectiveTimeout);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCancellation.Token,
            cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            TryKill(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            _ = await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException(
                $"Command '{executable}' exceeded its {effectiveTimeout} timeout.",
                exception);
        }

        string[] output = await outputTask.ConfigureAwait(false);
        string[] error = await errorTask.ConfigureAwait(false);
        foreach (string line in output)
        {
            Log.Debug("{CommandOutput}", line);
        }

        foreach (string line in error)
        {
            Log.Debug("{CommandError}", line);
        }

        var result = new CommandResult(process.ExitCode, output, error);
        return process.ExitCode != 0 && allowedExitCodes?.Contains(process.ExitCode) != true
            ? throw new InvalidOperationException(
                string.Create(CultureInfo.InvariantCulture, $"Command '{executable}' failed with exit code {process.ExitCode}."))
            : result;
    }

    private ProcessStartInfo CreateStartInfo(string executable, IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static void LogInvocation(
        string executable,
        IEnumerable<string> arguments,
        IReadOnlySet<int>? secretArgumentIndexes)
    {
        string displayedArguments = string.Join(
            ' ',
            arguments.Select((argument, index) => secretArgumentIndexes?.Contains(index) == true
                ? "[REDACTED]"
                : argument));
        Log.Information("> {Executable} {Arguments}", executable, displayedArguments);
    }

    private static async Task<string[]> ReadLinesAsync(StreamReader reader)
    {
        var lines = new List<string>();
        int capturedCharacters = 0;
        bool truncated = false;
        while (await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(false) is { } line)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (capturedCharacters + trimmed.Length <= MaximumCapturedCharacters)
            {
                lines.Add(trimmed);
                capturedCharacters += trimmed.Length;
            }
            else
            {
                truncated = true;
            }
        }

        if (truncated)
        {
            lines.Add($"[output truncated after {MaximumCapturedCharacters} characters]");
        }

        return [.. lines];
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process exited between cancellation and cleanup.
        }
    }
}
