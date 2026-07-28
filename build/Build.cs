using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace ModularBase.Build;

internal enum BuildConfiguration
{
    Debug,
    Release,
}

internal sealed partial class Build : NukeBuild
{
    private const string PackageId = "IX.Modularity";
    private const string PackageTagPrefix = "IX.Modularity-v";
    private const string PackageSource = "https://nuget.pkg.github.com/i-x-io/index.json";

    [Solution("IX.Modularity.slnx", GenerateProjects = true)]
    private readonly global::Solution Solution = null!;

    [GitRepository]
    private readonly GitRepository Repository = null!;

    private Project ProductProject => Solution.src.IX_Modularity;

    private Project TestProject => Solution.test.IX_Modularity_Tests;

    private AbsolutePath BuildProject => BuildProjectFile;

    private AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    private AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";

    private AbsolutePath TestResultsDirectory => ArtifactsDirectory / "test-results";

    private AbsolutePath SbomDirectory => ArtifactsDirectory / "sbom";

    [Parameter("Build configuration. Defaults to Release.")]
    private readonly BuildConfiguration Configuration = BuildConfiguration.Release;

    [Parameter("Short-lived GitHub Packages token. Required only by Publish.")]
    [Secret]
    private readonly string? PackageToken = null;

    public static int Main() => Execute<Build>(build => build.Validate);

    private Target Clean => target => target
        .Description("Removes repository build artifacts.")
        .Executes(() => ArtifactsDirectory.CreateOrCleanDirectory());

    private Target UpdateLocks => target => target
        .Description("Regenerates package lock files after a reviewed dependency change.")
        .Executes(() =>
        {
            DotNetToolRestore(settings => settings
                .SetProcessWorkingDirectory(RootDirectory));
            DotNetRestore(settings => settings
                .SetProjectFile(Solution.Path)
                .EnableForceEvaluate()
                .DisableLockedMode()
                .SetProperty("RestoreLockedMode", false)
                .SetProcessWorkingDirectory(RootDirectory));
            DotNetRestore(settings => settings
                .SetProjectFile(BuildProject)
                .EnableForceEvaluate()
                .DisableLockedMode()
                .SetProperty("RestoreLockedMode", false)
                .SetProcessWorkingDirectory(RootDirectory));
        });

    private Target Restore => target => target
        .Description("Restores local tools and all projects using committed lock files.")
        .Executes(() =>
        {
            AssertRepositoryShape();
            DotNetToolRestore(settings => settings
                .SetProcessWorkingDirectory(RootDirectory));
            DotNetRestore(settings => settings
                .SetProjectFile(Solution.Path)
                .EnableLockedMode()
                .SetProcessWorkingDirectory(RootDirectory));
            DotNetRestore(settings => settings
                .SetProjectFile(BuildProject)
                .EnableLockedMode()
                .SetProcessWorkingDirectory(RootDirectory));
        });

    private Target Format => target => target
        .Description("Verifies repository C# and project formatting.")
        .DependsOn(Restore)
        .Executes(() => DotNetFormat(settings => settings
            .SetProject(Solution.Path)
            .EnableVerifyNoChanges()
            .EnableNoRestore()
            .SetProcessWorkingDirectory(RootDirectory)));

    private Target Compile => target => target
        .Description("Builds all product and test projects in the selected configuration.")
        .DependsOn(Restore)
        .Executes(() => DotNetBuild(settings => settings
            .SetProjectFile(Solution.Path)
            .SetConfiguration(Configuration.ToString())
            .EnableNoRestore()
            .EnableNoLogo()
            .SetProcessWorkingDirectory(RootDirectory)));

    private Target Test => target => target
        .Description("Runs all Microsoft Testing Platform tests and requires at least one test.")
        .DependsOn(Compile)
        .Produces(TestResultsDirectory / "*.trx")
        .Executes(() =>
        {
            TestResultsDirectory.CreateOrCleanDirectory();
            DotNetTest(settings => settings
                .SetProjectFile(Solution.Path)
                .SetConfiguration(Configuration.ToString())
                .EnableNoBuild()
                .EnableNoRestore()
                .SetResultsDirectory(TestResultsDirectory)
                .SetProcessAdditionalArguments(
                    "--minimum-expected-tests",
                    "1",
                    "--",
                    "--report-xunit-trx",
                    "--report-xunit-trx-filename",
                    "IX.Modularity.Tests.trx")
                .SetProcessWorkingDirectory(RootDirectory));
        });

