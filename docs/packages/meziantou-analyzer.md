# Meziantou.Analyzer

## Catalog entry

`Meziantou.Analyzer` **3.0.134** — universal catalog analyzer supplied through a shared `GlobalPackageReference` with private analyzer assets.

- **Adoption:** Global analyzer
- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** Review when the analyzer pin, `MAxxxx` rule inventory/default severity, SDK analyzer set, or repository warning policy changes.

## Decision and scope

Use as a repository-wide supplementary quality analyzer. The catalog's global reference means future projects inherit it without local `PackageReference` duplication; it is not a production dependency and no custom analyzer is required. It complements the .NET SDK analyzers with `MAxxxx` diagnostics covering correctness, security, performance, globalization, and maintainability.

## Recommended registration and use

The repository already owns the registration in `Directory.Packages.props`; keep its version, private-assets boundary, and analyzer/build asset inclusion centralized:

```xml
<GlobalPackageReference Include="Meziantou.Analyzer" Version="3.0.134"
                        PrivateAssets="all"
                        IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
```

Do not repeat this in project files. In another centrally managed repository that cannot use `GlobalPackageReference`, the project-scoped equivalent omits `Version` and remains private:

```xml
<PackageReference Include="Meziantou.Analyzer"
                  PrivateAssets="all"
                  IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
```

Configure one diagnostic at a time in `.editorconfig` when a source path needs different policy. For example, MA0009 detects regular expressions created without an evaluation timeout:

```editorconfig
[*.cs]
dotnet_diagnostic.MA0009.severity = error
```

Meziantou also supports `MeziantouAnalysisMode` values such as `Default`, `None`, `all-suggestions`, `all-warnings`, and `all-errors`. This repository already establishes repository-wide warning severity in `ModularBase.globalconfig`, so prefer diagnostic-ID overrides here instead of adding a second bulk policy.

| Setting | Catalog guidance |
| --- | --- |
| `MeziantouAnalysisMode` | Leave at the repository-approved default; changing it is a catalog-wide diagnostic policy migration. |
| `dotnet_diagnostic.MAxxxx.severity` | Use for reviewed rule-specific policy and the narrowest necessary source scope. |
| `PrivateAssets="all"` | Preserve on every installation so analyzer assets do not flow to consumers. |
| `IncludeAssets` | Keep analyzer/build assets available to compilation and confirm the produced library package remains clean. |

## Enterprise implementation guidance

A common adoption workflow is:

1. Restore and build a representative solution with the new analyzer version without changing severities.
2. Inventory new `MAxxxx` IDs and separate defects from style preferences or overlap with SDK, Roslynator, and Sonar rules.
3. Fix high-confidence correctness and security findings first. Baseline only the remaining known debt with narrow diagnostic-ID and path-scoped entries.
4. Keep the same committed configuration for IDE and CI, then let the repository's `TreatWarningsAsErrors` policy enforce warnings.
5. Review and remove temporary baselines in small batches; treat every analyzer upgrade as a build-policy change.

Use code fixes as proposed refactorings, not as proof of semantic equivalence. Review diffs and run affected tests, especially for culture, comparison, cancellation, regex, and async rules.

### Upgrade and rollback

Change the global pin in isolation, restore, and build representative projects before modifying severity configuration. Diff emitted `MAxxxx` IDs, default severities, code-fix output, and clean-build time, then resolve overlap with SDK, Roslynator, and Sonar rules. If new diagnostics cannot be triaged safely or build/IDE cost regresses, restore `3.0.134` and its lock-file resolution; keep any independently valid source fixes, but revert suppressions or baselines added only to accommodate the failed upgrade.

## Integration with the catalog

This shares the global analyzer mechanism with `microsoft-codeanalysis-bannedapianalyzers.md`, `microsoft-visualstudio-threading-analyzers.md`, `roslynator-analyzers.md`, and `sonaranalyzer-csharp.md`. `Directory.Build.props` adds `ModularBase.globalconfig` to every project; file-scoped policy uses `.editorconfig`, whose matching entries take precedence over global AnalyzerConfig entries. Central package management owns the pin, so project files never add a version. Review its [supply-chain record](../package-guidance/supply-chain.md#meziantou-analyzer) before changing compiler-loaded tooling.

## Security, performance, AOT, trimming, and operations

Security-oriented diagnostics are preventative review signals, not vulnerability verification. Confirm a finding against the trust boundary and add regression tests for the unsafe input or behavior. Restore analyzer packages only from approved feeds because analyzers execute inside compiler and IDE processes.

Analyzer execution can increase IDE latency and build CPU. When an upgrade materially slows builds, compare clean-build binary logs and diagnostic counts before disabling rules; scope an expensive rule by diagnostic ID and path only with recorded evidence. Private analyzer assets prevent consumer flow. The package emits no application runtime code and therefore has no direct trimming or NativeAOT effect, although its diagnostics can recommend runtime-relevant changes that still require normal publish testing.

## Avoid

Do not add duplicate per-project references, enable overlapping rules without choosing one authoritative diagnostic, turn off broad rule families to avoid triage, accept bulk fixes without tests, or author a custom analyzer for policies already expressible through configured third-party or SDK analyzers.

## Verification checklist

- [ ] Confirm restore resolves centrally pinned version `3.0.134` and no project duplicates the reference.
- [ ] Build a representative future project locally and in CI and confirm expected `MAxxxx` diagnostics appear.
- [ ] Verify one scoped `.editorconfig` setting has the intended precedence over `ModularBase.globalconfig`.
- [ ] Inspect a packed library and confirm no Meziantou analyzer asset flows to the package consumer.
- [ ] Review diagnostic-count, clean-build-time, and code-fix changes before a central version upgrade.

## Sources

- [Meziantou.Analyzer 3.0.134 on NuGet](https://www.nuget.org/packages/Meziantou.Analyzer/3.0.134) (Accessed 2026-07-28)
- [Meziantou.Analyzer installation, rules, and analysis modes](https://github.com/meziantou/Meziantou.Analyzer) (Accessed 2026-07-27)
- [Microsoft: Configuration files for .NET code-analysis rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files) (Accessed 2026-07-27)
