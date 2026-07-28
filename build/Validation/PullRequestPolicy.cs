using System.Text.RegularExpressions;
using ModularBase.Build.Release;

namespace ModularBase.Build.Validation;

internal static partial class PullRequestPolicy
{
    public static IReadOnlyList<PolicyViolation> Validate(
        PullRequestContext context,
        BuildPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);
        var violations = new List<PolicyViolation>();
        ValidateTitle(context.Title, policy, violations);

        if (!context.Author.EndsWith("[bot]", StringComparison.OrdinalIgnoreCase))
        {
            Match branch = BranchPattern.Match(context.HeadBranch);
            if (!branch.Success
                || !policy.Validation.AllowedPullRequestTypes.Contains(branch.Groups["type"].Value))
            {
                violations.Add(new(
                    "PR002",
                    $"Branch '{context.HeadBranch}' must follow <type>/<issue>-<lowercase-description>."));
            }

            if (!IssuePattern.IsMatch(context.Body))
            {
                violations.Add(new(
                    "PR003",
                    "The pull request body must link an issue with 'Closes #123' or an equivalent closing keyword."));
            }
        }

        return violations;
    }

    private static void ValidateTitle(
        string title,
        BuildPolicy policy,
        List<PolicyViolation> violations)
    {
        if (ReleasePolicy.IsStableTitle(title, policy.Release))
        {
            return;
        }

        Match match = TitlePattern.Match(title);
        if (!match.Success
            || !policy.Validation.AllowedPullRequestTypes.Contains(match.Groups["type"].Value))
        {
            violations.Add(new(
                "PR001",
                "The pull request title must be a Conventional Commit using an approved type and a non-empty description."));
        }
    }

    [GeneratedRegex(
        "^(?<type>[a-z]+)(?:\\([a-z0-9][a-z0-9._/-]*\\))?!?: [^\\s].*$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex TitlePattern
    {
        get;
    }

    [GeneratedRegex(
        "^(?<type>[a-z]+)/[1-9][0-9]*-[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex BranchPattern
    {
        get;
    }

    [GeneratedRegex(
        "(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)\\s+#[1-9][0-9]*",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex IssuePattern
    {
        get;
    }
}
