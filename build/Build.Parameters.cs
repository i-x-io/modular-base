using ModularBase.Build.Pipeline;
using ModularBase.Build.Repository;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;

namespace ModularBase.Build;

internal sealed partial class Build : NukeBuild
{
    [Solution("IX.Modularity.slnx")]
    private readonly Solution Solution = null!;

    [GitRepository]
    private readonly GitRepository Repository = null!;

    [Parameter("Build configuration. Defaults to Release.")]
    private readonly BuildConfiguration Configuration = BuildConfiguration.Release;

    [Parameter("Regenerates package locks instead of enforcing locked restore.")]
    private readonly bool UpdateLocks;

    [Parameter("Short-lived GitHub token used only to read the merged pull request.")]
    [Secret]
    private readonly string? GitHubToken;

    private BuildPaths Paths => field ??= BuildPaths.Create(
        RootDirectory,
        RootDirectory / "artifacts");

    private BuildPipeline Pipeline => field ??= BuildComposition.Create(
        Paths,
        Solution,
        Repository,
        GitHubActions.Instance,
        new(Configuration, UpdateLocks, GitHubToken));
}
