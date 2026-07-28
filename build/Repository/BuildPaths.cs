using Nuke.Common.IO;

namespace ModularBase.Build.Repository;

internal sealed record BuildPaths
{
    private BuildPaths(AbsolutePath rootDirectory, AbsolutePath artifactsDirectory)
    {
        RootDirectory = rootDirectory;
        ArtifactsDirectory = artifactsDirectory;
        AbsolutePath buildDirectory = rootDirectory / "build";
        BuildProject = buildDirectory / "_build.csproj";
        BuildTestProject = buildDirectory / "tests" / "ModularBase.Build.Tests.csproj";
        PackagesDirectory = artifactsDirectory / "packages";
        TestResultsDirectory = artifactsDirectory / "test-results";
        SbomDirectory = artifactsDirectory / "sbom";
        ReleaseDirectory = artifactsDirectory / "release";
        ReleasePlan = ReleaseDirectory / "release-plan.json";
        ReleaseEvidenceDirectory = artifactsDirectory / "release-evidence";
        ReleaseManifest = ReleaseEvidenceDirectory / "release-manifest.json";
        Checksums = ReleaseEvidenceDirectory / "SHA256SUMS";
    }

    public AbsolutePath RootDirectory
    {
        get;
    }

    public AbsolutePath BuildProject
    {
        get;
    }

    public AbsolutePath ArtifactsDirectory
    {
        get;
    }

    public AbsolutePath BuildTestProject
    {
        get;
    }

    public AbsolutePath PackagesDirectory
    {
        get;
    }

    public AbsolutePath TestResultsDirectory
    {
        get;
    }

    public AbsolutePath SbomDirectory
    {
        get;
    }

    public AbsolutePath ReleaseDirectory
    {
        get;
    }

    public AbsolutePath ReleasePlan
    {
        get;
    }

    public AbsolutePath ReleaseEvidenceDirectory
    {
        get;
    }

    public AbsolutePath ReleaseManifest
    {
        get;
    }

    public AbsolutePath Checksums
    {
        get;
    }

    public static BuildPaths Create(AbsolutePath rootDirectory, AbsolutePath artifactsDirectory)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);
        ArgumentNullException.ThrowIfNull(artifactsDirectory);
        EnsureOwnedArtifactsDirectory(rootDirectory, artifactsDirectory);
        return new(rootDirectory, artifactsDirectory);
    }

    public AbsolutePath GetTestResultsDirectory(BuildUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return TestResultsDirectory / unit.ArtifactName;
    }

    private static void EnsureOwnedArtifactsDirectory(
        AbsolutePath rootDirectory,
        AbsolutePath artifactsDirectory)
    {
        string root = Path.GetFullPath(rootDirectory);
        string artifacts = Path.GetFullPath(artifactsDirectory);
        string relativeArtifacts = Path.GetRelativePath(root, artifacts);
        bool isDescendant = !string.Equals(relativeArtifacts, ".", StringComparison.Ordinal)
            && !string.Equals(relativeArtifacts, "..", StringComparison.Ordinal)
            && !relativeArtifacts.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !Path.IsPathRooted(relativeArtifacts);
        if (!isDescendant)
        {
            throw new ArgumentException(
                "The artifact directory must be a child of the repository root.",
                nameof(artifactsDirectory));
        }

        string buildDirectory = Path.GetFullPath(rootDirectory / "build");
        string buildFromArtifacts = Path.GetRelativePath(artifacts, buildDirectory);
        bool containsBuild = string.Equals(buildFromArtifacts, ".", StringComparison.Ordinal)
            || (!string.Equals(buildFromArtifacts, "..", StringComparison.Ordinal)
                && !buildFromArtifacts.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        if (containsBuild)
        {
            throw new ArgumentException(
                "The artifact directory must not contain the NUKE build host.",
                nameof(artifactsDirectory));
        }
    }
}
