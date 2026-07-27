# SonarAnalyzer.CSharp

## Catalog entry

`SonarAnalyzer.CSharp` **10.30.0.144632** — universal catalog analyzer supplied through a shared `GlobalPackageReference` with private analyzer assets.

## Decision and scope

Use as the repository's compile-time Sonar rule set for future C# code. It is not SonarQube/SonarCloud server analysis, does not upload data by itself, and does not replace code review or security testing.

## Recommended registration and use

Retain the existing global package reference and configure diagnostic severity in `.editorconfig` or `ModularBase.globalconfig`. Treat rule activation and upgrades as policy changes; keep local overrides narrowly scoped and documented. Do not create a custom analyzer for an existing Sonar or SDK rule.

## Enterprise implementation guidance

Align build-time rule severities with the organization's quality profile where one exists, then resolve differences intentionally. Review security findings with threat context, use suppression only with a durable rationale, and preserve CI diagnostics as review evidence without publishing sensitive source details.

## Integration with the catalog

This shares the global analyzer mechanism with `meziantou-analyzer.md`, `roslynator-analyzers.md`, and the other universal analyzers. Repository-wide defaults are in `ModularBase.globalconfig`; path/file overrides live in `.editorconfig` and take precedence.

## Security, performance, AOT, trimming, and operations

Sonar diagnostics can identify maintainability and security risks, but they are neither a penetration test nor a runtime protection control. Analyzer work affects IDE/build time only; private analyzer assets keep it out of produced packages and it has no NativeAOT/trimming effect.

## Avoid

Do not equate a clean analyzer build with security approval, suppress an issue without a threat-model reason, duplicate the package per project, or expect this package alone to perform server-side Sonar reporting.

## Verification checklist

- Build a representative C# project and record expected Sonar diagnostic IDs.
- Verify global and `.editorconfig` severity precedence for one scoped rule.
- Review security-rule findings with the owning security process.
- Test central-version upgrades before enforcement changes reach CI.

## Sources

- https://www.nuget.org/packages/SonarAnalyzer.CSharp/10.30.0.144632 (Accessed 2026-07-27)
- https://github.com/SonarSource/sonar-dotnet (Accessed 2026-07-27)
- https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files (Accessed 2026-07-27)
