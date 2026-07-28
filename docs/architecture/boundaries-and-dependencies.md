# Boundaries and dependencies

## Purpose

Define the source and package dependency boundaries that protect reusable library policy from application and vendor details.

## Canonical definitions

### Boundary and dependency

A boundary separates concepts that change for different reasons. A dependency is a compile-time, package, or runtime commitment. A port is a capability contract owned by the policy that needs it; an adapter translates that contract to a technology.

## Related and contrasting terms

Project references describe implementation relationships; NuGet dependencies describe consumer commitments. A composition root selects implementations and belongs outside a technology-neutral library. An adapter is not automatically an anti-corruption layer, though it can be one.

## Normative rules

- Follow the [project-role matrix](project-structure.md#dependency-matrix); source dependency cycles are prohibited.
- `Contracts` and `Abstractions` remain portable and may not expose vendor, ORM, hosting, or container types.
- Adapters depend inward and translate at the edge; neutral consumers never need the adapter’s technology.
- `PackageReference` entries omit `Version` because `Directory.Packages.props` is the sole package-version authority. `ProjectReference` entries express source project dependencies and are governed by the project-role graph.

## Library-focused examples

A `Library` can depend on an `Abstractions` port; an `Adapter` can implement it using a vendor SDK. An `Integration` package can offer a registration extension without making the core library build a service provider.

Externally visible service operations return FluentResults for expected outcomes. Reusable libraries return typed coded errors and propagate exceptional failures; the outer application or transport boundary maps those errors to HTTP, messages, or UI states. Do not put status codes, controller results, middleware, or transport envelopes in service contracts.

## Anti-patterns

An abstractions package that references a database provider, a core package that references its adapter, circular project references, and making a normal library consume analyzer assemblies at runtime are forbidden.

## Review questions

- Which side owns the contract and which side owns the technology?
- Does this public package impose a dependency every consumer must take?
- Is the new project a release boundary or merely a folder split?

## Analyzer and build enforcement

The architecture suite validates role metadata, reference direction, and cycles. Compiler-tool references must be analyzer-loading edges, not runtime assembly dependencies. See [analyzer policy](analyzer-policy.md).

## Authoritative references

- [Architectural rules](architectural-rules.md)
- [Dependency policy](dependency-policy.md)
- [Project structure](project-structure.md)

## Last research/access date

2026-07-27.
