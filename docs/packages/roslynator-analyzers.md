# Roslynator.Analyzers

## Catalog entry

`Roslynator.Analyzers` **4.15.0** — universal catalog analyzer supplied through a shared `GlobalPackageReference` with private analyzer assets.

- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** Review when the analyzer pin, `RCSxxxx` catalog/default severities, generated-code scope, or overlap with SDK/Meziantou/Sonar policy changes.

## Decision and scope

Use as a broad supplementary static-analysis set for future C# projects. It supplies `RCSxxxx` diagnostics and code fixes. It is not a formatter, a replacement for SDK analyzers, or a reason to introduce a custom analyzer for existing rule coverage; the separate Roslynator formatting analyzer and command-line tool are not part of this package decision.

## Recommended registration and use

The repository owns the global reference in `Directory.Packages.props`:

```xml
<GlobalPackageReference Include="Roslynator.Analyzers" Version="4.15.0"
                        PrivateAssets="all"
                        IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
```

Do not add local duplicates. In a different centrally managed repository that requires project-scoped installation, omit `Version` and keep analyzer assets private:

```xml
<PackageReference Include="Roslynator.Analyzers"
                  PrivateAssets="all"
                  IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
```

Configure individual diagnostics in `.editorconfig`. For example, keep RCS1085 (use auto-property) advisory while suppressing it only for generated clients:

```editorconfig
[*.cs]
dotnet_diagnostic.RCS1085.severity = suggestion

[src/**/Generated/**/*.cs]
dotnet_diagnostic.RCS1085.severity = none
```

Prefer per-ID settings over `dotnet_analyzer_diagnostic.category-roslynator.severity` when adopting or baselining rules. Category-wide changes can enable or suppress a large, changing surface during an upgrade.

| Setting | Catalog guidance |
| --- | --- |
| `dotnet_diagnostic.RCSxxxx.severity` | Preferred rule-specific control; document non-default suppressions. |
| `dotnet_analyzer_diagnostic.category-roslynator.severity` | Avoid for upgrades and baselines because category membership changes. |
| Path-scoped `.editorconfig` sections | Use only for a defined generated or compatibility boundary. |
| `PrivateAssets="all"` | Preserve so analyzer assets stay build-only. |

## Enterprise implementation guidance

Introduce rules in small batches: build the solution, export or record the emitted `RCSxxxx` IDs, identify overlap with SDK, Meziantou, and Sonar diagnostics, and choose one authoritative rule for each policy. Fix high-confidence correctness findings first. For subjective refactorings, start at suggestion, collect examples, then promote only after the team agrees on the intended semantics.

Review each code fix as a source change and run affected tests; do not bulk-apply fixes across public APIs, expression trees, generated code, serialization models, or performance-sensitive paths without targeted review. CI needs no separate Roslynator CLI here: `dotnet build` loads the NuGet analyzer and the repository converts warnings to errors. If a team separately adopts `Roslynator.DotNet.Cli`, manage and pin that tool independently rather than assuming it is supplied by this analyzer package.

### Upgrade and rollback

Upgrade the global reference without a category-wide severity change. Build representative projects, diff `RCSxxxx` IDs/default severities and code-fix output, check rule overlap, and compare clean-build time. If the version adds unreviewed policy, produces unsafe fixes, breaks a supported compiler host, or causes unacceptable build cost, restore `4.15.0` and its lock-file resolution. Retain independently reviewed fixes, but remove upgrade-only suppressions and do not introduce the CLI as an alternate rollback path.

## Integration with the catalog

Roslynator uses the same global analyzer integration as `meziantou-analyzer.md` and `sonaranalyzer-csharp.md`. SDK analyzer settings are in `Directory.Build.props`; `ModularBase.globalconfig` owns repository defaults; `.editorconfig` overrides matching source paths. Central package management owns version `4.15.0`, and `PrivateAssets=all` prevents the analyzer from flowing through produced packages. Review its [supply-chain record](../package-guidance/supply-chain.md#roslynator-analyzers) before upgrading.

## Security, performance, AOT, trimming, and operations

Roslynator findings are static review aids, not a vulnerability scan or runtime control. Changes suggested for allocation, LINQ, disposal, async, or API shape can alter behavior; validate them with tests and measurements. The analyzer itself affects IDE/build CPU and memory, not application runtime. Profile clean builds before using broad suppressions to address performance, and keep generated-code exclusions narrowly scoped.

Private analyzer assets prevent transitive consumer flow. The package has no direct NativeAOT or trimming effect, but a proposed refactoring may; run the project's publish checks when relevant. Restore analyzer packages only from approved sources because analyzers execute in compiler and IDE processes.

## Avoid

Do not accept bulk code fixes without tests, use this package to enforce formatting already owned by `.editorconfig`, enable or suppress the entire Roslynator category without an inventory, run analysis twice by adding an unmanaged CLI workflow, or add a local package reference or version to override central management.

## Verification checklist

- [ ] Confirm central version `4.15.0`, `PrivateAssets=all`, and no project-local duplicates.
- [ ] Build a representative C# project and record expected `RCSxxxx` diagnostics.
- [ ] Verify rule-ID and generated-path `.editorconfig` scopes behave identically locally and in CI.
- [ ] Run targeted tests after every analyzer-proposed semantic refactoring.
- [ ] Compare diagnostic counts and clean-build time before central upgrades.
- [ ] Inspect a packed library and confirm analyzer assets do not flow to consumers.

## Sources

- [Roslynator.Analyzers 4.15.0 on NuGet](https://www.nuget.org/packages/Roslynator.Analyzers/4.15.0) (Accessed 2026-07-27)
- [Roslynator analyzer catalog](https://josefpihrt.github.io/docs/roslynator/analyzers) (Accessed 2026-07-27)
- [Roslynator configuration reference](https://josefpihrt.github.io/docs/roslynator/configuration) (Accessed 2026-07-27)
- [Roslynator source repository](https://github.com/dotnet/roslynator) (Accessed 2026-07-27)
