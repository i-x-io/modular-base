namespace ModularBase.Build.Release;

internal sealed record ReleasePackage(
    string PackageId,
    string Version,
    string ProjectFile,
    IReadOnlyList<string> TargetFrameworks);
