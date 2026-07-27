# Architectural rules

## Scope and rule force

These are the normative architecture rules for future `IX.Modularity.*` library projects. They supplement, rather than replace, the repository [dependency policy](dependency-policy.md) and [code-quality policy](code-quality-policy.md). The vocabulary and its force labels are defined in [architecture terminology](terminology.md).

The repository currently has no `src/` production projects. It intentionally contains one `ArchitectureTest` project, but no library package project. Accordingly, a rule marked **enforceable** states the check performed for a qualifying project; it does not imply that every rule can already be exercised against production code. Exceptions require an architecture-decision record or pull-request decision that names the rule, scope, justification, owner, and removal/review date.

## Boundary rules

1. **A project has one declared `IXModularityProjectRole`.** Set the role in the project metadata/property once the project exists, and use only the exact values in [project structure](project-structure.md#project-roles). A project may perform one primary role; split it only when the role and dependency matrix demand different dependency directions. **Enforceable.**
2. **Dependencies follow the project-role matrix.** A source project reference or direct package dependency that would reverse the matrix is prohibited. External framework packages are allowed only when they are necessary to the owning role and do not expose their technology-specific types through a role that promises independence. **Enforceable in part.**
3. **No project dependency cycles.** The complete `ProjectReference` graph is acyclic (ADP), including production, reusable testing support, ordinary tests, and architecture tests. MSBuild rejects cycles in the evaluated build graph; the architecture suite also checks the declared repository graph. **Enforceable.**
4. **Contracts and abstractions stay technology-neutral.** `Contracts` and `Abstractions` may not reference concrete transport, storage, ORM, hosting, DI-container, or vendor SDK packages. They own portable data and capability boundaries. **Enforceable in part.**
5. **Adapters translate at the edge.** An `Adapter` owns technology-specific types and converts them to its consuming contract or abstraction. It must not require consumers of a technology-neutral package to reference the adapter's technology. **Enforceable in part.**
6. **Composition remains outside reusable policy.** Construction, configuration binding, container registration, and runtime selection of concrete adapters belong in `Integration` or an application composition root. A `Library`, `Contracts`, or `Abstractions` project must not build a service provider or locate dependencies dynamically. **Enforceable in part.**
7. **Keep the public surface deliberate.** Public and protected APIs are package contracts. Do not expose an implementation-only type, an external vendor type, or a technology-specific exception through an independent package API unless the package explicitly owns that integration boundary. **Review-required.**
8. **Release compatible changes deliberately.** For a released package, preserve source, binary, and behavioral compatibility unless the release intentionally communicates a breaking change. Package IDs, dependencies, target-framework assemblies, public API baselines, and documentation are part of the release contract. Use the package version as the public release identifier. **Enforceable in part.**
9. **Use a baseline where compatibility matters.** Packable projects follow the existing public API baseline policy. When a released package enables package/API compatibility validation, compare it with the previous compatible version and investigate every difference. Do not suppress a difference without a documented compatibility decision. **Enforceable when tooling is configured.**
10. **Avoid speculative boundaries.** Do not create a project, package, interface, adapter, generic extension point, or CQRS split without a current consumer, dependency direction, independent release cadence, or testability need. **Review-required.**

## Package and API rules

| Rule | Requirement |
| --- | --- |
| Package ownership | A package owns a cohesive consumer-facing capability. Avoid a package that makes consumers take unrelated dependencies (CRP). |
| Release ownership | Types that must be changed, versioned, and released together normally belong in one package (REP). A C# project is split from another only when it creates a meaningful architectural or release boundary. |
| Assembly and package identity | Treat NuGet package identity/version, assembly identity/version, and public API as distinct contracts. Do not infer runtime behavior from only the NuGet version. |
| Compatible TFMs | A multi-targeted package exposes compatible API surface for compatible target frameworks, unless the package documentation declares a supported platform/API difference. |
| Public API additions | Additions need XML documentation, tests, and a public API baseline update when the package policy applies. Consider whether a capability can remain internal until a consumer needs it. |
| Breaking changes | Removing or changing public types, members, signatures, constraints, inheritance, behavior, or package dependencies requires explicit compatibility review and release-version decision. A major version does not make an undocumented break harmless. |
| Dependency flow | A public package dependency is a transitive consumer commitment. Keep it minimal, deliberate, centrally catalogued, and versionless in project files as required by the dependency policy. |

## CQS, CQRS, and DDD usage

- Public operations should make mutation versus observation clear. A query should not make an observable domain-state change; a command should not promise a meaningful data result merely to return the modified object (CQS).
- CQRS is optional. Separate read and write models only when their validation, consistency, security, scale, or representation needs justify the extra model and operational cost.
- A DDD aggregate is a consistency boundary, not a persistence-shaped object. Repositories, domain services, and events appear only when they clarify a genuine domain responsibility.
- A bounded-context or anti-corruption boundary is justified by a difference in model meaning or ownership, not merely by a namespace or database table.

These are **review heuristics** except where a boundary rule makes them normative.

## What this policy does not prescribe

This repository does not require a clean, onion, hexagonal, layered, or modular-monolith implementation; microservices; event sourcing; CQRS; a particular DI container; or a universal repository pattern. Select an architectural style per consuming system while preserving this repository's project roles, dependency direction, and public API commitments.

## Sources

- [Microsoft, .NET API changes that affect compatibility](https://learn.microsoft.com/en-us/dotnet/core/compatibility/library-change-rules) — Accessed 2026-07-27.
- [Microsoft, NuGet package compatibility rules](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/nuget-package-compatibility-rules) and [versioning .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/versioning) — Accessed 2026-07-27.
- [Microsoft, API compatibility tools](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/overview) — Accessed 2026-07-27.
- [Microsoft, CQRS pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs) — Accessed 2026-07-27.
- [Martin Fowler, Command Query Separation](https://martinfowler.com/bliki/CommandQuerySeparation.html) — Accessed 2026-07-27.
