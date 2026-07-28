using ModularBase.Build.Release;
using ModularBase.Build.Validation;

namespace ModularBase.Build.Tests;

public sealed class BuildPolicyTests
{
    [Fact]
    public void DefinesEnterpriseDefaultsInSource()
    {
        BuildPolicy policy = BuildPolicy.Default;

        Assert.Equal(1, policy.Validation.MinimumExpectedTests);
        Assert.Equal("RELEASE:", policy.Release.StableTitlePrefix);
        Assert.Equal("v", policy.Release.TagPrefix);
        Assert.Contains("release", policy.Validation.AllowedPullRequestTypes);
        Assert.Equal(2, ReleasePlan.CurrentSchemaVersion);
        Assert.Equal(2, ReleaseManifest.CurrentSchemaVersion);
    }
}
