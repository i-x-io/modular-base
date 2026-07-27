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

## A–Z linked glossary

The following stable anchors are the preferred cross-document links for terms used by the deep taxonomy. They complement the canonical vocabulary above; they do not create additional project roles or application architecture requirements.

## A

[Adapter](boundaries-and-dependencies.md#boundary-and-dependency) translates technology at a policy boundary.

## B

Boundary separates concerns that change for different reasons.

## C

Contract is a consumer-visible API, behavior, or dependency commitment.

## D

[Domain model](domain-modeling.md#domain-model) gives library terms their stable meaning.

## E

Entity is a domain object defined by continuity of identity.

## F

Framework detail is implementation technology, not a neutral public contract.

## G

Generated code is compiler-produced output and outside user-authored documentation diagnostics.

## H

Heuristic is a review aid rather than an automatically rejecting rule.

## I

Immutable means state cannot change after construction.

## J

Justification records why a narrow policy exception is necessary.

## K

KISS prefers the smallest understandable design that meets the current need.

## L

[Library public API](library-public-api-and-evolution.md#library-public-api) is the released consumer contract.

## M

Magic string is a repeated semantic literal lacking one authoritative named or typed representation.

## N

[Nullability](type-system-and-data-modeling.md#type-system-and-data-model) is a public promise about absent values.

## O

[Observability](observability-and-operability.md#observability-and-operability) provides bounded, structured operational signals.

## P

[Performance](performance-and-resource-management.md#performance-and-resource-management) is a measured workload property, not an intuition.

## Q

Query observes state and returns information without a meaningful domain-state mutation.

## R

Record is a C# type with value-oriented generated members.

[Result](#result) represents an expected service outcome without using an exception as ordinary control flow.

## S

[Single responsibility](design-principles.md#design-principles) means one primary reason for a type or package to change.

## T

Trace correlates one operation across component boundaries.

## U

Ubiquitous language is the shared, context-specific language used for stable domain names.

## V

Value object is defined by attributes/value equality rather than continuity of identity.

## W

Warning is a diagnostic severity; repository policy can promote it to a build error.

## X

[XML documentation](documentation-testing-and-quality.md#documentation-testing-and-quality) is the consumer-facing C# API contract emitted from documentation comments.

## Y

YAGNI rejects speculative public capability and premature project splits.

## Z

Zero-cost abstraction is a performance claim that requires measurement, not a default assumption.

| Term | Definition and policy link |
| --- | --- |
| <a id="adapter"></a>**Adapter** | Technology-specific translation at a boundary; see [boundaries and dependencies](boundaries-and-dependencies.md). |
| <a id="analyzer"></a>**Analyzer** | Compiler-time diagnostic tooling; see [analyzer policy](analyzer-policy.md). |
| <a id="api-compatibility"></a>**API compatibility** | Source, binary, and behavioral preservation for released contracts; see [library public API and evolution](library-public-api-and-evolution.md). |
| <a id="bounded-context"></a>**Bounded context** | Boundary within which a model has one meaning; see [domain modeling](domain-modeling.md). |
| <a id="composition-root"></a>**Composition root** | Application/integration boundary that selects implementations; see [boundaries and dependencies](boundaries-and-dependencies.md). |
| <a id="contract"></a>**Contract** | Consumer-visible API, behavior, or dependency commitment; see [library public API and evolution](library-public-api-and-evolution.md). |
| <a id="data-object"></a>**Data object** | Public value/data carrier with documented semantics; see [type system and data modeling](type-system-and-data-modeling.md). |
| <a id="dependency"></a>**Dependency** | Compile-time, package, or runtime commitment; see [boundaries and dependencies](boundaries-and-dependencies.md). |
| <a id="documentation-contract"></a>**Documentation contract** | Complete XML documentation required by `IXM1001`–`IXM1005`; see [analyzer taxonomy](analyzer-taxonomy.md). |
| <a id="domain-service"></a>**Domain service** | Stateless domain behavior not owned by an entity/value object; see [domain modeling](domain-modeling.md). |
| <a id="entity"></a>**Entity** | Domain object defined by continuity of identity; see [domain modeling](domain-modeling.md). |
| <a id="immutable"></a>**Immutable** | State cannot change after construction; see [type system and data modeling](type-system-and-data-modeling.md). |
| <a id="interface"></a>**Interface** | Public capability shape owned by the policy requiring it; see [boundaries and dependencies](boundaries-and-dependencies.md). |
| <a id="logging"></a>**Logging** | Structured operational events, not an application configuration mechanism; see [observability and operability](observability-and-operability.md). |
| <a id="magic-string"></a>**Magic string** | Repeated semantic literal lacking one named/typed authority; see [design principles](design-principles.md). |
| <a id="memory"></a>**Memory** | Heap-storable buffer view for async/retained boundaries; see [performance and resource management](performance-and-resource-management.md). |
| <a id="nullability"></a>**Nullability** | Static promise about null values in a public contract; see [type system and data modeling](type-system-and-data-modeling.md). |
| <a id="ownership"></a>**Ownership** | Responsibility to dispose, return, or refrain from mutating a resource; see [performance and resource management](performance-and-resource-management.md). |
| <a id="package"></a>**Package** | NuGet distribution and version-selection unit; see [library public API and evolution](library-public-api-and-evolution.md). |
| <a id="port"></a>**Port** | Capability interface owned by the policy that needs it; see [boundaries and dependencies](boundaries-and-dependencies.md). |
| <a id="public-api"></a>**Public API** | Public/protected compile-time and documented consumer contract; see [library public API and evolution](library-public-api-and-evolution.md). |
| <a id="record"></a>**Record** | C# type with value-oriented generated members; see [type system and data modeling](type-system-and-data-modeling.md). |
| <a id="result"></a>**Result** | `FluentResults.Result` or `Result<T>` that represents an expected service outcome; see [FluentResults](../packages/fluentresults.md). |
| <a id="expected-failure"></a>**Expected failure** | Caller-actionable business or application outcome returned as a coded result; see [domain modeling](domain-modeling.md). |
| <a id="exceptional-failure"></a>**Exceptional failure** | Cancellation, programming fault, broken invariant, corrupt state, or unexpected technical failure that propagates as an exception; see [boundaries and dependencies](boundaries-and-dependencies.md). |
| <a id="error-code"></a>**Error code** | Immutable lowercase snake-case `public const string Code` owned by a concrete business error type; see [IXM3002](diagnostics/ixm3002.md). |
| <a id="failure-atomicity"></a>**Failure atomicity** | A failure leaves no undocumented partial state or false appearance of success; see [documentation, testing, and quality](documentation-testing-and-quality.md). |
| <a id="resource-lifetime"></a>**Resource lifetime** | Period in which a resource/buffer may safely be used; see [performance and resource management](performance-and-resource-management.md). |
| <a id="service"></a>**Service** | Public behavior coordinator with one primary responsibility; see [design principles](design-principles.md). |
| <a id="source-generated-logging"></a>**Source-generated logging** | `LoggerMessage`-generated static structured logging method; see [observability and operability](observability-and-operability.md). |
| <a id="span"></a>**Span** | Stack-only contiguous-memory view for synchronous work; see [performance and resource management](performance-and-resource-management.md). |
| <a id="srp"></a>**Single responsibility principle (SRP)** | One primary reason for a type/package to change; see [design principles](design-principles.md). |
| <a id="suppression"></a>**Suppression** | Narrow, justified, owned, reviewed exception to a diagnostic; see [documentation, testing, and quality](documentation-testing-and-quality.md). |
| <a id="value-object"></a>**Value object** | Domain object defined by attributes/value equality; see [domain modeling](domain-modeling.md). |
| <a id="xml-documentation"></a>**XML documentation** | C# API documentation emitted from `///` comments; see [analyzer taxonomy](analyzer-taxonomy.md). |

## Sources

- [Microsoft, CQRS pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs) — Accessed 2026-07-27.
- [Martin Fowler, Command Query Separation](https://martinfowler.com/bliki/CommandQuerySeparation.html) and [CQRS](https://martinfowler.com/bliki/CQRS.html) — Accessed 2026-07-27.
- [Microsoft, .NET API compatibility tools](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/overview) — Accessed 2026-07-27.
- [Microsoft, NuGet package compatibility rules](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/nuget-package-compatibility-rules) and [versioning .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/versioning) — Accessed 2026-07-27.
- Robert C. Martin, [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2011/11/22/Clean-Architecture.html) — Accessed 2026-07-27.
- Eric Evans, [*Domain-Driven Design*](https://www.domainlanguage.com/ddd/) — Accessed 2026-07-27.
