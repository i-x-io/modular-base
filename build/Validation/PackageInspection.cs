using ModularBase.Build.Repository;
using NuGet.Versioning;
using Nuke.Common.IO;

namespace ModularBase.Build.Validation;

internal sealed record PackageInspection(
    PackageProject Project,
    AbsolutePath PackageFile,
    AbsolutePath SymbolsPackageFile,
    NuGetVersion Version);
