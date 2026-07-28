namespace ModularBase.Build.Release;

internal sealed record ReleasePlan(
    int SchemaVersion,
    string Commit,
    string Version,
    string Tag,
    bool IsPrerelease,
    string SourceTitle,
    IReadOnlyList<ReleasePlanPackage> Packages)
{
    public const int CurrentSchemaVersion = 2;
}
