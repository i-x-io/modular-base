using System.IO.Compression;
using System.Text;
using ModularBase.Build.Repository;
using ModularBase.Build.Validation;

namespace ModularBase.Build.Tests;

public sealed class PackageValidatorTests
{
    [Fact]
    public void InspectsEveryExpectedPackageInLockstep()
    {
        string root = CreateDirectory();
        try
        {
            var paths = BuildPaths.Create(root, Path.Combine(root, "artifacts"));
            Directory.CreateDirectory(paths.PackagesDirectory);
            PackageProject first = Project(root, "First");
            PackageProject second = Project(root, "First.Extensions");
            CreatePackage(paths, first, "1.2.3", "abc123");
            CreatePackage(paths, second, "1.2.3", "abc123");

            IReadOnlyList<PackageInspection> result = new PackageValidator(paths)
                .InspectAll([first, second], "abc123");

            Assert.Equal(
                ["First", "First.Extensions"],
                result.Select(item => item.Project.PackageId),
                StringComparer.Ordinal);
            Assert.All(result, item => Assert.Equal("1.2.3", item.Version.ToNormalizedString()));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RejectsPackagesThatDoNotUseOneVersion()
    {
        string root = CreateDirectory();
        try
        {
            var paths = BuildPaths.Create(root, Path.Combine(root, "artifacts"));
            Directory.CreateDirectory(paths.PackagesDirectory);
            PackageProject first = Project(root, "First");
            PackageProject second = Project(root, "Second");
            CreatePackage(paths, first, "1.2.3", "abc123");
            CreatePackage(paths, second, "1.2.4", "abc123");

            InvalidDataException exception = Assert.Throws<InvalidDataException>(
                () => new PackageValidator(paths).InspectAll([first, second], "abc123"));

            Assert.Contains("lockstep version", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RejectsPackageMetadataFromAnotherCommit()
    {
        string root = CreateDirectory();
        try
        {
            var paths = BuildPaths.Create(root, Path.Combine(root, "artifacts"));
            Directory.CreateDirectory(paths.PackagesDirectory);
            PackageProject project = Project(root, "Package");
            CreatePackage(paths, project, "1.2.3", "old");

            InvalidDataException exception = Assert.Throws<InvalidDataException>(
                () => new PackageValidator(paths).InspectAll([project], "current"));

            Assert.Contains("does not match HEAD", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static PackageProject Project(string root, string packageId)
    {
        return new(
            Path.Combine(root, "src", packageId + ".csproj"),
            packageId,
            packageId,
            "https://example.test/repository",
            "v",
            ["net10.0"],
            ["Microsoft.Extensions.DependencyInjection.Abstractions"]);
    }

    private static void CreatePackage(
        BuildPaths paths,
        PackageProject project,
        string version,
        string repositoryCommit)
    {
        string packageFile = paths.PackagesDirectory / $"{project.PackageId}.{version}.nupkg";
        string symbolsPackage = paths.PackagesDirectory / $"{project.PackageId}.{version}.snupkg";
        File.WriteAllText(symbolsPackage, "symbols");
        using ZipArchive archive = ZipFile.Open(packageFile, ZipArchiveMode.Create);
        WriteEntry(archive, "README.md", "readme");
        WriteEntry(archive, "LICENSE", "license");
        WriteEntry(archive, $"lib/net10.0/{project.AssemblyName}.dll", "assembly");
        WriteEntry(archive, $"lib/net10.0/{project.AssemblyName}.xml", "documentation");
        string nuspec = "<package xmlns=\"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd\">"
            + $"<metadata><id>{project.PackageId}</id><version>{version}</version>"
            + $"<repository type=\"git\" url=\"{project.RepositoryUrl}\" commit=\"{repositoryCommit}\" />"
            + "<dependencies><group targetFramework=\"net10.0\">"
            + "<dependency id=\"Microsoft.Extensions.DependencyInjection.Abstractions\" version=\"[10.0.0,)\" />"
            + "</group></dependencies></metadata></package>";
        WriteEntry(archive, project.PackageId + ".nuspec", nuspec);
    }

    private static void WriteEntry(ZipArchive archive, string name, string contents)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name);
        using Stream stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(contents);
    }

    private static string CreateDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"modular-base-package-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
