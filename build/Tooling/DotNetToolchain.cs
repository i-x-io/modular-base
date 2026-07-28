using System.Globalization;
using ModularBase.Build.Pipeline;
using ModularBase.Build.Repository;
using ModularBase.Build.Validation;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace ModularBase.Build.Tooling;

internal sealed class DotNetToolchain(BuildPaths paths, ValidationPolicy validationPolicy)
{
    private readonly BuildPaths _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    private readonly ValidationPolicy _validationPolicy = validationPolicy
        ?? throw new ArgumentNullException(nameof(validationPolicy));

    public void Restore(IReadOnlyCollection<BuildUnit> units, bool updateLocks)
    {
        ArgumentNullException.ThrowIfNull(units);
        _ = DotNetToolRestore(settings => settings.SetProcessWorkingDirectory(_paths.RootDirectory));
        foreach (BuildUnit unit in units)
        {
            _ = DotNetRestore(settings =>
            {
                settings = settings
                    .SetProjectFile(unit.Path)
                    .SetProcessWorkingDirectory(_paths.RootDirectory);
                return updateLocks
                    ? settings
                        .EnableForceEvaluate()
                        .DisableLockedMode()
                        .SetProperty(k: "RestoreLockedMode", v: false)
                    : settings.EnableLockedMode();
            });
        }
    }

    public void Format(IReadOnlyCollection<BuildUnit> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        foreach (BuildUnit unit in units)
        {
            _ = DotNetFormat(settings => settings
                .SetProject(unit.Path)
                .EnableVerifyNoChanges()
                .EnableNoRestore()
                .SetProcessWorkingDirectory(_paths.RootDirectory));
        }
    }

    public void Test(IReadOnlyCollection<BuildUnit> units, BuildConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(units);
        foreach (BuildUnit unit in units)
        {
            _ = DotNetBuild(settings => settings
                .SetProjectFile(unit.Path)
                .SetConfiguration(configuration.ToString())
                .EnableNoRestore()
                .EnableNoLogo()
                .SetProcessWorkingDirectory(_paths.RootDirectory));
        }

        foreach (BuildUnit unit in units)
        {
            AbsolutePath resultsDirectory = _paths.GetTestResultsDirectory(unit);
            _ = resultsDirectory.CreateOrCleanDirectory();
            _ = DotNetTest(settings => settings
                .SetProjectFile(unit.Path)
                .SetConfiguration(configuration.ToString())
                .EnableNoBuild()
                .EnableNoRestore()
                .SetResultsDirectory(resultsDirectory)
                .SetProcessAdditionalArguments(
                    "--minimum-expected-tests",
                    _validationPolicy.MinimumExpectedTests.ToString(CultureInfo.InvariantCulture),
                    "--",
                    "--report-xunit-trx",
                    "--report-xunit-trx-filename",
                    $"{unit.ArtifactName}.trx")
                .SetProcessWorkingDirectory(_paths.RootDirectory));
        }
    }

    public void Pack(
        IReadOnlyCollection<PackageProject> projects,
        BuildConfiguration configuration,
        bool rebuild)
    {
        ArgumentNullException.ThrowIfNull(projects);
        _ = _paths.PackagesDirectory.CreateOrCleanDirectory();
        foreach (PackageProject project in projects)
        {
            _ = DotNetPack(settings =>
            {
                settings = settings
                    .SetProject(project.ProjectFile)
                    .SetConfiguration(configuration.ToString())
                    .EnableNoRestore()
                    .EnableNoLogo()
                    .SetOutputDirectory(_paths.PackagesDirectory)
                    .SetProcessWorkingDirectory(_paths.RootDirectory);
                return rebuild ? settings : settings.EnableNoBuild();
            });
        }
    }

    public void Publish(IReadOnlyCollection<string> packageFiles, Uri source, string token)
    {
        ArgumentNullException.ThrowIfNull(packageFiles);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        foreach (string packageFile in packageFiles)
        {
            _ = DotNetNuGetPush(settings => settings
                .SetTargetPath(packageFile)
                .SetSource(source.AbsoluteUri)
                .SetApiKey(token)
                .EnableSkipDuplicate()
                .SetProcessWorkingDirectory(_paths.RootDirectory));
        }
    }
}
