# Code quality policy

## Scope and enforcement

This is the quality contract for projects beneath the repository root. It applies to the non-packable `ArchitectureTest` project at `test/IX.Modularity.Architecture.Tests` and to future production projects. The architecture-test project validates governance; `src/` contains no production projects.

`Directory.Build.props` supplies the default framework, language, nullable, documentation-file, determinism, analyzer, and warning policy. `Directory.Build.targets` supplies package-reference validation, packable-project validation, and a self-contained lexical source-policy MSBuild task. The root `.editorconfig` supplies file-scoped editor and C# style rules. `ModularBase.globalconfig` supplies repository-wide analyzer diagnostic defaults through `GlobalAnalyzerConfigFiles`.

## Evaluation and precedence

MSBuild imports `Directory.Build.props` early, before the project file, and imports `Directory.Build.targets` after the project. Standard MSBuild evaluation is last writer wins, so a property in a project can override a shared property unless the build policy separately rejects it. Keep project exceptions narrow and documented.

For analyzer configuration, `.editorconfig` is rooted at this repository and controls matching source files, while `ModularBase.globalconfig` is explicitly included for every project and establishes repository-wide diagnostic defaults. A more specific applicable setting can override a broader one. Build severity comes from the resolved analyzer/editor configuration plus `TreatWarningsAsErrors` and `CodeAnalysisTreatWarningsAsErrors` in shared props. Banned API analysis excludes generated code; the repository source-policy task also excludes generated paths and generated-file suffixes.

## Required code practices

The Banned API analyzer reads [`BannedSymbols.txt`](../../BannedSymbols.txt). Its generated-code exclusion prevents diagnostics from SDK and source-generator output. Banned API analysis alone does not catch every plain type declaration, so `ValidateSourcePolicy` runs before `CoreCompile` as a lexical MSBuild task, not a custom analyzer or Roslyn AST task. It tokenizes source, ignores comments and literals, excludes generated files, and still tokenizes expressions inside interpolated strings. Because this is not a custom analyzer, semantic API operations remain Banned API analyzer responsibility.

The source-policy task emits these diagnostics for non-generated source:

- `MB0001` for `dynamic`.
- `MB0002` for qualified `IDictionary` usage and recognizable type-position syntax. Unrelated class, member, or local identifiers named `IDictionary` pass.
- `MB0003` for `typeof`, qualified `System.Type`, and `System.Reflection` namespace syntax.

The Banned API analyzer semantically detects actual `System.Activator`, `object.GetType`, and other banned reflection operations. The lexical task does not claim to detect arbitrary reflection member names or `Activator` usages.

The source-policy task makes one narrow, path-specific exception for `test/IX.Modularity.Architecture.Tests/IX.Modularity.Architecture.Tests.csproj`: that non-packable architecture-test project may inspect compiled assembly metadata to enforce architectural rules. The lexical exception skips only `MB0003`; `MB0001` and `MB0002` remain active. The required Banned API suppression is scoped to the two assembly-metadata statements in the test source. No project-wide suppression exists, and production or ordinary test projects receive no exception. See [architectural rules](architectural-rules.md), [project structure](project-structure.md), and [architecture terminology](terminology.md) for the governing role and rule definitions.

Production code must not use:

- Non-generic `IDictionary` or generic `IDictionary<TKey, TValue>` as a contract. Prefer `IReadOnlyDictionary<TKey, TValue>` for read-only inputs, a concrete `Dictionary<TKey, TValue>` for owned mutation, or a purpose-specific immutable type.
- Ambient clocks: `DateTime.Now`, `DateTime.UtcNow`, `DateTime.Today`, `DateTimeOffset.Now`, and `DateTimeOffset.UtcNow`. Domain and application code depends on a consuming-project-owned `IClock` contract instead; this catalog does not supply that interface. An infrastructure `IClock` implementation may wrap `TimeProvider.System`, using `GetUtcNow()` by default and `GetLocalNow()` only when the local-zone meaning is required. Do not make `TimeProvider` the primary domain/application contract.
- Runtime reflection: `object.GetType`, `Type`, `Activator`, and `System.Reflection`. Prefer source generators, explicit registration, generated serializers, or a reviewed framework integration.
- The `dynamic` keyword.

`Microsoft.Extensions.TimeProvider.Testing` is catalogued for tests. Test code may back or fake `IClock` with `FakeTimeProvider` to control time-dependent behavior deterministically. This repository defines no `IClock` implementation.

Public library code also follows these explicit quality rules:

- A public data object, interface, interface member, service type, and service member must meet the complete XML documentation contract in [analyzer taxonomy](analyzer-taxonomy.md). `CS1591` is intentionally disabled because it is broader and less precise than the `IXM1001`–`IXM1005` contract.
- A syntactically eligible class-shaped data object should be reviewed for a record. `IXM2001` does not infer immutability; mutable lifecycle, identity, EF/proxy/framework/interop, and compatibility are valid reviewed reasons to retain and locally suppress a class. It is never a reason to change public equality or compatibility semantics without review.
- Repeated source strings are contract debt. `S1192` is an error in `src/**/*.cs` and nonblocking in tests; use a named constant, typed option, or semantic value where one authoritative value exists.
- Public library APIs document nullability, ownership, mutation, and lifetime. Use spans only for synchronous contiguous-memory work; use memory abstractions across asynchronous or retained boundaries. See [performance and resource management](performance-and-resource-management.md).
- Use source-generated logging for reusable parameterized logging paths. Keep templates static and structured; do not configure logging providers or build service providers in reusable libraries.

## Analyzer and public API policy

The following analyzers are configured as centrally-versioned `GlobalPackageReference` items and apply to all projects: Meziantou, Banned API, Visual Studio Threading, Roslynator, and SonarAnalyzer. They are build-only (`PrivateAssets="all"`) and never become package dependencies.

For packable projects only, shared targets add `Microsoft.CodeAnalysis.PublicApiAnalyzers` and include `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` when present. Before packing, the policy requires both files, a `PackageId`, and `PackageVersion` or `Version`. This keeps API-compatibility tracking a package concern rather than a catalog-wide requirement.

The produced `IX.Modularity.Analyzers` package is consumer opt-in compiler tooling, never a runtime dependency. `IXM1001` through `IXM1005` default to warning and are repository errors; `IXM2001` defaults to info and is a repository suggestion. `CA1200`, `CA1845`, `CA1846`, `CA1848`, `CA1873`, and `CA2254` are errors; `MA0109` is a suggestion. The exact settings are intentionally duplicated in `ModularBase.globalconfig` and `.editorconfig` for repository-wide and editor-visible enforcement.

## Sources

- [Customize the build by folder](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory) — Accessed 2026-07-27.
- [MSBuild properties](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-properties) — Accessed 2026-07-27.
- [Code-style rule options](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/code-style-rule-options) — Accessed 2026-07-27.
- [TimeProvider overview](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview) and [testing with FakeTimeProvider](https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing) — Accessed 2026-07-27.
