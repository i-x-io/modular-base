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

    public async Task<IReadOnlyList<string>> GenerateSbomsAsync(
        IReadOnlyCollection<PackageInspection> packages,
        BuildConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(packages);
        _ = _paths.SbomDirectory.CreateOrCleanDirectory();
        var sboms = new List<string>(packages.Count);
        foreach (PackageInspection package in packages)
        {
            AbsolutePath output = _paths.SbomDirectory / package.Project.PackageId;
            _ = output.CreateOrCleanDirectory();
            _ = await _commands.RunAsync(
                "dotnet",
                [
                    "CycloneDX", package.Project.ProjectFile,
                    "--output-format", "Json",
                    "--output", output,
                    "--filename", $"{package.Project.PackageId}.cdx.json",
                    "--disable-package-restore",
                    "--configuration", configuration.ToString(),
                    "--exclude-dev",
                    "--exclude-test-projects",
                    "--set-name", package.Project.PackageId,
                    "--set-version", package.Version.ToNormalizedString(),
                    "--set-type", "Library",
                    "--set-nuget-purl",
                ]).ConfigureAwait(false);
            sboms.Add(SbomValidator.Validate(output, package));
        }

        return sboms;
    }
}