    private Target Pack => target => target
        .Description("Creates the package and symbols package.")
        .DependsOn(Compile)
        .Produces(PackagesDirectory / "*.nupkg", PackagesDirectory / "*.snupkg")
        .Executes(() =>
        {
            PackagesDirectory.CreateOrCleanDirectory();
            DotNetPack(settings => settings
                .SetProject(ProductProject.Path)
                .SetConfiguration(Configuration.ToString())
                .EnableNoBuild()
                .EnableNoRestore()
                .EnableNoLogo()
                .SetOutputDirectory(PackagesDirectory)
                .SetProcessWorkingDirectory(RootDirectory));
        });

    private Target InspectPackages => target => target
        .Description("Validates the generated NuGet package contents and metadata.")
        .DependsOn(Pack)
        .Executes(InspectPackage);

    private Target Audit => target => target
        .Description("Reports vulnerable direct and transitive packages.")
        .DependsOn(Restore)
        .Executes(() =>
        {
            IReadOnlyCollection<Output> solutionAudit = DotNet(
                $"package list --project {Solution.Path} --vulnerable --include-transitive --format json --output-version 1 --no-restore",
                workingDirectory: RootDirectory);
            AssertNoVulnerablePackages(solutionAudit, "solution");

            IReadOnlyCollection<Output> buildAudit = DotNet(
                $"package list --project {BuildProject} --vulnerable --include-transitive --format json --output-version 1 --no-restore",
                workingDirectory: RootDirectory);
            AssertNoVulnerablePackages(buildAudit, "build");
        });

    private Target Sbom => target => target
        .Description("Generates a CycloneDX SBOM and rejects an empty result.")
        .DependsOn(Restore)
        .Produces(SbomDirectory / "bom.json")
        .Executes(() =>
        {
            SbomDirectory.CreateOrCleanDirectory();
            DotNet(
                $"CycloneDX {Solution.Path} --output-format Json --output {SbomDirectory} --filename bom.json --disable-package-restore --configuration {Configuration}",
                workingDirectory: RootDirectory);
            AssertSbom();
        });

    private Target Outdated => target => target
        .Description("Reports dependency updates for scheduled maintenance.")
        .DependsOn(Restore)
        .Executes(() => DotNet(
            $"outdated {Solution.Path} --fail-on-updates",
            workingDirectory: RootDirectory));

    private Target Validate => target => target
        .Description("Runs the complete authoritative repository validation.")
        .DependsOn(Format, Test, InspectPackages, Audit, Sbom);

    private Target Publish => target => target
        .Description("Publishes an exactly tagged package to i-x-io GitHub Packages.")
        .DependsOn(Validate)
        .Requires(() => IsServerBuild)
        .Requires(() => PackageToken)
        .Executes(() =>
        {
            GitHubActions? githubActions = GitHubActions.Instance;
            Require(
                githubActions is not null,
                "Publish may run only in GitHub Actions.");
            Require(
                string.Equals(githubActions!.RepositoryOwner, "i-x-io", StringComparison.Ordinal),
                "Publish may run only in the i-x-io GitHub organization.");
            Require(!string.IsNullOrWhiteSpace(PackageToken), "Publish requires --package-token.");

            string tag = GetExactPackageTag();
            string tagVersion = tag[PackageTagPrefix.Length..];
            AbsolutePath package = GetSinglePackage("*.nupkg", excludeSymbols: true);
            string packageVersion = ReadPackageVersion(package);

            Require(
                string.Equals(tagVersion, packageVersion, StringComparison.Ordinal),
                $"Tag version '{tagVersion}' does not match package version '{packageVersion}'.");

            DotNetNuGetPush(settings => settings
                .SetTargetPath(package)
                .SetSource(PackageSource)
                .SetApiKey(PackageToken!)
                .EnableSkipDuplicate()
                .SetProcessWorkingDirectory(RootDirectory));
        });

    private void AssertRepositoryShape()
    {
        Require(File.Exists(Solution.Path), $"Solution '{Solution.Path}' does not exist.");
        Require(File.Exists(ProductProject.Path), $"Product project '{ProductProject.Path}' does not exist.");
        Require(File.Exists(TestProject.Path), $"Test project '{TestProject.Path}' does not exist.");
    }

