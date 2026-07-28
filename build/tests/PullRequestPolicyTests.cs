using ModularBase.Build.Validation;

namespace ModularBase.Build.Tests;

public sealed class PullRequestPolicyTests
{
    [Theory]
    [InlineData("feat: add modules")]
    [InlineData("fix(api): preserve order")]
    [InlineData("refactor!: remove legacy behavior")]
    public void AcceptsConventionalTitles(string title)
    {
        PullRequestContext context = ValidContext() with
        {
            Title = title,
        };

        IReadOnlyList<PolicyViolation> violations = PullRequestPolicy.Validate(
            context,
            BuildPolicy.Default);

        Assert.DoesNotContain(
            violations,
            violation => string.Equals(violation.RuleId, "PR001", StringComparison.Ordinal));
    }

    [Fact]
    public void AcceptsStableReleaseTitleOnAReleaseBranch()
    {
        PullRequestContext context = ValidContext() with
        {
            Title = "RELEASE: publish the validated packages",
            HeadBranch = "release/123-stable-packages",
        };

        Assert.Empty(PullRequestPolicy.Validate(context, BuildPolicy.Default));
    }

    [Fact]
    public void ReturnsEveryApplicableViolation()
    {
        PullRequestContext context = ValidContext() with
        {
            Title = "invalid title",
            HeadBranch = "feature/zero",
            Body = "No issue is linked.",
        };

        IReadOnlyList<PolicyViolation> violations = PullRequestPolicy.Validate(
            context,
            BuildPolicy.Default);

        Assert.Equal(
            ["PR001", "PR002", "PR003"],
            violations.Select(item => item.RuleId),
            StringComparer.Ordinal);
    }

    [Fact]
    public void ExemptsBotsFromBranchAndIssuePolicyOnly()
    {
        PullRequestContext context = ValidContext() with
        {
            Author = "dependabot[bot]",
            HeadBranch = "dependabot/nuget/example",
            Body = string.Empty,
        };

        Assert.Empty(PullRequestPolicy.Validate(context, BuildPolicy.Default));
    }

    private static PullRequestContext ValidContext()
    {
        return new(
            123,
            "feat(api): add explicit modules",
            "Closes #123",
            "feat/123-explicit-modules",
            "i-x-io/modular-base",
            "i-x-io/modular-base",
            "contributor");
    }
}
