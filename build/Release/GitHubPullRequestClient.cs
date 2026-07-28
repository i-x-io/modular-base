using ModularBase.Build.Repository;
using ModularBase.Build.Validation;
using Octokit;

namespace ModularBase.Build.Release;

internal sealed class GitHubPullRequestClient(RepositoryIdentity repository)
{
    private readonly RepositoryIdentity _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    public async Task<PullRequestContext> ResolveMergedAsync(string commit, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commit);
        GitHubClient client = GitHubClientFactory.Create(token);
        IReadOnlyList<CommitPullRequest> candidates = await client.Repository.Commit
            .PullRequests(_repository.Owner, _repository.Name, commit)
            .ConfigureAwait(false);
        CommitPullRequest[] matches = [.. candidates.Where(pullRequest =>
            pullRequest.MergedAt is not null
            && string.Equals(pullRequest.Base.Ref, "main", StringComparison.Ordinal)
            && string.Equals(
                pullRequest.Base.Repository.FullName,
                _repository.Identifier,
                StringComparison.Ordinal))];
        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                $"Commit '{commit}' must be associated with exactly one merged pull request into main; "
                + $"found {matches.Length}.");
        }

        CommitPullRequest match = matches[0];
        return new(
            match.Number,
            match.Title,
            match.Body ?? string.Empty,
            match.Head.Ref,
            match.Head.Repository.FullName,
            match.Base.Repository.FullName,
            match.User.Login);
    }
}
