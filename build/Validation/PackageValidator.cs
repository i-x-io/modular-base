using System.IO.Compression;
using System.Xml.Linq;
using ModularBase.Build.Repository;
using NuGet.Versioning;
using Nuke.Common.IO;

namespace ModularBase.Build.Validation;

internal sealed class PackageValidator(BuildPaths paths)
{
    private readonly BuildPaths _paths = paths ?? throw new ArgumentNullException(nameof(paths));

    public IReadOnlyList<PackageInspection> InspectAll(
        IReadOnlyCollection<PackageProject> projects,
        string expectedCommit)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedCommit);
        Dictionary<string, AbsolutePath> packageFiles = IndexBinaryPackages();
        PackageInspection[] inspections = [.. projects
            .OrderBy(project => project.PackageId, StringComparer.Ordinal)
            .Select(project => Inspect(project, expectedCommit, packageFiles))];
        EnsureNoUnexpectedPackages(inspections);
        EnsureLockstepVersion(inspections);
        return inspections;
    }

    private static PackageInspection Inspect(
        PackageProject project,
        string expectedCommit,
        Dictionary<string, AbsolutePath> packageFiles)
    {
        AbsolutePath package = packageFiles.TryGetValue(project.PackageId, out AbsolutePath? candidate)
            ? candidate
            : throw new InvalidDataException(
                $"Expected one binary package for '{project.PackageId}' but found 0.");
        AbsolutePath symbols = GetSymbolsPackage(package);
        using ZipArchive archive = ZipFile.OpenRead(package);
        ValidateContents(archive, project);
        NuGetVersion version = ReadMetadata(archive, project, expectedCommit);
        return new(project, package, symbols, version);
    }

    private static void ValidateContents(ZipArchive archive, PackageProject project)
    {
        var entries = archive.Entries
            .Select(entry => entry.FullName)
            .ToHashSet(StringComparer.Ordinal);
        Require(entries.Contains("README.md"), $"Package '{project.PackageId}' README.md is missing.");
        Require(entries.Contains("LICENSE"), $"Package '{project.PackageId}' LICENSE is missing.");
        foreach (string framework in project.TargetFrameworks)
        {
            Require(
                entries.Contains($"lib/{framework}/{project.AssemblyName}.dll"),
                $"Package '{project.PackageId}' library for {framework} is missing.");
            Require(
                entries.Contains($"lib/{framework}/{project.AssemblyName}.xml"),
                $"Package '{project.PackageId}' XML documentation for {framework} is missing.");
        }
    }

    private static NuGetVersion ReadMetadata(
        ZipArchive archive,
        PackageProject project,
        string expectedCommit)
    {
        ZipArchiveEntry nuspecEntry = archive.Entries.Single(
            entry => entry.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using Stream stream = nuspecEntry.Open();
        var nuspec = XDocument.Load(stream);
        XNamespace xmlNamespace = nuspec.Root?.Name.Namespace ?? XNamespace.None;
        XElement metadata = nuspec.Descendants(xmlNamespace + "metadata").Single();
        Require(
            string.Equals(
                (string?)metadata.Element(xmlNamespace + "id"),
                project.PackageId,
                StringComparison.Ordinal),
            $"Package '{project.PackageId}' has an unexpected package ID.");

        XElement repository = metadata.Element(xmlNamespace + "repository")
            ?? throw new InvalidDataException(
                $"Package '{project.PackageId}' repository metadata is missing.");
        string repositoryUrl = (string?)repository.Attribute("url")
            ?? throw new InvalidDataException(
                $"Package '{project.PackageId}' repository URL is missing.");
        Require(
            string.Equals(
                RepositoryIdentity.NormalizeRepositoryUrl(repositoryUrl),
                project.RepositoryUrl,
                StringComparison.OrdinalIgnoreCase),
            $"Package '{project.PackageId}' has an unexpected repository URL.");
        string repositoryCommit = (string?)repository.Attribute("commit")
            ?? throw new InvalidDataException(
                $"Package '{project.PackageId}' repository commit is missing.");
        Require(
            string.Equals(repositoryCommit, expectedCommit, StringComparison.OrdinalIgnoreCase),
            $"Package '{project.PackageId}' commit '{repositoryCommit}' does not match HEAD '{expectedCommit}'.");

        string[] dependencies = [.. metadata
            .Descendants(xmlNamespace + "dependency")
            .Select(element => (string?)element.Attribute("id"))
            .Where(id => id is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];
        Require(
            dependencies.SequenceEqual(project.RuntimeDependencies, StringComparer.OrdinalIgnoreCase),
            $"Package '{project.PackageId}' runtime dependencies differ from the evaluated project: "
            + string.Join(", ", dependencies));

        string versionText = metadata.Element(xmlNamespace + "version")?.Value
            ?? throw new InvalidDataException($"Package '{project.PackageId}' version is missing.");
        return NuGetVersion.TryParse(versionText, out NuGetVersion? version)
            ? version
            : throw new InvalidDataException(
                $"Package '{project.PackageId}' version '{versionText}' is not valid semantic versioning.");
    }

    private Dictionary<string, AbsolutePath> IndexBinaryPackages()
    {
        string[] files = [.. Directory
            .GetFiles(_paths.PackagesDirectory, "*.nupkg", SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))];
        ILookup<string, string> byPackageId = files.ToLookup(
            ReadPackageId,
            StringComparer.Ordinal);
        IGrouping<string, string>? duplicate = byPackageId.FirstOrDefault(group => group.Skip(1).Any());
        return duplicate is null
            ? byPackageId.ToDictionary(
                group => group.Key,
                group => (AbsolutePath)group.Single(),
                StringComparer.Ordinal)
            : throw new InvalidDataException(
                $"Expected one binary package for '{duplicate.Key}' but found {duplicate.Count()}.");
    }

    private static AbsolutePath GetSymbolsPackage(AbsolutePath binaryPackage)
    {
        string binaryPath = binaryPackage;
        string symbolsPackage = binaryPath[..^".nupkg".Length] + ".snupkg";
        return File.Exists(symbolsPackage)
            ? (AbsolutePath)symbolsPackage
            : throw new InvalidDataException(
                $"Symbols package '{symbolsPackage}' does not exist.");
    }

    private static string ReadPackageId(string packagePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        ZipArchiveEntry nuspecEntry = archive.Entries.Single(
            entry => entry.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using Stream stream = nuspecEntry.Open();
        var nuspec = XDocument.Load(stream);
        XNamespace xmlNamespace = nuspec.Root?.Name.Namespace ?? XNamespace.None;
        return nuspec.Descendants(xmlNamespace + "metadata")
            .Single()
            .Element(xmlNamespace + "id")?.Value
            ?? throw new InvalidDataException($"Package '{packagePath}' has no package ID.");
    }

    private void EnsureNoUnexpectedPackages(IEnumerable<PackageInspection> inspections)
    {
        var expected = inspections
            .SelectMany(inspection => new[]
            {
                Path.GetFullPath(inspection.PackageFile),
                Path.GetFullPath(inspection.SymbolsPackageFile),
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        string[] unexpected = [.. Directory
            .GetFiles(_paths.PackagesDirectory, "*.*nupkg", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFullPath)
            .Where(path => !expected.Contains(path))];
        if (unexpected.Length != 0)
        {
            throw new InvalidDataException(
                $"Unexpected package artifacts: {string.Join(", ", unexpected)}.");
        }
    }

    private static void EnsureLockstepVersion(IReadOnlyCollection<PackageInspection> inspections)
    {
        string[] versions = [.. inspections
            .Select(inspection => inspection.Version.ToNormalizedString())
            .Distinct(StringComparer.Ordinal)];
        if (versions.Length != 1)
        {
            throw new InvalidDataException(
                $"Packable projects must use one lockstep version: {string.Join(", ", versions)}.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }
}
