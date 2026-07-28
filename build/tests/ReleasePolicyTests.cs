using ModularBase.Build.Release;
using ModularBase.Build.Repository;
using ModularBase.Build.Validation;
using NuGet.Versioning;

namespace ModularBase.Build.Tests;

public sealed class ReleasePolicyTests
{
    private readonly ReleasePolicy _policy = new(ReleaseConvention.Default);

    [Fact]
    public void PlansAutomaticMergesAsLockstepPrereleases()
    {
        ReleasePlan plan = _policy.CreatePlan(
            [Package("First", "1.2.3-preview.0.7"), Package("Second", "1.2.3-preview.0.7")],
            PullRequest("feat: add modules"),
            "abc123");

        Assert.True(plan.IsPrerelease);
        Assert.Equal("1.2.3-preview.0.7", plan.Version);
        Assert.Equal("v1.2.3-preview.0.7", plan.Tag);
        Assert.Equal(
            ["First", "Second"],
            plan.Packages.Select(package => package.PackageId),
            StringComparer.Ordinal);
    }

    [Fact]
    public void PromotesTheMinVerCandidateForAStableReleaseTitle()
    {
        ReleasePlan plan = _policy.CreatePlan(
            [Package("Package", "1.2.3-preview.0.7")],
            PullRequest("RELEASE: publish the validated package"),
            "abc123");

        Assert.False(plan.IsPrerelease);
        Assert.Equal("1.2.3", plan.Version);
        Assert.Equal("v1.2.3", plan.Tag);
    }

    [Theory]
    [InlineData("release: publish")]
    [InlineData("RELEASE:")]
    [InlineData("RELEASE:   ")]
    [InlineData("RELEASE:publish")]
    public void TreatsOnlyAnExactUppercasePrefixWithADescriptionAsStable(string title)
    {
        Assert.False(ReleasePolicy.IsStableTitle(title, ReleaseConvention.Default));
    }

    [Fact]
    public void RejectsAnAutomaticReleaseWhenMinVerProducesAStableVersion()
    {
        _ = Assert.Throws<InvalidOperationException>(() => _policy.CreatePlan(
            [Package("Package", "1.2.3")],
            PullRequest("fix: example"),
            "abc123"));
    }

    [Fact]
    public void RejectsDifferentCandidateVersions()
    {
        _ = Assert.Throws<InvalidOperationException>(() => _policy.CreatePlan(
            [Package("First", "1.2.3-preview.0.1"), Package("Second", "1.2.3-preview.0.2")],
            PullRequest("feat: example"),
            "abc123"));
    }

    private static PackageInspection Package(string packageId, string version)
    {
        var project = new PackageProject(
            "/tmp/" + packageId + ".csproj",
            packageId,
            packageId,
            "https://example.test/repository",
            "v",
            ["net10.0"],
            []);
        return new(
            project,
            "/tmp/" + packageId + ".nupkg",
            "/tmp/" + packageId + ".snupkg",
            NuGetVersion.Parse(version));
    }

    private static PullRequestContext PullRequest(string title)
    {
        return new(
            123,
            title,
            "Closes #123",
            "feat/123-example",
            "i-x-io/modular-base",
            "i-x-io/modular-base",
            "contributor");
    }
}
