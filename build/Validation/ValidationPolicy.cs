using System.Collections.Immutable;

namespace ModularBase.Build.Validation;

internal sealed record ValidationPolicy
{
    private ValidationPolicy(int minimumExpectedTests, IEnumerable<string> allowedPullRequestTypes)
    {
        MinimumExpectedTests = minimumExpectedTests;
        AllowedPullRequestTypes = allowedPullRequestTypes.ToImmutableHashSet(StringComparer.Ordinal);
    }

    public int MinimumExpectedTests
    {
        get;
    }

    public ImmutableHashSet<string> AllowedPullRequestTypes
    {
        get;
    }

    public static ValidationPolicy Default
    {
        get;
    } = new(
        1,
        [
            "build", "chore", "ci", "docs", "feat", "fix", "perf", "refactor",
            "release", "revert", "style", "test",
        ]);
}
