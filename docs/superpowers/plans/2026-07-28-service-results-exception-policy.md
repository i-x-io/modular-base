# Service Results and Exception Policy Implementation Plan

## Objective

Implement the approved [service results and exception policy design](../specs/2026-07-28-service-results-exception-policy-design.md) as three repository-owned Roslyn diagnostics, package-role governance, synchronized documentation, and executable verification.

Implementation runs on `codex/service-result-policy` in an isolated worktree. The dirty `main` worktree is not modified.

## Fixed decisions

- `DocumentationAndRecordAnalyzer` remains the only public analyzer entry point.
- `IXM3001`, `IXM3002`, and `IXM3003` default to warning in the package and are errors in this repository.
- The analyzer package has no FluentResults runtime or package dependency.
- Analyzer tests use the actual centrally pinned FluentResults 4.0.0 assembly through a private test-fixture reference.
- `IXM3002` is skipped only for known `Test`, `ArchitectureTest`, `Analyzer`, and `SourceGenerator` roles. A missing role is analyzed for downstream-consumer compatibility.
- The exact analyzer-test project has a narrow `PrivateAssets="all"` FluentResults fixture exception. It is not a production-role permission.
- Direct `Result.Try` calls are diagnosed because their internal broad catch cannot prove a specific expected exception translation.
- `IXM3003` classifies only untyped catches and exact `System.Exception` catches. Filters do not exempt them.
- `AnalyzerReleases.Shipped.md` remains historical. New rules go in `AnalyzerReleases.Unshipped.md`.

## Task 1: Test harness and real FluentResults metadata

Files:

- `test/IX.Modularity.Analyzers.Tests/IX.Modularity.Analyzers.Tests.csproj`
- `test/IX.Modularity.Analyzers.Tests/AnalyzerTestHarness.cs`
- `test/IX.Modularity.Analyzers.Tests/packages.lock.json`

Add a versionless private FluentResults package reference. Extend the harness to add `MetadataReference.CreateFromFile(typeof(FluentResults.Result).Assembly.Location)` on demand, omit it for missing-reference tests, and represent an absent project-role property distinctly from `Library`.

Acceptance: source snippets bind to real FluentResults 4.0.0 symbols; the analyzer project remains Roslyn-only.

## Task 2: IXM3001 service return contracts

Files:

- `src/IX.Modularity.Analyzers/DocumentationAndRecordAnalyzer.cs`
- internal analyzer helper files as needed
- focused analyzer test file for service-result rules

Add compilation-scoped cached symbols for `Result`, `Result<T>`, `Task<T>`, and `ValueTask<T>`. Analyze externally visible ordinary methods on existing `I*Service`/`*Service` classifications. Accept only the six approved result shapes. Report the interface-owned contract once, suppress the matching implementation report, and still check concrete-only service methods. Resolve by symbol identity and tolerate missing/error symbols.

Acceptance: exact tests cover every allowed/rejected shape, aliases, inheritance, visibility, partial/generated/malformed input, cancellation, and concurrency.

## Task 3: IXM3002 coded failures

Files:

- analyzer implementation files
- focused coded-error analyzer tests

Analyze the actual FluentResults 4.0.0 semantic surface:

- `Result.Fail` and `Result.Fail<T>` string, string-collection, error, and error-collection overloads;
- `Result.OkIf`, `Result.FailIf`, and `Result.FailIfNotEmpty` failure arguments and factories;
- `ResultBase<T>.WithError` and `WithErrors`;
- `ResultExtensions.OrFailIf` string failures;
- implicit `Error` and `List<Error>` conversions to `Result`/`Result<T>`; and
- every direct `Result.Try` call, regardless of the error-factory result.

Concrete errors must derive from `FluentResults.Error` and declare their own `public const string Code` matching `^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$`. Do not accept an inherited code, property, static-readonly field, metadata entry, base `Error`, or unrelated `IError` implementation. Recursively inspect statically visible collections and lambdas. Report unverifiable factory/collection arguments at recognized direct failure boundaries; a narrow justified suppression is the escape hatch. Arbitrary methods returning `Result` are outside interprocedural analysis.

Acceptance: tests cover valid typed/multi-error cases, all invalid code shapes, string/base errors, conversions, unknown factories, `Result.Try`, role gating, suppression, missing metadata, malformed input, and concurrency.

## Task 4: IXM3003 broad catch control flow

Files:

- analyzer implementation files
- focused broad-catch analyzer tests

Use semantic catch identity and Roslyn control-flow analysis. A broad catch passes only when it has no reachable normal exit, return, replacement throw, `throw caughtException`, or unresolved terminal path, and every reachable terminal throw is an operand-free bare rethrow. Include filters and `finally` effects; analyze nested local functions/lambdas independently. Fail safely when malformed source prevents a valid operation graph.

Acceptance: branch, loop, filter, finally, fall-through, return, replacement, throw-variable, logging/cleanup, cancellation, generated, malformed, cancellation, and concurrent cases are covered.

## Task 5: Package-role and architecture enforcement

Files:

- `Directory.Build.targets`
- `test/IX.Modularity.Architecture.Tests/ProjectGraph.cs`
- `test/IX.Modularity.Architecture.Tests/RepositoryArchitectureTests.cs`
- `test/IX.Modularity.Architecture.Tests/DocumentationIntegrityTests.cs`

Permit FluentResults for `Library`, `Contracts`, `Abstractions`, `Adapter`, and `Integration`. Permit it in the exact analyzer-test project only with `PrivateAssets="all"`. Reject other roles and retain the no-other-package rule for neutral roles. Change diagnostic documentation validation from six rules to the exact nine-rule union of shipped and unshipped release entries.

Acceptance: allowed/rejected role fixtures, narrow test exception, descriptor/help/release/index/taxonomy parity, and all existing graph rules pass.

## Task 6: Configuration, release metadata, and documentation

Files:

- `.editorconfig`
- `ModularBase.globalconfig`
- `src/IX.Modularity.Analyzers/AnalyzerReleases.Unshipped.md`
- analyzer README and package guide
- three source diagnostic help pages
- architecture analyzer index, taxonomy, policy, and three diagnostic pages
- architecture dependency, structure, boundary, domain, public API, quality, observability, and terminology pages
- `docs/packages/fluentresults.md`

Synchronize all nine diagnostics and distinguish analyzer-enforced structure from review-only semantics. Document the narrow FluentResults role exception, stable typed codes, expected outcomes, exception propagation, cancellation, specific translation, broad-catch rethrow, `Result.Try` prohibition, failure atomicity, and breaking API migrations.

Acceptance: Markdown links/anchors/schema and diagnostic inventories pass. Existing unrelated package-document expansion is absent from this isolated worktree and is not modified on `main`.

## Task 7: Verification and delivery

Run focused analyzer and architecture tests, Debug and Release builds/tests, formatting and validation, audit, outdated-package check, SBOM, pack, and Release CI through the Makefile. Inspect the NuGet package for analyzer-only layout and no FluentResults dependency. Build temporary clean consumers covering every diagnostic's pass/fail behavior and role-absent IXM3002 behavior. Remove all probes and inspect the complete diff.

Then run independent correctness, security, test, and final-verification reviews. Resolve findings and rerun affected checks before committing the implementation branch.

## Exclusions

- No code fixes, runtime library, result wrapper, application host, HTTP mapping, middleware, or automatic public API migration.
- No blanket throw prohibition and no claim that analyzer structure proves business semantics or failure atomicity.
- No FluentResults dependency in the analyzer package.
- No edits to the dirty `main` worktree or unrelated package documentation.
