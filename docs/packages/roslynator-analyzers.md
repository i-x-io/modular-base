# Roslynator.Analyzers

## Catalog entry

`Roslynator.Analyzers` **4.15.0** — universal catalog analyzer supplied through a shared `GlobalPackageReference` with private analyzer assets.

## Decision and scope

Use as a broad supplementary static-analysis set for future C# projects. It is not a formatter, a replacement for SDK analyzers, or a reason to introduce a custom analyzer for existing rule coverage.

## Recommended registration and use

Keep the repository's global package reference. Enable, suppress, or set severity for individual diagnostics in `.editorconfig`; use `ModularBase.globalconfig` for repository-wide analyzer defaults. Review suggested code fixes for semantic impact before bulk application.

## Enterprise implementation guidance

Adopt rules in small, reviewable batches. Prefer diagnostic IDs over category-wide suppression, preserve public behavior during refactors, and test generated-code exclusions and source-generator output in actual projects before setting strict severity.

## Integration with the catalog

Uses the same global analyzer integration as `meziantou-analyzer.md` and `sonaranalyzer-csharp.md`. SDK analyzer settings are in `Directory.Build.props`; `.editorconfig` overrides global analyzer configuration for matching files.

## Security, performance, AOT, trimming, and operations

Analyzer and code-fix execution can increase IDE/build work; keep version upgrades deliberate. Private analyzer assets prevent transitive consumer flow. It has no application runtime, NativeAOT, or trimming impact.

## Avoid

Do not accept bulk code fixes without tests, use it to enforce formatting that the existing `.editorconfig` already owns, or add a local package reference to override central management.

## Verification checklist

- Build a representative C# project and review new diagnostic IDs.
- Test semantic behavior after any analyzer-proposed code fix.
- Confirm `.editorconfig` overrides are scoped to the intended files.
- Review central-version upgrade changes before enabling warnings as errors.

## Sources

- https://www.nuget.org/packages/Roslynator.Analyzers/4.15.0 (Accessed 2026-07-27)
- https://josefpihrt.github.io/docs/roslynator/ (Accessed 2026-07-27)
- https://github.com/JosefPihrt/Roslynator (Accessed 2026-07-27)
