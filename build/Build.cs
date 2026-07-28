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
            Paths.SbomDirectory / "bom.json")
        .Executes(() => Pipeline.CIAsync());

    private Target Publish => target => target
        .Description("Plans, tags, validates, evidences, and publishes the merged package release.")
        .DependsOn(CI, Test)
        .Requires(() => IsServerBuild)
        .Requires(() => GitHubToken)
        .Requires(() => ReleaseToken)
        .Produces(
            Paths.ReleasePlan,
            Paths.ReleaseManifest,
            Paths.Checksums)
        .Executes(() => Pipeline.PublishAsync());
}
