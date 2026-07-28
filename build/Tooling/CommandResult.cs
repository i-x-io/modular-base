namespace ModularBase.Build.Tooling;

internal sealed record CommandResult(
    int ExitCode,
    IReadOnlyList<string> Output,
    IReadOnlyList<string> Error);
