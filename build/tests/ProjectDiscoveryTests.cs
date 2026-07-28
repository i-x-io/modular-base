using ModularBase.Build.Repository;
using Nuke.Common.IO;

namespace ModularBase.Build.Tests;

public sealed class ProjectDiscoveryTests
{
    [Fact]
    public void SelectsAllPackableProjectsInDeterministicOrder()
    {
        ProjectMetadata[] projects =
        [
            Project("test/Test.csproj", isPackable: false),
            Project("src/Second.csproj", isPackable: true),
            Project("src/First.csproj", isPackable: true),
        ];

        IReadOnlyList<PackageProject> result = ProjectDiscovery.SelectPackageProjects(projects);

        Assert.Equal(
            ["First", "Second"],
            result.Select(project => project.PackageId),
            StringComparer.Ordinal);
        Assert.All(result, project => Assert.Equal(["net10.0", "net9.0"], project.TargetFrameworks));
    }

    [Fact]
    public void RejectsARepositoryWithoutAPackableProject()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ProjectDiscovery.SelectPackageProjects([
                Project("test/Test.csproj", isPackable: false),
            ]));

        Assert.Contains("at least one packable", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDuplicatePackageIds()
    {
        ProjectMetadata first = Project("src/First.csproj", isPackable: true) with
        {
            PackageId = "Shared",
        };
        ProjectMetadata second = Project("src/Second.csproj", isPackable: true) with
        {
            PackageId = "Shared",
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ProjectDiscovery.SelectPackageProjects([first, second]));

        Assert.Contains("Package IDs must be unique", exception.Message, StringComparison.Ordinal);
    }

    private static ProjectMetadata Project(string path, bool isPackable)
    {
        AbsolutePath project = Path.Combine(Path.GetTempPath(), path);
        string packageId = Path.GetFileNameWithoutExtension(path);
        return new(
            project,
            isPackable,
            packageId,
            packageId,
            "https://example.test/repository",
            "v",
            "net10.0;net9.0",
            string.Empty,
            ["Runtime.Dependency"]);
    }
}
