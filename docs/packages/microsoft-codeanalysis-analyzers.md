# Microsoft.CodeAnalysis.Analyzers

## Catalog entry

`Microsoft.CodeAnalysis.Analyzers` **5.6.0** — centrally pinned analyzer-authoring support for projects that implement Roslyn analyzers or source generators.

## Decision and scope

Use it only to build and validate repository-owned compiler tooling. It helps analyzer projects follow Roslyn analyzer authoring guidance; it is distinct from the analyzer package that consumers receive.

## Recommended registration and use

Reference it versionlessly from a compiler-tool project. Address its diagnostics in analyzer/source-generator implementation code and keep its package assets private to build tooling.

## Enterprise implementation guidance

Treat analyzer diagnostics as maintainability checks for compiler tooling. Keep analyzer callbacks bounded, cancelable, deterministic, and free from I/O. Test observable diagnostics rather than depending on implementation registration details unless a registration contract is deliberately exposed.

## Integration with the catalog

The central catalog owns version `5.6.0` alongside the Roslyn compiler APIs. It is approved only for `Analyzer` and `SourceGenerator` project roles described in [project structure](../architecture/project-structure.md).

## Security, performance, AOT, trimming, and operations

The package affects compiler-tool builds, not library runtime behavior. Analyzer work happens in consumers’ compilation processes, so expensive callbacks or environment-dependent behavior are operational risks.

## Avoid

Do not add it as a universal global analyzer, a runtime library dependency, or a substitute for tests of the produced analyzer’s diagnostics.

## Verification checklist

- [ ] The compiler-tool project uses a versionless reference resolving `5.6.0`.
- [ ] Analyzer callbacks have no network, repository-path, or runtime-product dependency.
- [ ] Analyzer tests assert observable diagnostics independently.

## Sources

- [Roslyn analyzer tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix) — Accessed 2026-07-27.
- [Microsoft.CodeAnalysis.Analyzers 5.6.0 on NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Analyzers/5.6.0) — Accessed 2026-07-27.
