# Analyzer policy

## Purpose

`IX.Modularity.Analyzers` is a produced, analyzer-only package for library consumers. It enforces the repository's semantic XML-documentation contract and provides a non-blocking record recommendation. It is compiler tooling, never a runtime dependency, source generator, code-fix package, or application framework.

## Roles and distribution

Only a project with role `Analyzer` or `SourceGenerator` may use Roslyn compiler implementation packages. A consumer loads `IX.Modularity.Analyzers` as an analyzer asset; it must not reference its assembly at runtime. The package is documented separately from catalogued external packages in [the package guide](../packages/ix-modularity-analyzers.md).

The analyzer reads the compiler-visible `IXModularityProjectRole` property only for rules whose taxonomy says that the role matters. It never uses repository paths, reflection, or application conventions to infer a role.

## Severity and suppression

`IXM1001` through `IXM1005` are repository errors. `IXM2001` is a suggestion. The definitive settings appear in both [`ModularBase.globalconfig`](../../ModularBase.globalconfig) and [`.editorconfig`](../../.editorconfig); the latter also controls source/test-specific magic-string severity for Sonar `S1192`.

Do not suppress an `IXM100x` diagnostic merely to publish an undocumented public contract. Correct the XML documentation, reduce the public surface, or record a narrow, reviewed exception at the declaration with a justification and expiry/review date. `IXM2001` is a syntactic class-shaped data-object suggestion, not an immutability inference. Suppress it locally after review when mutable lifecycle, identity, EF/proxy/framework/interop, or another class contract is intentional; a suggestion is not permission to change a public type's semantics without compatibility review.

## Enforcement boundaries

Analyzer defaults describe package behavior. Repository configuration describes this repository's build policy. XML documentation diagnostics are not a substitute for public API baselines, package validation, tests, or a behavioral compatibility review. See [analyzer taxonomy](analyzer-taxonomy.md), [code quality policy](code-quality-policy.md), and [library public API and evolution](library-public-api-and-evolution.md).

## Authoritative references

- [Roslyn analyzer overview](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
- [Configuration files for code-analysis rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files)
- [NuGet analyzer conventions](https://learn.microsoft.com/en-us/nuget/guides/analyzers-conventions)

## Last research/access date

2026-07-27.
