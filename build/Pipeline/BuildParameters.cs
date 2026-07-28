namespace ModularBase.Build.Pipeline;

internal sealed record BuildParameters(
    BuildConfiguration Configuration,
    bool UpdateLocks,
    string? GitHubToken,
    string? ReleaseToken);
