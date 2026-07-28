using Octokit;

namespace ModularBase.Build.Release;

internal static class GitHubClientFactory
{
    public static GitHubClient Create(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return new(new ProductHeaderValue("modular-base-build"))
        {
            Credentials = new Credentials(token),
        };
    }
}
