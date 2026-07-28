using System.Text.Json;
using ModularBase.Build.Repository;
using Nuke.Common.CI.GitHubActions;

namespace ModularBase.Build.Validation;

internal static class PullRequestContextResolver
{
    public static PullRequestContext? Resolve(
        GitHubActions? github,
        RepositoryIdentity repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        if (github is null
            || !string.Equals(github.EventName, "pull_request", StringComparison.Ordinal))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(github.EventPath));
        JsonElement pullRequest = document.RootElement.GetProperty("pull_request");
        string baseRepository = ReadString(pullRequest, "base", "repo", "full_name");
        return !string.Equals(baseRepository, repository.Identifier, StringComparison.Ordinal)
            ? throw new InvalidOperationException(
                $"Pull request base repository '{baseRepository}' does not match '{repository.Identifier}'.")
            : new(
            pullRequest.GetProperty("number").GetInt32(),
            ReadString(pullRequest, "title"),
            pullRequest.GetProperty("body").GetString() ?? string.Empty,
            ReadString(pullRequest, "head", "ref"),
            ReadString(pullRequest, "head", "repo", "full_name"),
            baseRepository,
            ReadString(pullRequest, "user", "login"));
    }

    private static string ReadString(JsonElement element, params string[] path)
    {
        foreach (string part in path)
        {
            element = element.GetProperty(part);
        }

        return element.GetString()
            ?? throw new InvalidDataException($"GitHub event property '{string.Join('.', path)}' is null.");
    }
}
