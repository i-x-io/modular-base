# Documentation, testing, and quality

## Purpose

Keep library contracts discoverable, testable, and continuously checked without treating generated output or application examples as policy evidence.

## Canonical definitions

### Documentation testing and quality

Documentation states consumer intent and operational constraints. A unit test checks a focused behavior. An architecture test checks dependency/structure rules. A baseline records an approved public surface. A suppression is a documented exception, not a way to erase a rule.

## Related and contrasting terms

XML documentation is API guidance; package guides describe dependency decisions; architecture policy defines repository rules. A test proves selected behavior, not every compatibility property. Formatting validates style but not design.

## Normative rules

- Every public data object, interface, interface member, service type, and service member has complete XML documentation under the `IXM100x` contract.
- Service operations use approved FluentResults shapes; direct failures use typed coded errors; and broad catches rethrow under `IXM3001`–`IXM3003`.
- Package catalog entries have exactly one guide following the [package documentation schema](package-documentation-schema.md).
- Tests cover public behavior, failure modes, and regressions; architecture tests cover role, package, and dependency invariants.
- Use the root Makefile as the only public build interface. Do not document direct `eng/Build.proj` invocation as a public command.

## Library-focused examples

A package guide explains why `Markdig` is allowed and where it may be used. An analyzer test compiles a minimal library snippet and asserts `IXM1003`, rather than relying on an application sample. A public API addition updates its XML docs, tests, and unshipped baseline together.

## Anti-patterns

Copying application examples into policy docs, declaring a package “tested” because formatting passes, broad analyzer suppression, and linking a catalog entry to no guide are rejected.

The result diagnostics verify shapes and direct static patterns. Code review must verify expected-versus-exceptional classification, the honesty of specific exception translation, message safety, state consistency, and failure atomicity. Documentation and tests must state those behavioral contracts explicitly.

## Review questions

- Can a consumer understand and use this public contract from XML docs and the package guide?
- Which test detects a regression in the promised behavior?
- Is the suppression local, justified, owned, and time-bounded?

## Analyzer and build enforcement

`IXM1001`–`IXM1005` and `IXM3001`–`IXM3003` are errors; `IXM2001` and `MA0109` are suggestions. `CS1591` is intentionally none. `make validate`, `make build`, `make test`, and `make format` are the documented public checks.

## Authoritative references

- [Analyzer index](analyzer-index.md)
- [Package documentation schema](package-documentation-schema.md)
- [Code quality policy](code-quality-policy.md)

## Last research/access date

2026-07-27.
