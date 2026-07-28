using ModularBase.Build.Repository;
using ModularBase.Build.Tooling;

namespace ModularBase.Build.Validation;

internal sealed class RepositoryValidator(BuildPaths paths, CommandRunner commands)
{
    private readonly BuildPaths _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    private readonly CommandRunner _commands = commands ?? throw new ArgumentNullException(nameof(commands));

    public async Task ValidateAsync()
    {
        PackageCatalogFile.Validate(_paths.RootDirectory);
        _ = await _commands.RunAsync("pre-commit", ["run", "--all-files", "--show-diff-on-failure"]).ConfigureAwait(false);
        _ = await _commands.RunAsync(
            "pre-commit",
            ["run", "gitleaks-repository", "--all-files", "--hook-stage", "manual"]).ConfigureAwait(false);
    }
}
