# Architecture terminology

## Purpose and authority

This glossary gives future `IX.Modularity.*` libraries one shared vocabulary. A term being defined here does not, by itself, create a build rule, project type, package, or dependency. The normative requirements are in [architectural rules](architectural-rules.md) and the permitted project relationships are in [project structure](project-structure.md).

The A–Z glossary below is the canonical vocabulary. Each term has a native
Markdown heading for stable cross-document links. Status words such as
“normative” and “heuristic” describe the force of a term; they do not create
additional project roles or application architecture requirements.

## A

### Adapter

Code that translates between a port or contract and a concrete technology,
protocol, or external library. It is defined vocabulary and a project role with
specific dependency constraints; see [boundaries and dependencies](boundaries-and-dependencies.md#boundary-and-dependency).

### ADP — Acyclic Dependencies Principle

The component dependency graph has no cycles. This is normative and enforceable
when dependency checks exist.

### Aggregate

A consistency boundary with an aggregate root controlling changes to its
contained model. This is defined vocabulary.

### Analyzer

Compiler-time diagnostic tooling; see [analyzer policy](analyzer-policy.md).

### Anti-corruption layer (ACL)

Translation that prevents one bounded context or external model from leaking
into another. This is a heuristic; an `Adapter` may implement an ACL.

### API compatibility

Source, binary, and behavioral preservation for released contracts; see
[library public API and evolution](library-public-api-and-evolution.md).

### Application service

Orchestrates a use case, coordinates dependencies, and delegates domain
decisions to domain policy. This is defined vocabulary.

### Assembly

The CLR deployment and type-identity unit produced by a project. This is defined
vocabulary.

## B

### Behavioral compatibility

An update preserves documented and reasonably relied-on behavior. This is a
normative release concern that requires review and tests, not only API tooling.

### Binary compatibility

Previously compiled consumer code can run against the updated assembly without
a missing or changed public contract. This is a normative release concern.

### Bounded context

An explicit boundary within which a model and its terms have one meaning. This
is defined vocabulary and a heuristic for package or module boundaries; see
[domain modeling](domain-modeling.md).

### Boundary

Separates concerns that change for different reasons; see
[boundaries and dependencies](boundaries-and-dependencies.md).

## C

### CCP — Common Closure Principle

Put classes that change together for the same reason in the same component.
This is a heuristic.

### Clean architecture

Organizes code around business policies and controls source-code dependencies
so details depend on policies. This is defined vocabulary and a heuristic.

### Cohesion

The degree to which the contents of a unit belong together for one reason to
change. This is a heuristic; prefer cohesive projects and packages.

### Component

A releasable and independently versioned unit, normally a NuGet package with
one or more assemblies. It is not synonymous with a C# project.

### Composition root

The outermost place that selects implementations and assembles an object graph.
This is defined vocabulary. It belongs in an application or integration
boundary, never in an abstractions-only package.

### Context map

A description of relationships between bounded contexts. This is out of scope
unless a future multi-context system needs one.

### Contract

A consumer-visible API, behavior, or dependency commitment; see
[library public API and evolution](library-public-api-and-evolution.md).

### Coupling

The degree to which one unit must know about or change with another. This is a
heuristic; prefer narrow, explicit, acyclic dependencies.

### CQS

At an operation boundary, a query observes state and returns information; a
command changes state. This is a heuristic for public APIs and application
services.

### CQRS

Separates read and write models when their responsibilities justify it. It is
broader than CQS and can use one or separate stores. It is out of scope as a
required architecture and should be adopted only for a documented workload
need.

### CRP — Common Reuse Principle

Do not force consumers to depend on types they do not use. This is normative
for public package dependencies and a heuristic for internal layout.

## D

### Data object

A public value or data carrier with documented semantics; see
[type system and data modeling](type-system-and-data-modeling.md).

### Defined vocabulary

A name used consistently by this repository. It may describe a concept without
requiring a particular implementation.

### Dependency

A compile-time, package, or runtime commitment; see
[boundaries and dependencies](boundaries-and-dependencies.md).

### Documentation contract

Complete XML documentation required by `IXM1001`–`IXM1005`; see
[analyzer taxonomy](analyzer-taxonomy.md).

### Domain

The subject area and rules the software serves. This is defined vocabulary.

### Domain-driven design vocabulary

Terms that describe a domain model. They do not require every library to
contain a domain model.

### Domain event

A record of a meaningful fact that occurred in a domain. This is defined
vocabulary.

### Domain model

Gives library terms their stable meaning; see
[domain modeling](domain-modeling.md#domain-model).

### Domain service

Stateless domain behavior that does not naturally belong to one entity or value
object. This is defined vocabulary.

### DRY

Keep one authoritative representation of a rule or piece of knowledge. This is
normative for policy, contracts, and compatibility baselines; use judgment for
coincidental code similarity.

## E

### Enforceable

A normative rule that can be checked mechanically once a suitable project or
architecture test exists. An unenforced normative rule remains required.

### Entity

A domain object defined by continuity of identity. This is defined vocabulary;
see [domain modeling](domain-modeling.md).

### Error code

An immutable lowercase snake-case `public const string Code` owned by a concrete
business error type; see [IXM3002](diagnostics/ixm3002.md).

### Exceptional failure

Cancellation, programming fault, broken invariant, corrupt state, or unexpected
technical failure that propagates as an exception; see
[boundaries and dependencies](boundaries-and-dependencies.md).

### Expected failure

A caller-actionable business or application outcome returned as a coded result;
see [domain modeling](domain-modeling.md).

## F

### Failure atomicity

A failure leaves no undocumented partial state or false appearance of success;
see [documentation, testing, and quality](documentation-testing-and-quality.md#documentation-testing-and-quality).

### Framework detail

Implementation technology, not a neutral public contract.

## G

### Generated code

Compiler-produced output that is outside user-authored documentation
diagnostics.

## H

### Heuristic

A decision aid. It directs design review but is not an automatic rejection
criterion.

### Hexagonal architecture

Separates an application core from primary or driving and secondary or driven
adapters through ports. This is defined vocabulary and a heuristic. A repository
`Adapter` role is an implementation boundary, not proof that a system is
hexagonal.

## I

### Immutable

State cannot change after construction; see
[type system and data modeling](type-system-and-data-modeling.md).

### Interface

A public capability shape owned by the policy requiring it; see
[boundaries and dependencies](boundaries-and-dependencies.md).

## J

### Justification

Records why a narrow policy exception is necessary.

## K

### KISS

Prefer the smallest understandable design that satisfies the stated need. This
is a heuristic.

## L

### Layered architecture

Organizes code into responsibility layers; dependencies generally point inward
or downward according to the chosen layer model. This is defined vocabulary.
The repository uses its dependency direction, not a fixed set of application
layers.

### Logging

Structured operational events, not an application configuration mechanism; see
[observability and operability](observability-and-operability.md).

## M

### Magic string

A repeated semantic literal lacking one authoritative named or typed
representation; see [design principles](design-principles.md).

### Memory

A heap-storable buffer view for asynchronous or retained boundaries; see
[performance and resource management](performance-and-resource-management.md).

### Modular monolith

One deployable application with modules that retain explicit internal
boundaries. This is out of scope for this library repository. Libraries may
support one, but do not prescribe an application deployment topology.

## N

### Normative

A requirement for future library projects. A pull request must either comply or
record an approved exception.

### NuGet package

The distribution and version-selection unit consumed through NuGet. This is
defined vocabulary; one package can contain multiple assemblies.

### Nullability

A static public promise about absent values; see
[type system and data modeling](type-system-and-data-modeling.md#type-system-and-data-model).

## O

### Observability

Provides bounded, structured operational signals; see
[observability and operability](observability-and-operability.md#operational-signals).

### Onion architecture

Places domain policy at the center and infrastructure at outer rings;
dependencies point inward. This is defined vocabulary and a heuristic.

### Out of scope

Useful context that this repository intentionally does not prescribe.

### Ownership

Responsibility to dispose, return, or refrain from mutating a resource; see
[performance and resource management](performance-and-resource-management.md).

## P

### Package validation

SDK or API compatibility validation of a package or assembly against compatible
target frameworks or a baseline package. It is enforceable when enabled for a
package project.

### Performance

A measured workload property, not an intuition; see
[performance and resource management](performance-and-resource-management.md#memory-views-ownership-and-measurement).

### Port

An interface owned by the policy that needs a capability. This is defined
vocabulary and normally belongs in `Abstractions` or `Contracts`, not in an
adapter; see [boundaries and dependencies](boundaries-and-dependencies.md).

### Project

An MSBuild project that produces an assembly, package, analyzer, generator, or
test output. This is defined vocabulary. A project is an implementation unit,
not necessarily a release unit.

### Public API

Public and protected types and members that consumers can compile against, plus
documented behavior that consumers reasonably rely on. It is the normative
compatibility boundary and released consumer contract; see
[library public API and evolution](library-public-api-and-evolution.md#library-public-api).

### Public API baseline

A checked-in record of intentional shipped and pending public API surface, used
by public API analyzers. It is normative for packable projects under the
existing code-quality policy.

## Q

### Query

Observes state and returns information without a meaningful domain-state
mutation.

## R

### Record

A C# type with value-oriented generated members; see
[type system and data modeling](type-system-and-data-modeling.md).

### REP — Release Equivalence Principle

Units released together should be versioned and released together. This is
normative when choosing a package boundary.

### Repository

A collection-like abstraction for retrieving and persisting aggregates. This
is defined vocabulary; do not introduce one solely to mirror a database.

### Resource lifetime

The period in which a resource or buffer may safely be used; see
[performance and resource management](performance-and-resource-management.md).

### Result

`FluentResults.Result` or `Result<T>` represents an expected service outcome
without using an exception as ordinary control flow; see
[FluentResults](../packages/fluentresults.md).

## S

### SAP — Stable Abstractions Principle

A stable component should be abstract enough to avoid resisting change, without
becoming empty ceremony. This is a heuristic.

### SDP — Stable Dependencies Principle

Depend in the direction of greater stability. This is a heuristic; use stable
contracts and abstractions to avoid implementation-driven dependency direction.

### Separation of concerns (SoC)

Place concerns that change for different reasons behind distinct boundaries.
This is normative at the project-boundary level; see
[architectural rules](architectural-rules.md).

### Service

A public behavior coordinator with one primary responsibility; see
[design principles](design-principles.md).

### Single responsibility principle (SRP)

One primary reason for a type or package to change; see
[design principles](design-principles.md#responsibility-cohesion-and-separation-of-concerns).

### SOLID

The family of object-oriented principles: single responsibility, open/closed,
Liskov substitution, interface segregation, and dependency inversion. This is a
heuristic for examining responsibilities and dependencies, not a substitute for
a concrete rule.

### Source compatibility

Consumer source still compiles after an update. This is defined vocabulary and
is insufficient on its own for a released package.

### Source-generated logging

A `LoggerMessage`-generated static structured logging method; see
[observability and operability](observability-and-operability.md).

### Span

A stack-only contiguous-memory view for synchronous work; see
[performance and resource management](performance-and-resource-management.md).

### Suppression

A narrow, justified, owned, reviewed exception to a diagnostic; see
[documentation, testing, and quality](documentation-testing-and-quality.md).

## T

### Trace

Correlates one operation across component boundaries.

## U

### Ubiquitous language

Shared, context-specific language used by domain experts and developers. This
is a heuristic; use it for public domain-facing names.

## V

### Value object

A domain object defined by attributes or value equality rather than continuity
of identity. This is defined vocabulary; see [domain modeling](domain-modeling.md).

## W

### Warning

A diagnostic severity; repository policy can promote it to a build error.

## X

### XML documentation

The consumer-facing C# API contract emitted from `///` documentation comments;
see [analyzer taxonomy](analyzer-taxonomy.md).

## Y

### YAGNI

Do not add capability until a current requirement needs it. This is normative
against speculative public abstractions and premature project splits.

## Z

### Zero-cost abstraction

A performance claim that requires measurement, not a default assumption.

## Sources

- [Microsoft, CQRS pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs) — Accessed 2026-07-27.
- [Martin Fowler, Command Query Separation](https://martinfowler.com/bliki/CommandQuerySeparation.html) and [CQRS](https://martinfowler.com/bliki/CQRS.html) — Accessed 2026-07-27.
- [Microsoft, .NET API compatibility tools](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/overview) — Accessed 2026-07-27.
- [Microsoft, NuGet package compatibility rules](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/nuget-package-compatibility-rules) and [versioning .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/versioning) — Accessed 2026-07-27.
- Robert C. Martin, [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2011/11/22/Clean-Architecture.html) — Accessed 2026-07-27.
- Eric Evans, [*Domain-Driven Design*](https://www.domainlanguage.com/ddd/) — Accessed 2026-07-27.
