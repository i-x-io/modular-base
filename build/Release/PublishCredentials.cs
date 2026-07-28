namespace ModularBase.Build.Release;

internal sealed record PublishCredentials
{
    private PublishCredentials(string githubToken, string releaseToken)
    {
        GitHubToken = githubToken;
        ReleaseToken = releaseToken;
    }

    public string GitHubToken
    {
        get;
    }

    public string ReleaseToken
    {
        get;
    }

    public static PublishCredentials Create(string? githubToken, string? releaseToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(githubToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseToken);
        return new(githubToken, releaseToken);
    }
}
