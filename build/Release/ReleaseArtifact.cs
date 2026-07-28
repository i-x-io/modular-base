namespace ModularBase.Build.Release;

internal sealed record ReleaseArtifact(
    string RelativePath,
    string MediaType,
    long Length,
    string Sha256);
