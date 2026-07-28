# Analyzer policy

## Purpose

`IX.Modularity.Analyzers` is a produced, analyzer-only package for library consumers. It enforces the repository's semantic XML-documentation contract and provides a non-blocking record recommendation. It is compiler tooling, never a runtime dependency, source generator, code-fix package, or application framework.

## Roles and distribution

Roslyn compiler implementation packages are allowed for `Analyzer` and `SourceGenerator` production roles, their focused test projects, and the designated `ArchitectureTest` project when it performs focused C# documentation-syntax validation. A consumer loads `IX.Modularity.Analyzers` as an analyzer asset; it must not reference its assembly at runtime. See [dependency policy](dependency-policy.md) for the authoritative eligibility rule. The package is documented separately from catalogued external packages in [the package guide](../packages/ix-modularity-analyzers.md).

The analyzer reads the compiler-visible `IXModularityProjectRole` property only for rules whose taxonomy says that the role matters. It never uses repository paths, reflection, or application conventions to infer a role.

## Severity and suppression

`IXM1001` through `IXM1005` and `IXM3001` through `IXM3003` are repository errors. `IXM2001` is a suggestion. The definitive settings appear in both [`ModularBase.globalconfig`](../../ModularBase.globalconfig) and [`.editorconfig`](../../.editorconfig); the latter also controls source/test-specific magic-string severity for Sonar `S1192`.

Do not suppress an `IXM100x` diagnostic merely to publish an undocumented public contract. Correct the XML documentation, reduce the public surface, or record a narrow, reviewed exception at the declaration with a justification and expiry/review date. `IXM2001` is a syntactic class-shaped data-object suggestion, not an immutability inference. Suppress it locally after review when mutable lifecycle, identity, EF/proxy/framework/interop, or another class contract is intentional; a suggestion is not permission to change a public type's semantics without compatibility review.

`IXM3001` requires an approved FluentResults return shape. `IXM3002` permits a narrow local suppression only for an indirect factory or collection the analyzer cannot prove safe; it never approves string-only or uncoded failures. `IXM3003` should normally be fixed by removing a broad catch or using bare `throw;`. Reviewers, not the analyzer, decide whether a specific caught exception is an expected outcome, whether a translation is complete and safe, and whether state remains failure-atomic.

## Enforcement boundaries

Analyzer defaults describe package behavior. Repository configuration describes this repository's build policy. XML documentation diagnostics are not a substitute for public API baselines, package validation, tests, or a behavioral compatibility review. See [analyzer taxonomy](analyzer-taxonomy.md), [code quality policy](code-quality-policy.md), and [library public API and evolution](library-public-api-and-evolution.md).

## Authoritative references

- [Roslyn analyzer overview](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
- [Configuration files for code-analysis rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files)
- [NuGet analyzer conventions](https://learn.microsoft.com/en-us/nuget/guides/analyzers-conventions)

## Last research/access date

2026-07-27.
