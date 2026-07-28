using ModularBase.Build.Release;
using ModularBase.Build.Tooling;
using ModularBase.Build.Validation;
using Nuke.Common.CI.GitHubActions;
using Serilog;

namespace ModularBase.Build.Pipeline;

internal sealed class BuildPipeline(
    BuildEnvironment environment,
    BuildParameters parameters,
    GitHubActions? githubActions,
    DotNetToolchain dotNet,
    PackageValidator packages,
    DependencyValidator dependencies,
    RepositoryValidator repositoryValidator,
    GitHubPullRequestClient pullRequests,
    ReleasePublisher publisher)
{
    private readonly BuildParameters _parameters = parameters
        ?? throw new ArgumentNullException(nameof(parameters));
    private readonly GitHubActions? _githubActions = githubActions;
    private readonly DotNetToolchain _dotNet = dotNet ?? throw new ArgumentNullException(nameof(dotNet));
    private readonly PackageValidator _packages = packages
        ?? throw new ArgumentNullException(nameof(packages));
    private readonly DependencyValidator _dependencies = dependencies
        ?? throw new ArgumentNullException(nameof(dependencies));
    private readonly RepositoryValidator _repositoryValidator = repositoryValidator
        ?? throw new ArgumentNullException(nameof(repositoryValidator));
    private readonly GitHubPullRequestClient _pullRequests = pullRequests
        ?? throw new ArgumentNullException(nameof(pullRequests));
    private readonly ReleasePublisher _publisher = publisher
        ?? throw new ArgumentNullException(nameof(publisher));
    private ValidationResult? _validationResult;

    public BuildEnvironment Environment
    {
        get;
    } = environment
        ?? throw new ArgumentNullException(nameof(environment));

    public void Restore()
    {
        _dotNet.Restore(Environment.Repository.RepositoryUnits, _parameters.UpdateLocks);
    }

    public void Test()
    {
        _dotNet.Test(Environment.Repository.TestUnits, _parameters.Configuration);
    }

    public async Task CIAsync()
    {
        PullRequestContext? pullRequest = PullRequestContextResolver.Resolve(
            _githubActions,
            Environment.Identity);
        if (pullRequest is not null)
        {
            EnforcePullRequestPolicy(pullRequest);
        }

        _dotNet.Format(Environment.Repository.RepositoryUnits);
        _dotNet.Pack(
            Environment.Repository.PackageProjects,
            _parameters.Configuration,
            rebuild: false);
        IReadOnlyList<PackageInspection> packageInspections = _packages.InspectAll(
            Environment.Repository.PackageProjects,
            Environment.Identity.Commit);
        await _dependencies.AuditAsync(Environment.Repository.RepositoryUnits).ConfigureAwait(false);
        IReadOnlyList<string> sboms = await _dependencies.GenerateSbomsAsync(
            packageInspections,
            _parameters.Configuration).ConfigureAwait(false);
        await _repositoryValidator.ValidateAsync().ConfigureAwait(false);
        _validationResult = new(packageInspections, sboms);
        Log.Information(
            "Validated {PackageCount} packable project(s)",
            packageInspections.Count);
    }

    public async Task PrepareReleaseAsync()
    {
        GitHubActions githubContext = _githubActions
            ?? throw new InvalidOperationException("PrepareRelease may run only in GitHub Actions.");
        if (!string.Equals(githubContext.EventName, "push", StringComparison.Ordinal)
            || !string.Equals(githubContext.Ref, "refs/heads/main", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("PrepareRelease may run only for a push to main.");
        }

        Environment.Identity.RequireGitHubRepository(
            githubContext.Repository,
            githubContext.RepositoryOwner);
        if (string.IsNullOrWhiteSpace(_parameters.GitHubToken))
        {
            throw new InvalidOperationException(
                "GITHUB_TOKEN is required to resolve the merged pull request.");
        }

        string githubToken = _parameters.GitHubToken;
        ValidationResult validation = _validationResult
            ?? throw new InvalidOperationException("PrepareRelease requires a completed CI validation.");
        PullRequestContext pullRequest = await _pullRequests.ResolveMergedAsync(
            Environment.Identity.Commit,
            githubToken).ConfigureAwait(false);
        EnforcePullRequestPolicy(pullRequest);
        await _publisher.PrepareAsync(
            validation.Packages,
            pullRequest,
            _parameters.Configuration,
            TimeProvider.System.GetUtcNow()).ConfigureAwait(false);
    }

    private void EnforcePullRequestPolicy(PullRequestContext pullRequest)
    {
        IReadOnlyList<PolicyViolation> violations = PullRequestPolicy.Validate(
            pullRequest,
            Environment.Policy);
        if (violations.Count != 0)
        {
            throw new InvalidOperationException(string.Join(
                System.Environment.NewLine,
                violations.Select(violation => $"{violation.RuleId}: {violation.Message}")));
        }
    }
}
