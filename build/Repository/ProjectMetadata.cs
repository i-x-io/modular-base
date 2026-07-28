using Nuke.Common.IO;

namespace ModularBase.Build.Repository;

internal sealed record ProjectMetadata(
    AbsolutePath ProjectFile,
    bool IsPackable,
    string PackageId,
    string AssemblyName,
    string RepositoryUrl,
    string TagPrefix,
    string TargetFrameworks,
    string TargetFramework,
    IReadOnlyList<string> RuntimeDependencies);
