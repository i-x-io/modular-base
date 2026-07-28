# Microsoft.CodeAnalysis.Analyzers

## Catalog entry

`Microsoft.CodeAnalysis.Analyzers` **5.6.0** — centrally pinned analyzer-authoring support for projects that implement Roslyn analyzers or source generators.

- **Adoption:** Direct
- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** Review when this package or the aligned Roslyn compiler API pins change, or when analyzer packaging/diagnostic release policy changes.

## Decision and scope

Use it only to build and validate compiler tooling. It helps analyzer and source-generator projects follow Roslyn authoring guidance; it is not a runtime dependency for ordinary libraries.

## Recommended registration and use

Reference it versionlessly from a compiler-tool project. Address its diagnostics in analyzer/source-generator implementation code and keep its package assets private to build tooling.

| Build setting | Catalog guidance |
| --- | --- |
| `PrivateAssets="all"` | Keep analyzer-authoring dependencies out of the produced package's consumer dependency graph. |
| `IncludeAssets` | Include analyzer/build assets needed during compiler-tool builds; inspect the packed output rather than assuming metadata is sufficient. |
| `EnforceExtendedAnalyzerRules` | Enable only deliberately and baseline the resulting RS diagnostics; it changes build policy, not runtime behavior. |
| `NoWarn` / `dotnet_diagnostic.RSxxxx.severity` | Prefer a reviewed per-ID severity with rationale over broad suppression. |

## Enterprise implementation guidance

Treat analyzer diagnostics as maintainability checks for compiler tooling. Keep analyzer callbacks bounded, cancelable, deterministic, and free from I/O. Test observable diagnostics rather than depending on implementation registration details unless a registration contract is deliberately exposed.

### Upgrade and rollback

Move this package with `Microsoft.CodeAnalysis.Common` and `Microsoft.CodeAnalysis.CSharp`, then build analyzer projects and run diagnostic/code-fix tests against the repository's supported compiler hosts. Inventory new, removed, and severity-changed `RSxxxx` diagnostics and inspect the `.nupkg` analyzer layout. If the upgrade breaks supported hosts, changes diagnostics unexpectedly, or introduces unacceptable build cost, restore all three `5.6.0` pins and lock-file entries together; do not roll back only the authoring analyzer and leave a mixed Roslyn toolset.

## Integration with the catalog

The central catalog owns version `5.6.0` alongside the Roslyn compiler APIs. Reference it explicitly and privately only from projects that build analyzers or source generators. Review its [supply-chain record](../package-guidance/supply-chain.md#microsoft-codeanalysis-analyzers) before changing compiler-loaded tooling.

## Security, performance, AOT, trimming, and operations

The package affects compiler-tool builds, not library runtime behavior. Analyzer work happens in consumers’ compilation processes, so expensive callbacks or environment-dependent behavior are operational risks.

## Avoid

Do not add it as a universal global analyzer, a runtime library dependency, or a substitute for tests of compiler-tool diagnostics.

## Verification checklist

- [ ] The compiler-tool project uses a versionless reference resolving `5.6.0`.
- [ ] Analyzer callbacks have no network, repository-path, or runtime-product dependency.
- [ ] Analyzer tests assert observable diagnostics independently.

## Sources

- [Roslyn analyzer tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix) — Accessed 2026-07-27.
- [Microsoft.CodeAnalysis.Analyzers 5.6.0 on NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Analyzers/5.6.0) — Accessed 2026-07-27.
