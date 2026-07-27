# Microsoft.CodeAnalysis.Common

## Catalog entry

`Microsoft.CodeAnalysis.Common` **5.6.0** — centrally pinned Roslyn compiler API for `Analyzer` and `SourceGenerator` implementation or test projects.

## Decision and scope

Use it only to build compiler tooling that inspects compilations, symbols, syntax, diagnostics, or analyzer configuration. It does not belong in a normal runtime library or application dependency graph.

## Recommended registration and use

Reference it versionlessly from a compiler-tool project. Use Roslyn semantic APIs rather than text matching for language meaning, and keep analyzers incremental, cancellation-aware, deterministic, and free of repository-path assumptions.

## Enterprise implementation guidance

An analyzer package ships its DLL solely as an analyzer asset and must not expose Roslyn runtime APIs to a consumer. Analyzer tests may use the API to compile isolated snippets; production library tests should not take it merely for convenience.

## Integration with the catalog

The central catalog owns version `5.6.0`, shared with `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.Analyzers`. Compiler-tool reference metadata is governed by [analyzer policy](../architecture/analyzer-policy.md).

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
