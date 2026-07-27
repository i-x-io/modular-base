# Architecture terminology

## Purpose and authority

This glossary gives future `IX.Modularity.*` libraries one shared vocabulary. A term being defined here does not, by itself, create a build rule, project type, package, or dependency. The normative requirements are in [architectural rules](architectural-rules.md) and the permitted project relationships are in [project structure](project-structure.md).

The labels used throughout the architecture documentation have precise force:

| Label | Meaning |
| --- | --- |
| **Defined vocabulary** | A name used consistently by this repository. It may describe a concept without requiring a particular implementation. |
| **Normative** | A requirement for future library projects. A pull request must either comply or record an approved exception. |
| **Enforceable** | A normative rule that can be checked mechanically once a suitable project or architecture test exists. An unenforced normative rule remains required. |
| **Heuristic** | A decision aid. It directs design review but is not an automatic rejection criterion. |
| **Out of scope** | Useful context that this repository intentionally does not prescribe. |

## Design principles

### Defined vocabulary

| Term | Meaning | Status in this repository |
| --- | --- | --- |
| **SOLID** | The family of object-oriented principles: single responsibility, open/closed, Liskov substitution, interface segregation, and dependency inversion. | Heuristic. Use it to examine responsibilities and dependencies; do not treat the acronym as a substitute for a concrete rule. |
| **Separation of concerns (SoC)** | Place concerns that change for different reasons behind distinct boundaries. | Normative at the project-boundary level; see architectural rules. |
| **Cohesion** | The degree to which the contents of a unit belong together for one reason to change. | Heuristic. Prefer cohesive projects and packages. |
| **Coupling** | The degree to which one unit must know about or change with another. | Heuristic. Prefer narrow, explicit, acyclic dependencies. |
| **DRY** | Keep one authoritative representation of a rule or piece of knowledge. | Normative for policy, contracts, and compatibility baselines; use judgment for coincidental code similarity. |
| **KISS** | Prefer the smallest understandable design that satisfies the stated need. | Heuristic. |
| **YAGNI** | Do not add capability until a current requirement needs it. | Normative against speculative public abstractions and project splits. |
| **CQS** | At an operation boundary, a query observes state and returns information; a command changes state. | Heuristic for public APIs and application services. |
| **CQRS** | Separate read and write models when their responsibilities justify it. It is broader than CQS and can use one or separate stores. | Out of scope as a required architecture. Adopt it only for a documented workload need. |

### Component and package principles

Here, a **component** means a releasable and independently versioned unit, normally a NuGet package with one or more assemblies. It is not synonymous with a C# project.

| Term | Meaning | Status in this repository |
| --- | --- | --- |
| **REP** — Release Equivalence Principle | Units released together should be versioned and released together. | Normative when choosing a package boundary. |
| **CCP** — Common Closure Principle | Put classes that change together for the same reason in the same component. | Heuristic. |
| **CRP** — Common Reuse Principle | Do not force consumers to depend on types they do not use. | Normative for public package dependencies; heuristic for internal layout. |
| **ADP** — Acyclic Dependencies Principle | The component dependency graph has no cycles. | Normative and enforceable when dependency checks exist. |
| **SDP** — Stable Dependencies Principle | Depend in the direction of greater stability. | Heuristic. Use stable contracts and abstractions to avoid implementation-driven dependency direction. |
| **SAP** — Stable Abstractions Principle | A stable component should be abstract enough to avoid resisting change, without becoming empty ceremony. | Heuristic. |

## Architectural styles and boundaries

| Term | Meaning | Status in this repository |
| --- | --- | --- |
| **Layered architecture** | Organizes code into responsibility layers; dependencies generally point inward or downward according to the chosen layer model. | Defined vocabulary. This repository uses its dependency direction, not a fixed set of application layers. |
| **Clean architecture** | Organizes code around business policies and controls source-code dependencies so details depend on policies. | Defined vocabulary and heuristic. |
| **Onion architecture** | Places domain policy at the center and infrastructure at outer rings; dependencies point inward. | Defined vocabulary and heuristic. |
| **Hexagonal architecture** | Separates an application core from primary/driving and secondary/driven adapters through ports. | Defined vocabulary and heuristic. A repository `Adapter` role is an implementation boundary, not proof that a system is hexagonal. |
| **Modular monolith** | One deployable application with modules that retain explicit internal boundaries. | Out of scope for this library repository. Libraries may support one, but do not prescribe an application deployment topology. |
| **Composition root** | The outermost place that selects implementations and assembles an object graph. | Defined vocabulary. It belongs in an application or integration boundary, never in an abstractions-only package. |
| **Port** | An interface owned by the policy that needs a capability. | Defined vocabulary. It normally belongs in `Abstractions` or `Contracts`, not in an adapter. |
| **Adapter** | Code that translates between a port/contract and a concrete technology, protocol, or external library. | Defined vocabulary. It is a project role with specific dependency constraints. |

