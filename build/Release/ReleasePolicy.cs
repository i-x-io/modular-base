using ModularBase.Build.Validation;
using NuGet.Versioning;

namespace ModularBase.Build.Release;

internal sealed class ReleasePolicy(ReleaseConvention convention)
{
    private readonly ReleaseConvention _convention = convention
        ?? throw new ArgumentNullException(nameof(convention));

    public ReleasePlan CreatePlan(
        IReadOnlyCollection<PackageInspection> packages,
        PullRequestContext pullRequest,
        string commit)
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(pullRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(commit);
        if (packages.Count == 0)
        {
            throw new InvalidOperationException("A release requires at least one package.");
        }

        NuGetVersion candidate = packages.First().Version;
        if (packages.Any(package => !VersionComparer.VersionReleaseMetadata.Equals(
                package.Version,
                candidate)))
        {
            throw new InvalidOperationException("Release packages must use one lockstep version.");
        }

        bool stable = IsStableTitle(pullRequest.Title, _convention);
        NuGetVersion version = stable ? ToStableVersion(candidate) : RequirePrerelease(candidate);
        string normalizedVersion = version.ToNormalizedString();
        return new(
            ReleasePlan.CurrentSchemaVersion,
            commit,
            normalizedVersion,
            _convention.TagPrefix + normalizedVersion,
            !stable,
            pullRequest.Title.Trim(),
            [.. packages
                .OrderBy(package => package.Project.PackageId, StringComparer.Ordinal)
                .Select(package => new ReleasePlanPackage(
                    package.Project.PackageId,
                    normalizedVersion))]);
    }

    public static void ValidateTaggedPackages(
        ReleasePlan plan,
        IReadOnlyCollection<PackageInspection> packages)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(packages);
        string[] actualPackageIds = [.. packages
            .OrderBy(package => package.Project.PackageId, StringComparer.Ordinal)
            .Select(package => package.Project.PackageId)];
        string[] plannedPackageIds = [.. plan.Packages
            .OrderBy(package => package.PackageId, StringComparer.Ordinal)
            .Select(package => package.PackageId)];
        if (!actualPackageIds.SequenceEqual(plannedPackageIds, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Tagged packages do not match the release plan.");
        }

        if (packages.Any(package => !string.Equals(
                package.Version.ToNormalizedString(),
                plan.Version,
                StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Every tagged package must have release version '{plan.Version}'.");
        }
    }

    public static bool IsStableTitle(string title, ReleaseConvention convention)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(convention);
        return title.StartsWith(convention.StableTitlePrefix, StringComparison.Ordinal)
            && title.Length > convention.StableTitlePrefix.Length
            && char.IsWhiteSpace(title[convention.StableTitlePrefix.Length])
            && !string.IsNullOrWhiteSpace(title[convention.StableTitlePrefix.Length..]);
    }

    private static NuGetVersion ToStableVersion(NuGetVersion version)
    {
        return version.IsPrerelease
            ? new NuGetVersion(version.Major, version.Minor, version.Patch)
            : version;
    }

    private static NuGetVersion RequirePrerelease(NuGetVersion version)
    {
        return version.IsPrerelease
            ? version
            : throw new InvalidOperationException(
                $"Automatic releases must be prereleases, but MinVer produced '{version}'.");
    }
}
