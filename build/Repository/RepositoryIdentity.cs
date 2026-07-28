using Nuke.Common.Git;

namespace ModularBase.Build.Repository;

internal sealed record RepositoryIdentity(
    string Endpoint,
    string Owner,
    string Name,
    string Identifier,
    string HttpsUrl,
    string Commit,
    Uri PackageSource)
{
    public static RepositoryIdentity From(GitRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        string[] identifierParts = repository.Identifier.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (identifierParts.Length != 2)
        {
            throw new InvalidOperationException(
                $"Repository identifier '{repository.Identifier}' must use the '<owner>/<name>' form.");
        }

        RequireValue(repository.Endpoint, "Git endpoint");
        RequireValue(repository.HttpsUrl, "Git HTTPS URL");
        RequireValue(repository.Commit, "Git commit");
        string owner = identifierParts[0];
        return new(
            repository.Endpoint,
            owner,
            identifierParts[1],
            string.Join('/', identifierParts),
            NormalizeRepositoryUrl(repository.HttpsUrl),
            repository.Commit,
            new Uri($"https://nuget.pkg.github.com/{owner}/index.json", UriKind.Absolute));
    }

    public void RequireGitHubRepository(string repository, string owner)
    {
        if (!string.Equals(Endpoint, "github.com", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Identifier, repository, StringComparison.Ordinal)
            || !string.Equals(Owner, owner, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The local repository and GitHub Actions repository identity do not match.");
        }
    }

    public bool MatchesRepositoryUrl(string repositoryUrl)
    {
        return string.Equals(
            HttpsUrl,
            NormalizeRepositoryUrl(repositoryUrl),
            StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeRepositoryUrl(string repositoryUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryUrl);
        return repositoryUrl.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? repositoryUrl[..^4].TrimEnd('/')
            : repositoryUrl.TrimEnd('/');
    }

    private static void RequireValue(string value, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{description} is required.");
        }
    }
}