## Domain-driven design vocabulary

DDD terms describe a domain model; they do not require that every library contain a domain model.

| Term | Meaning | Status in this repository |
| --- | --- | --- |
| **Domain** | The subject area and rules the software serves. | Defined vocabulary. |
| **Ubiquitous language** | Shared, context-specific language used by domain experts and developers. | Heuristic. Use it for public domain-facing names. |
| **Bounded context** | An explicit boundary within which a model and its terms have one meaning. | Defined vocabulary and heuristic for package/module boundaries. |
| **Context map** | A description of relationships between bounded contexts. | Out of scope unless a future multi-context system needs one. |
| **Aggregate** | A consistency boundary with an aggregate root controlling changes to its contained model. | Defined vocabulary. |
| **Entity** | An object defined by continuity of identity. | Defined vocabulary. |
| **Value object** | An object defined by its attributes rather than identity. | Defined vocabulary. |
| **Repository** | A collection-like abstraction for retrieving and persisting aggregates. | Defined vocabulary; do not introduce one solely to mirror a database. |
| **Domain service** | Stateless domain behavior that does not naturally belong to one entity or value object. | Defined vocabulary. |
| **Application service** | Orchestrates a use case, coordinates dependencies, and delegates domain decisions to domain policy. | Defined vocabulary. |
| **Domain event** | A record of a meaningful fact that occurred in a domain. | Defined vocabulary. |
| **Anti-corruption layer (ACL)** | Translation that prevents one bounded context or external model from leaking into another. | Heuristic. An `Adapter` may implement an ACL. |

## .NET library vocabulary

| Term | Meaning | Status in this repository |
| --- | --- | --- |
| **Project** | An MSBuild project that produces an assembly, package, analyzer, generator, or test output. | Defined vocabulary. A project is an implementation unit, not necessarily a release unit. |
| **Assembly** | The CLR deployment and type-identity unit produced by a project. | Defined vocabulary. |
| **NuGet package** | The distribution and version-selection unit consumed through NuGet. | Defined vocabulary. One package can contain multiple assemblies. |
| **Public API** | Public and protected types and members that consumers can compile against, plus documented behavior that consumers reasonably rely on. | Normative compatibility boundary for released libraries. |
| **Source compatibility** | Consumer source still compiles after an update. | Defined vocabulary; insufficient on its own for a released package. |
| **Binary compatibility** | Previously compiled consumer code can run against the updated assembly without a missing or changed public contract. | Normative release concern. |
| **Behavioral compatibility** | An update preserves documented and reasonably relied-on behavior. | Normative release concern; it requires review and tests, not only API tooling. |
| **Package validation** | SDK/API-compatibility validation of a package or assembly against compatible target frameworks or a baseline package. | Enforceable when enabled for a package project. |
| **Public API baseline** | A checked-in record of intentional shipped and pending public API surface, used by public API analyzers. | Normative for packable projects under the existing code-quality policy. |

## Sources

- [Microsoft, CQRS pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs) — Accessed 2026-07-27.
- [Martin Fowler, Command Query Separation](https://martinfowler.com/bliki/CommandQuerySeparation.html) and [CQRS](https://martinfowler.com/bliki/CQRS.html) — Accessed 2026-07-27.
- [Microsoft, .NET API compatibility tools](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/overview) — Accessed 2026-07-27.
- [Microsoft, NuGet package compatibility rules](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/nuget-package-compatibility-rules) and [versioning .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/versioning) — Accessed 2026-07-27.
- Robert C. Martin, [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2011/11/22/Clean-Architecture.html) — Accessed 2026-07-27.
- Eric Evans, [*Domain-Driven Design*](https://www.domainlanguage.com/ddd/) — Accessed 2026-07-27.
