using ModularBase.Build.Validation;
using Nuke.Common;

namespace ModularBase.Build;

internal sealed partial class Build
{
    public static int Main()
    {
        return Execute<Build>(build => build.CI);
    }

    private Target Restore => target => target
        .Description("Restores tools and every managed project graph from committed locks.")
        .Executes(() => Pipeline.Restore());

    private Target Test => target => target
        .Description("Compiles the repository and runs every managed test suite.")
        .DependsOn(Restore)
        .Produces(Paths.TestResultsDirectory / "**" / "*.trx")
        .Executes(() => Pipeline.Test());

    private Target CI => target => target
        .Description("Runs the complete pull-request and repository quality gate.")
        .DependsOn(Test)
        .Produces(
            Paths.PackagesDirectory / "*.nupkg",
            Paths.PackagesDirectory / "*.snupkg",
            Paths.SbomDirectory / "**" / "*.cdx.json")
        .Executes(() => Pipeline.CIAsync());

    private Target UpdatePackageCatalog => target => target
        .Description("Regenerates the machine-readable package catalog from central package versions and guides.")
        .Produces(Paths.RootDirectory / "eng" / "package-catalog.json")
        .Executes(() => PackageCatalogFile.Write(Paths.RootDirectory));

    private Target PrepareRelease => target => target
        .Description("Plans, tags, validates, and evidences immutable release inputs without publishing them.")
        .DependsOn(CI)
        .Requires(() => IsServerBuild)
        .Requires(() => GitHubToken)
        .Produces(
            Paths.ReleasePlan,
            Paths.ReleaseManifest,
            Paths.Checksums)
        .Executes(() => Pipeline.PrepareReleaseAsync());
}
