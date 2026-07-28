using Nuke.Common.ProjectModel;

namespace ModularBase.Build.Repository;

[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
internal sealed record RepositoryModel
{
    private RepositoryModel(
        BuildUnit solution,
        IReadOnlyList<BuildUnit> repositoryUnits,
        IReadOnlyList<BuildUnit> testUnits,
        IReadOnlyList<PackageProject> packageProjects)
    {
        Solution = solution;
        RepositoryUnits = repositoryUnits;
        TestUnits = testUnits;
        PackageProjects = packageProjects;
    }

    public BuildUnit Solution
    {
        get;
    }

    public IReadOnlyList<BuildUnit> RepositoryUnits
    {
        get;
    }

    public IReadOnlyList<BuildUnit> TestUnits
    {
        get;
    }

    public IReadOnlyList<PackageProject> PackageProjects
    {
        get;
    }

    public static RepositoryModel Create(
        BuildPaths paths,
        Solution solution,
        RepositoryIdentity identity,
        string expectedTagPrefix)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedTagPrefix);
        ValidateFile(solution.Path, "solution");
        ValidateFile(paths.BuildProject, "build project");
        ValidateFile(paths.BuildTestProject, "build test project");

        var solutionUnit = new BuildUnit("Solution", "solution", solution.Path);
        var buildUnit = new BuildUnit("Build infrastructure", "build", paths.BuildProject);
        var buildTestsUnit = new BuildUnit(
            "Build infrastructure tests",
            "build-tests",
            paths.BuildTestProject);
        IReadOnlyList<PackageProject> packages = ProjectDiscovery.FindPackageProjects(solution);
        ValidatePackages(packages, identity, expectedTagPrefix);
        return new(
            solutionUnit,
            [solutionUnit, buildUnit, buildTestsUnit],
            [solutionUnit, buildTestsUnit],
            packages);
    }

    private static void ValidatePackages(
        IReadOnlyCollection<PackageProject> packages,
        RepositoryIdentity identity,
        string expectedTagPrefix)
    {
        PackageProject? mismatched = packages.FirstOrDefault(
            package => !identity.MatchesRepositoryUrl(package.RepositoryUrl));
        if (mismatched is not null)
        {
            throw new InvalidOperationException(
                $"Package '{mismatched.PackageId}' repository URL does not match '{identity.HttpsUrl}'.");
        }

        if (packages.Any(package => !string.Equals(
                package.TagPrefix,
                expectedTagPrefix,
                StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Every packable project must use the repository tag prefix '{expectedTagPrefix}'.");
        }
    }

    private static void ValidateFile(string path, string description)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"The repository {description} '{path}' does not exist.");
        }
    }
}
