# Microsoft.CodeAnalysis.PublicApiAnalyzers

## Catalog entry

`Microsoft.CodeAnalysis.PublicApiAnalyzers` **5.6.0** — project-scoped catalog analyzer; packable projects opt in and own their public API baseline files.

## Decision and scope

Use only for packable libraries where public API compatibility is a release contract. It is intentionally not a repository-wide global analyzer and must not be added to non-packable applications by default.

## Recommended registration and use

The repository's `Directory.Build.targets` conditionally references it when `IsPackable=true`, with `PrivateAssets=all` and analyzer assets. Every packable project must own `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`; the same targets add existing files as `AdditionalFiles` and fail `Pack` when either baseline is missing.

## Enterprise implementation guidance

Review API-baseline changes as compatibility changes. Add intended new APIs to the unshipped file, move released APIs to shipped at release, and treat removals or signature changes as deliberate versioning decisions. Keep baselines adjacent to their package project; do not centralize unrelated package APIs.

## Integration with the catalog

This opt-in differs from the universal `GlobalPackageReference` analyzers documented in the other analyzer files. The pack policy is implemented in `Directory.Build.targets`; common severity policy is in `ModularBase.globalconfig` and `.editorconfig`.

## Security, performance, AOT, trimming, and operations

The analyzer runs only at compile time and does not flow to package consumers under the current private-assets configuration. Its operational risk is release friction from unreviewed baseline changes; it has no runtime, AOT, or trimming effect.

## Avoid

Do not suppress a baseline mismatch merely to ship, add it to non-packable projects without a compatibility contract, or edit shipped baselines to conceal breaking changes.

## Verification checklist

- Confirm every future packable project has both baseline files.
- Build and pack after an intentional public API addition and review the unshipped baseline.
- Validate a breaking API change triggers the analyzer before release.
- Confirm the analyzer is absent from non-packable projects unless explicitly approved.

## Sources

- https://www.nuget.org/packages/Microsoft.CodeAnalysis.PublicApiAnalyzers/5.6.0 (Accessed 2026-07-27)
- https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/overview (Accessed 2026-07-27)
