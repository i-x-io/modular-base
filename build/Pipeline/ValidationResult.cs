using ModularBase.Build.Validation;

namespace ModularBase.Build.Pipeline;

internal sealed record ValidationResult(
    IReadOnlyList<PackageInspection> Packages,
    IReadOnlyList<string> SbomPaths);
