using ModularBase.Build.Repository;
using Octokit;

namespace ModularBase.Build.Release;

internal sealed class GitHubReleaseClient(RepositoryIdentity repository)
{
    private readonly RepositoryIdentity _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    public async Task EnsureRemoteTagAsync(string tag, string commit, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        ArgumentException.ThrowIfNullOrWhiteSpace(commit);
        GitHubClient client = GitHubClientFactory.Create(token);
        try
        {
            Reference existing = await client.Git.Reference
                .Get(_repository.Owner, _repository.Name, "tags/" + tag)
                .ConfigureAwait(false);
            if (!string.Equals(existing.Object.Sha, commit, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Tag '{tag}' already targets '{existing.Object.Sha}', not '{commit}'.");
            }
        }
        catch (NotFoundException)
        {
            _ = await client.Git.Reference.Create(
                    _repository.Owner,
                    _repository.Name,
                    new NewReference("refs/tags/" + tag, commit))
                .ConfigureAwait(false);
        }
    }

    public async Task ReconcileReleaseAsync(
        ReleasePlan plan,
        IReadOnlyCollection<string> assetPaths,
        string token)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(assetPaths);
        string[] duplicateNames = [.. assetPaths
            .GroupBy(GetAssetName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Skip(1).Any())
            .Select(group => group.Key)];
        if (duplicateNames.Length != 0)
        {
            throw new InvalidOperationException(
                $"Release asset names must be unique: {string.Join(", ", duplicateNames)}.");
        }

        GitHubClient client = GitHubClientFactory.Create(token);
        Octokit.Release release = await GetOrCreateReleaseAsync(client, plan).ConfigureAwait(false);
        IReadOnlyList<ReleaseAsset> existingAssets = await client.Repository.Release
            .GetAllAssets(_repository.Owner, _repository.Name, release.Id)
            .ConfigureAwait(false);
        var desiredNames = assetPaths
            .Select(GetAssetName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (ReleaseAsset asset in existingAssets.Where(
            asset => desiredNames.Contains(asset.Name)))
        {
            await client.Repository.Release
                .DeleteAsset(_repository.Owner, _repository.Name, asset.Id)
                .ConfigureAwait(false);
        }

        foreach (string path in assetPaths.Order(StringComparer.Ordinal))
        {
            FileStream stream = File.OpenRead(path);
            await using (stream.ConfigureAwait(false))
            {
                var upload = new ReleaseAssetUpload(
                    GetAssetName(path),
                    GetMediaType(path),
                    stream,
                    TimeSpan.FromMinutes(5));
                _ = await client.Repository.Release
                    .UploadAsset(release, upload, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        foreach (ReleaseAsset asset in existingAssets.Where(
            asset => !desiredNames.Contains(asset.Name)))
        {
            await client.Repository.Release
                .DeleteAsset(_repository.Owner, _repository.Name, asset.Id)
                .ConfigureAwait(false);
        }
    }

    private async Task<Octokit.Release> GetOrCreateReleaseAsync(
        GitHubClient client,
        ReleasePlan plan)
    {
        try
        {
            Octokit.Release existing = await client.Repository.Release
                .Get(_repository.Owner, _repository.Name, plan.Tag)
                .ConfigureAwait(false);
            var update = new ReleaseUpdate
            {
                TagName = plan.Tag,
                TargetCommitish = plan.Commit,
                Name = GetReleaseName(plan),
                Draft = false,
                Prerelease = plan.IsPrerelease,
                MakeLatest = plan.IsPrerelease
                    ? MakeLatestQualifier.False
                    : MakeLatestQualifier.True,
            };
            return await client.Repository.Release
                .Edit(_repository.Owner, _repository.Name, existing.Id, update)
                .ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            var create = new NewRelease(plan.Tag)
            {
                TargetCommitish = plan.Commit,
                Name = GetReleaseName(plan),
                Draft = false,
                Prerelease = plan.IsPrerelease,
                GenerateReleaseNotes = true,
                MakeLatest = plan.IsPrerelease
                    ? MakeLatestQualifier.False
                    : MakeLatestQualifier.True,
            };
            return await client.Repository.Release
                .Create(_repository.Owner, _repository.Name, create)
                .ConfigureAwait(false);
        }
    }

    private static string GetReleaseName(ReleasePlan plan)
    {
        return plan.Version + (plan.IsPrerelease ? " (prerelease)" : string.Empty);
    }

    private static string GetAssetName(string path)
    {
        string name = Path.GetFileName(path);
        return string.IsNullOrWhiteSpace(name)
            ? throw new InvalidOperationException($"Release asset path '{path}' has no file name.")
            : name;
    }

    private static string GetMediaType(string path)
    {
        return Path.GetExtension(path).ToUpperInvariant() switch
        {
            ".JSON" => "application/json",
            ".TXT" => "text/plain",
            _ => "application/octet-stream",
        };
    }
}
