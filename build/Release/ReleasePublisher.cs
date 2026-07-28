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
    DependencyValidator dependencyValidator,
    ReleasePolicy releasePolicy,
    ReleaseEvidenceWriter evidence)
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
    private readonly DependencyValidator _dependencyValidator = dependencyValidator
        ?? throw new ArgumentNullException(nameof(dependencyValidator));
    private readonly ReleasePolicy _releasePolicy = releasePolicy
        ?? throw new ArgumentNullException(nameof(releasePolicy));
    private readonly ReleaseEvidenceWriter _evidence = evidence
        ?? throw new ArgumentNullException(nameof(evidence));
    public async Task PrepareAsync(
        IReadOnlyCollection<PackageInspection> candidatePackages,
        PullRequestContext pullRequest,
        BuildConfiguration configuration,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(candidatePackages);
        ArgumentNullException.ThrowIfNull(pullRequest);

        ReleasePlan plan = _releasePolicy.CreatePlan(
            candidatePackages,
            pullRequest,
            _identity.Commit);
        _evidence.WritePlan(plan);

        _ = await _commands.RunAsync("git", ["tag", plan.Tag, plan.Commit]).ConfigureAwait(false);
        _dotNet.Pack(_repository.PackageProjects, configuration, rebuild: true);
        IReadOnlyList<PackageInspection> taggedPackages = _packageValidator.InspectAll(
            _repository.PackageProjects,
            _identity.Commit);
        ReleasePolicy.ValidateTaggedPackages(plan, taggedPackages);
        IReadOnlyList<string> sboms = await _dependencyValidator.GenerateSbomsAsync(
            taggedPackages,
            configuration).ConfigureAwait(false);

        string[] evidenceInputs = [
            .. taggedPackages.SelectMany(package => new[]
            {
                (string)package.PackageFile,
                (string)package.SymbolsPackageFile,
            }),
            .. sboms,
            _paths.ReleasePlan,
        ];
        _ = _evidence.CreateManifest(
            plan,
            taggedPackages,
            evidenceInputs,
            _identity,
            configuration,
            createdAtUtc);

        Log.Information(
            "Prepared {PackageCount} immutable package(s) and release evidence for {Tag}",
            taggedPackages.Count,
            plan.Tag);
    }
}
