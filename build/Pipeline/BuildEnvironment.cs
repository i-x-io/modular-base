using ModularBase.Build.Repository;
using ModularBase.Build.Tooling;
using ModularBase.Build.Validation;

namespace ModularBase.Build.Pipeline;

internal sealed record BuildEnvironment(
    BuildPaths Paths,
    RepositoryIdentity Identity,
    RepositoryModel Repository,
    ToolchainVersions Toolchain,
    BuildPolicy Policy);
