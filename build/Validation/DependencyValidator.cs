using System.Globalization;
using ModularBase.Build.Pipeline;
using ModularBase.Build.Repository;
using ModularBase.Build.Tooling;
using Nuke.Common.IO;

namespace ModularBase.Build.Validation;

internal sealed class DependencyValidator(BuildPaths paths, CommandRunner commands)
{
    private readonly BuildPaths _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    private readonly CommandRunner _commands = commands ?? throw new ArgumentNullException(nameof(commands));

    public async Task AuditAsync(IReadOnlyCollection<BuildUnit> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        foreach (BuildUnit unit in units)
        {
            CommandResult result = await _commands.RunAsync(
                "dotnet",
                [
                    "package", "list", "--project", unit.Path,
                    "--vulnerable", "--include-transitive", "--format", "json",
                    "--output-version", "1", "--no-restore",
                ]).ConfigureAwait(false);
            int findingCount = DependencyAuditParser.CountFindings(
                string.Join(Environment.NewLine, result.Output));
            if (findingCount != 0)
            {
                throw new InvalidOperationException(
                    $"The {unit.Name} dependency graph contains "
                    + $"{findingCount.ToString(CultureInfo.InvariantCulture)} vulnerable package(s).");
            }
        }
    }

    public async Task<string> GenerateSbomAsync(BuildUnit solution, BuildConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(solution);
        _ = _paths.SbomDirectory.CreateOrCleanDirectory();
        _ = await _commands.RunAsync(
            "dotnet",
            [
                "CycloneDX", solution.Path, "--output-format", "Json", "--output",
                _paths.SbomDirectory, "--filename", "bom.json", "--disable-package-restore",
                "--configuration", configuration.ToString(),
            ]).ConfigureAwait(false);
        return SbomValidator.Validate(_paths.SbomDirectory);
    }
}
