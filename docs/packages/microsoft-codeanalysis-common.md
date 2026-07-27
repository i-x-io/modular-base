# Microsoft.CodeAnalysis.Common

## Catalog entry

`Microsoft.CodeAnalysis.Common` **5.6.0** — centrally pinned Roslyn compiler API for `Analyzer` and `SourceGenerator` implementation or test projects.

- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** Review when any aligned Roslyn pin, supported compiler host, analyzer target framework, or analyzer package layout changes.

## Decision and scope

Use it only to build compiler tooling that inspects compilations, symbols, syntax, diagnostics, or analyzer configuration. It does not belong in a normal runtime library or application dependency graph.

## Recommended registration and use

Reference it versionlessly from a compiler-tool project. Use Roslyn semantic APIs rather than text matching for language meaning, and keep analyzers incremental, cancellation-aware, deterministic, and free of repository-path assumptions.

| Build/package setting | Catalog guidance |
| --- | --- |
| `PrivateAssets="all"` | Prevent compiler APIs from becoming transitive consumer dependencies. |
| Analyzer package path | Place the produced analyzer DLL under `analyzers/dotnet`, never `lib/` or `ref/`. |
| Analyzer target framework | Target a framework supported by every intended compiler host; validate IDE and command-line loading. |
| Roslyn package versions | Keep Common, CSharp, and authoring analyzers aligned; avoid APIs newer than the oldest supported host. |

## Enterprise implementation guidance

An analyzer package ships its DLL solely as an analyzer asset and must not expose Roslyn runtime APIs to a consumer. Analyzer tests may use the API to compile isolated snippets; production library tests should not take it merely for convenience.

### Upgrade and rollback

Upgrade the aligned Roslyn set together and compile representative analyzer tests under every supported SDK/compiler host. Check assembly-load compatibility, diagnostic IDs/locations, generated-code handling, cancellation, and packed asset paths. A successful build on the newest SDK is not sufficient evidence for older IDE hosts. If loading or analyzer behavior regresses, restore the complete `5.6.0` Roslyn pin set and lock files; avoid binding redirects, runtime packaging, or dual compiler-API branches as rollback mechanisms.

## Integration with the catalog

The central catalog owns version `5.6.0`, shared with `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.Analyzers`. Compiler-tool reference metadata is governed by [analyzer policy](../architecture/analyzer-policy.md); provenance is recorded in the [supply-chain reference](../package-guidance/supply-chain.md#microsoft-codeanalysis-common).

## Security, performance, AOT, trimming, and operations

Compiler analysis runs at build time, not in consumer runtime paths. Avoid compilation-wide scans and filesystem/network access, which can make builds slow, nondeterministic, or unsafe. Runtime trimming/AOT behavior is irrelevant when the package is correctly packaged as an analyzer only.

## Avoid

Do not add it to a `Library`, `Contracts`, `Abstractions`, or application project; use reflection to discover symbols; or package it under `lib/` or `ref/` as a runtime dependency.

## Verification checklist

- [ ] The compiler-tool project references it without a version.
- [ ] Analyzer behavior uses semantic APIs and honors cancellation.
- [ ] The produced package has only analyzer assets and no runtime library assets.

## Sources

- [Roslyn analyzer tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix) — Accessed 2026-07-27.
- [Microsoft.CodeAnalysis.Common 5.6.0 on NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Common/5.6.0) — Accessed 2026-07-27.
