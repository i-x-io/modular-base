using ModularBase.Build.Tooling;

namespace ModularBase.Build.Validation;

internal sealed class RepositoryValidator(CommandRunner commands)
{
    private readonly CommandRunner _commands = commands ?? throw new ArgumentNullException(nameof(commands));

    public async Task ValidateAsync()
    {
        _ = await _commands.RunAsync("pre-commit", ["run", "--all-files", "--show-diff-on-failure"]).ConfigureAwait(false);
        _ = await _commands.RunAsync(
            "pre-commit",
            ["run", "gitleaks-repository", "--all-files", "--hook-stage", "manual"]).ConfigureAwait(false);
    }
}
