# Meziantou.Analyzer

## Catalog entry

`Meziantou.Analyzer` **3.0.132** — universal catalog analyzer supplied through a shared `GlobalPackageReference` with private analyzer assets.

## Decision and scope

Use as a repository-wide supplementary quality analyzer. The catalog's global reference means future projects inherit it without local `PackageReference` duplication; it is not a production dependency and no custom analyzer is required.

## Recommended registration and use

Keep the existing `GlobalPackageReference` metadata unchanged: `PrivateAssets=all` and analyzer/build asset inclusion. Configure individual diagnostic severity in `.editorconfig` or repository-wide settings in `ModularBase.globalconfig`; use a project-local `.editorconfig` only when the scope genuinely differs.

## Enterprise implementation guidance

Adopt rules deliberately: establish a baseline, promote high-value diagnostics progressively, and document justified scoped suppressions. Treat analyzer-version upgrades as build behavior changes and test them across all project types before updating the central version.

## Integration with the catalog

This shares the global analyzer mechanism with `microsoft-codeanalysis-bannedapianalyzers.md`, `microsoft-visualstudio-threading-analyzers.md`, `roslynator-analyzers.md`, and `sonaranalyzer-csharp.md`. Global configuration uses `ModularBase.globalconfig`; file-scoped policy uses `.editorconfig`.

## Security, performance, AOT, trimming, and operations

It executes within compiler/IDE processes, so restore analyzer packages only from approved feeds and expect build/IDE CPU cost. Private analyzer assets prevent consumer flow. It does not affect application runtime, trimming, or NativeAOT output.

## Avoid

Do not add duplicate per-project references, turn off broad rule families to avoid triage, or author a custom analyzer for policies already expressible through configured third-party or SDK analyzers.

## Verification checklist

- Build a representative future project and confirm diagnostics run locally and in CI.
- Verify a scoped `.editorconfig` setting has the intended precedence over the global config.
- Confirm package inspection shows no analyzer asset flowing to a produced library consumer.
- Review changed diagnostics when upgrading the centrally pinned version.

## Sources

- https://www.nuget.org/packages/Meziantou.Analyzer/3.0.132 (Accessed 2026-07-27)
- https://github.com/meziantou/Meziantou.Analyzer (Accessed 2026-07-27)
- https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files (Accessed 2026-07-27)
