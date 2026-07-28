using ModularBase.Build.Repository;
using Nuke.Common.IO;

namespace ModularBase.Build.Tests;

public sealed class BuildPathsTests
{
    [Fact]
    public void DerivesOwnedPathsFromTheConfiguredArtifactRoot()
    {
        AbsolutePath root = Path.Combine(Path.GetTempPath(), "repository");
        AbsolutePath artifacts = root / "custom-artifacts";

        var paths = BuildPaths.Create(root, artifacts);
        var input = new BuildUnit("Architecture tests", "architecture", root / "test.csproj");

        Assert.Equal(root / "build" / "_build.csproj", paths.BuildProject);
        Assert.Equal(artifacts / "packages", paths.PackagesDirectory);
        Assert.Equal(
            artifacts / "test-results" / "architecture",
            paths.GetTestResultsDirectory(input));
        Assert.Equal(
            artifacts / "release-evidence" / "release-manifest.json",
            paths.ReleaseManifest);
        Assert.Equal(artifacts / "release" / "release-plan.json", paths.ReleasePlan);
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("Product")]
    [InlineData("two/segments")]
    public void RejectsUnsafeArtifactNames(string artifactName)
    {
        AbsolutePath project = Path.Combine(Path.GetTempPath(), "test.csproj");

        _ = Assert.Throws<ArgumentException>(
            () => new BuildUnit("Tests", artifactName, project));
    }

    [Fact]
    public void RejectsAnArtifactRootThatContainsTheBuildHost()
    {
        AbsolutePath root = Path.Combine(Path.GetTempPath(), "repository");

        _ = Assert.Throws<ArgumentException>(() => BuildPaths.Create(root, root));
    }

    [Fact]
    public void RejectsAnArtifactRootOutsideTheRepository()
    {
        AbsolutePath root = Path.Combine(Path.GetTempPath(), "repository");
        AbsolutePath artifacts = Path.Combine(Path.GetTempPath(), "other-artifacts");

        _ = Assert.Throws<ArgumentException>(() => BuildPaths.Create(root, artifacts));
    }
}
