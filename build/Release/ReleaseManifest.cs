namespace ModularBase.Build.Release;

internal sealed record ReleaseManifest(
    int SchemaVersion,
    string Repository,
    string Commit,
    string Tag,
    string Configuration,
    string SdkVersion,
    string NukeVersion,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ReleasePackage> Packages,
    IReadOnlyList<ReleaseArtifact> Artifacts)
{
    public const int CurrentSchemaVersion = 2;
}
