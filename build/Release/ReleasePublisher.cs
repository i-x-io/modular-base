using ModularBase.Build.Pipeline;
using ModularBase.Build.Repository;
using ModularBase.Build.Tooling;
using ModularBase.Build.Validation;
using Serilog;

namespace ModularBase.Build.Release;

internal sealed class ReleasePublisher(
    BuildPaths paths,
    RepositoryModel repository,
    RepositoryIdentity identity,
    DotNetToolchain dotNet,
    CommandRunner commands,
    PackageValidator packageValidator,
    ReleasePolicy releasePolicy,
    ReleaseEvidenceWriter evidence,
    GitHubReleaseClient github)
{
    private readonly BuildPaths _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    private readonly RepositoryModel _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly RepositoryIdentity _identity = identity
        ?? throw new ArgumentNullException(nameof(identity));
    private readonly DotNetToolchain _dotNet = dotNet ?? throw new ArgumentNullException(nameof(dotNet));
    private readonly CommandRunner _commands = commands
        ?? throw new ArgumentNullException(nameof(commands));
    private readonly PackageValidator _packageValidator = packageValidator
        ?? throw new ArgumentNullException(nameof(packageValidator));
    private readonly ReleasePolicy _releasePolicy = releasePolicy
        ?? throw new ArgumentNullException(nameof(releasePolicy));
    private readonly ReleaseEvidenceWriter _evidence = evidence
        ?? throw new ArgumentNullException(nameof(evidence));
    private readonly GitHubReleaseClient _github = github
        ?? throw new ArgumentNullException(nameof(github));

    public async Task PublishAsync(
        IReadOnlyCollection<PackageInspection> candidatePackages,
        PullRequestContext pullRequest,
        PublishCredentials credentials,
        BuildConfiguration configuration,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(candidatePackages);
        ArgumentNullException.ThrowIfNull(pullRequest);
        ArgumentNullException.ThrowIfNull(credentials);

        ReleasePlan plan = _releasePolicy.CreatePlan(
            candidatePackages,
            pullRequest,
            _identity.Commit);
        _evidence.WritePlan(plan);

        _ = await _commands.RunAsync("git", ["tag", "--force", plan.Tag, plan.Commit]).ConfigureAwait(false);
        _dotNet.Pack(_repository.PackageProjects, configuration, rebuild: true);
        IReadOnlyList<PackageInspection> taggedPackages = _packageValidator.InspectAll(
            _repository.PackageProjects,
            _identity.Commit);
        ReleasePolicy.ValidateTaggedPackages(plan, taggedPackages);

        string[] evidenceInputs = [
            .. taggedPackages.SelectMany(package => new[]
            {
                (string)package.PackageFile,
                (string)package.SymbolsPackageFile,
            }),
            .. Directory.GetFiles(_paths.SbomDirectory, "*.json", SearchOption.AllDirectories),
            _paths.ReleasePlan,
        ];
        _ = _evidence.CreateManifest(
            plan,
            taggedPackages,
            evidenceInputs,
            _identity,
            configuration,
            createdAtUtc);

        await _github.EnsureRemoteTagAsync(plan.Tag, plan.Commit, credentials.ReleaseToken)
            .ConfigureAwait(false);
        _dotNet.Publish(
            [.. taggedPackages.Select(package => (string)package.PackageFile)],
            _identity.PackageSource,
            credentials.GitHubToken);

        string[] releaseAssets = [
            .. evidenceInputs.Where(path => !string.Equals(
                path,
                _paths.ReleasePlan,
                StringComparison.OrdinalIgnoreCase)),
            _paths.ReleasePlan,
            _paths.ReleaseManifest,
            _paths.Checksums,
        ];
        await _github.ReconcileReleaseAsync(plan, releaseAssets, credentials.ReleaseToken)
            .ConfigureAwait(false);
        Log.Information(
            "Published {PackageCount} package(s) and reconciled GitHub release {Tag}",
            taggedPackages.Count,
            plan.Tag);
    }
}
