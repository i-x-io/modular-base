using Nuke.Common.IO;

namespace ModularBase.Build.Repository;

internal sealed record PackageProject(
    AbsolutePath ProjectFile,
    string PackageId,
    string AssemblyName,
    string RepositoryUrl,
    string TagPrefix,
    IReadOnlyList<string> TargetFrameworks,
    IReadOnlyList<string> RuntimeDependencies);