    private void InspectPackage()
    {
        AbsolutePath package = GetSinglePackage("*.nupkg", excludeSymbols: true);
        _ = GetSinglePackage("*.snupkg", excludeSymbols: false);

        using ZipArchive archive = ZipFile.OpenRead(package);
        var entries = archive.Entries.Select(entry => entry.FullName).ToHashSet(StringComparer.Ordinal);

        Require(entries.Contains("README.md"), "Package README.md is missing.");
        Require(entries.Contains("LICENSE"), "Package LICENSE is missing.");
        Require(entries.Contains($"lib/net10.0/{PackageId}.dll"), "Package library is missing.");
        Require(entries.Contains($"lib/net10.0/{PackageId}.xml"), "Package XML documentation is missing.");

        ZipArchiveEntry nuspecEntry = archive.Entries.Single(entry => entry.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using Stream nuspecStream = nuspecEntry.Open();
        var nuspec = XDocument.Load(nuspecStream);
        XNamespace ns = nuspec.Root?.Name.Namespace ?? XNamespace.None;
        XElement metadata = nuspec.Descendants(ns + "metadata").Single();

        Require(string.Equals((string?)metadata.Element(ns + "id"), PackageId, StringComparison.Ordinal), "Unexpected package ID.");
        Require(
            string.Equals(
                (string?)metadata.Element(ns + "repository")?.Attribute("url"),
                "https://github.com/i-x-io/modular-base",
                StringComparison.Ordinal),
            "Unexpected package repository URL.");

        string[] dependencies = [.. metadata
            .Descendants(ns + "dependency")
            .Select(element => (string?)element.Attribute("id"))
            .Where(id => id is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)];
        Require(
            dependencies.SequenceEqual(["Microsoft.Extensions.DependencyInjection.Abstractions"], StringComparer.OrdinalIgnoreCase),
            $"Unexpected runtime dependencies: {string.Join(", ", dependencies)}");

        Log.Information("Validated package {Package} at version {Version}", package, ReadPackageVersion(package));
    }

    private void AssertSbom()
    {
        string[] candidates = Directory.GetFiles(SbomDirectory, "*.json", SearchOption.AllDirectories);
        Require(candidates.Length == 1, $"Expected one JSON SBOM but found {candidates.Length}.");

        using var document = JsonDocument.Parse(File.ReadAllText(candidates[0]));
        Require(
            document.RootElement.TryGetProperty("components", out JsonElement components)
            && components.ValueKind == JsonValueKind.Array
            && components.GetArrayLength() > 0,
            "The generated SBOM contains no components.");
    }

    private static void AssertNoVulnerablePackages(IReadOnlyCollection<Output> output, string graphName)
    {
        string json = string.Join(Environment.NewLine, output.Select(line => line.Text));
        using var document = JsonDocument.Parse(json);
        int findingCount = document.RootElement
            .GetProperty("projects")
            .EnumerateArray()
            .SelectMany(project => project.GetProperty("frameworks").EnumerateArray())
            .Sum(framework => CountPackages(framework, "topLevelPackages")
                + CountPackages(framework, "transitivePackages"));

        Require(findingCount == 0, $"The {graphName} dependency graph contains {findingCount} vulnerable package(s).");
    }

    private static int CountPackages(JsonElement framework, string propertyName) =>
        framework.TryGetProperty(propertyName, out JsonElement packages)
        && packages.ValueKind == JsonValueKind.Array
            ? packages.GetArrayLength()
            : 0;

    private AbsolutePath GetSinglePackage(string pattern, bool excludeSymbols)
    {
        string[] packages = [.. Directory
            .GetFiles(PackagesDirectory, pattern, SearchOption.TopDirectoryOnly)
            .Where(path => !excludeSymbols || !path.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))];
        Require(packages.Length == 1, $"Expected one '{pattern}' package but found {packages.Length}.");
        return packages[0];
    }

    private static string ReadPackageVersion(AbsolutePath package)
    {
        using ZipArchive archive = ZipFile.OpenRead(package);
        ZipArchiveEntry nuspecEntry = archive.Entries.Single(entry => entry.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using Stream nuspecStream = nuspecEntry.Open();
        var nuspec = XDocument.Load(nuspecStream);
        XNamespace ns = nuspec.Root?.Name.Namespace ?? XNamespace.None;
        return nuspec.Descendants(ns + "version").Single().Value;
    }

    private string GetExactPackageTag()
    {
        string[] matchingTags = [.. Repository.Tags
            .Select(tag => tag.Trim())
            .Where(tag => PackageTagPattern().IsMatch(tag))];
        Require(matchingTags.Length == 1, "HEAD must have exactly one IX.Modularity semantic-version tag.");
        return matchingTags[0];
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    [GeneratedRegex(
        "^IX\\.Modularity-v(?:0|[1-9][0-9]*)\\.(?:0|[1-9][0-9]*)\\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?(?:\\+[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex PackageTagPattern();
}
