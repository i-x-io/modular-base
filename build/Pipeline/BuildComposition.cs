using ModularBase.Build.Release;
using ModularBase.Build.Repository;
using ModularBase.Build.Tooling;
using ModularBase.Build.Validation;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;

namespace ModularBase.Build.Pipeline;

internal static class BuildComposition
{
    public static BuildPipeline Create(
        BuildPaths paths,
        Solution solution,
        GitRepository gitRepository,
        GitHubActions? githubActions,
        BuildParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(gitRepository);
        ArgumentNullException.ThrowIfNull(parameters);

        BuildPolicy policy = BuildPolicy.Default;
        var identity = RepositoryIdentity.From(gitRepository);
        var repository = RepositoryModel.Create(
            paths,
            solution,
            identity,
            policy.Release.TagPrefix);
        var toolchain = ToolchainVersions.Read(paths.RootDirectory);
        var environment = new BuildEnvironment(paths, identity, repository, toolchain, policy);
        var commands = new CommandRunner(paths.RootDirectory);
        var dotNet = new DotNetToolchain(paths, policy.Validation);
        var packages = new PackageValidator(paths);
        var dependencies = new DependencyValidator(paths, commands);
        var githubRelease = new GitHubReleaseClient(identity);
        var pullRequests = new GitHubPullRequestClient(identity);
        var releasePolicy = new ReleasePolicy(policy.Release);
        var publisher = new ReleasePublisher(
            paths,
            repository,
            identity,
            dotNet,
            commands,
            packages,
            releasePolicy,
            new ReleaseEvidenceWriter(paths, toolchain),
            githubRelease);
        return new(
            environment,
            parameters,
            githubActions,
            dotNet,
            packages,
            dependencies,
            new RepositoryValidator(commands),
            pullRequests,
            publisher);
    }
}
