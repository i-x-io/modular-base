# Project structure

## Scope

This document defines the project roles and permitted dependency direction for future `IX.Modularity.*` library projects. It deliberately supports an adaptive layout: the repository starts with one project when that is the smallest coherent boundary and introduces role-specific projects only when a real dependency, consumer, release, or test boundary exists.

The current repository intentionally contains `test/IX.Modularity.Architecture.Tests/IX.Modularity.Architecture.Tests.csproj` as its one `ArchitectureTest` project and contains no `src/` production projects. The production paths below are target conventions, not a claim that they already exist.

## Project roles

Every future project declares exactly one `IXModularityProjectRole` value. The value is a repository-controlled classification, not a framework-provided .NET enum. Use the names exactly as written.

| `IXModularityProjectRole` value | Responsibility | Typical output | Must not own |
| --- | --- | --- | --- |
| `Library` | Cohesive reusable implementation and public capability. | Packable library assembly/package. | Application composition, vendor-specific implementations that deserve a separate adapter. |
| `Contracts` | Stable cross-boundary messages, DTOs, events, request/response shapes, and shared semantic types. | Packable contracts assembly/package. | Infrastructure implementation, service registration, persistence or transport details. |
| `Abstractions` | Ports, interfaces, small capability contracts, and shared abstractions owned by policy. | Packable abstractions assembly/package. | Concrete adapters, DI/container setup, vendor SDK types. |
| `Adapter` | A technology-specific implementation or translation boundary. | Usually packable integration assembly/package. | Composition-root decisions or technology leakage into neutral contracts. |
| `Integration` | Integration-facing composition, registration extensions, or a deliberate facade that assembles library pieces. | Packable integration assembly/package when reusable. | Domain/policy ownership that belongs in `Library`, `Contracts`, or `Abstractions`. |
| `Testing` | Reusable testing support, fakes, fixtures, builders, or test extensions for consumers. | Packable testing assembly/package. | Production-only capability or a dependency required by production consumers. |
| `Analyzer` | Roslyn diagnostics and code fixes. | Analyzer package assets. | Runtime library implementation or analyzer behavior disguised as normal runtime code. |
| `SourceGenerator` | Roslyn incremental/source-generation behavior. | Analyzer/source-generator package assets. | Runtime reflection-based replacement for generated behavior. |
| `Test` | Tests for one or more production projects. | Non-packable test assembly. | Public production API ownership. |
| `ArchitectureTest` | Tests that assert project, assembly, namespace, or dependency architecture. | Non-packable test assembly. | Production functionality or package assets. |

`Testing` is reusable test support; `Test` executes tests. `ArchitectureTest` is a `Test` specialization used only to validate architectural constraints. `Analyzer` and `SourceGenerator` are compiler tooling roles; they are not normal runtime dependencies.

`Analyzer` produces analyzer assets consumed by the compiler, and `SourceGenerator` produces compiler-time generated source. Their project references to compiler-tool peers use explicit analyzer-loading metadata and must never become runtime assembly references. Their normal test projects may reference the implementation as a test subject, but that does not change consumer package behavior.

## Adaptive layouts

### Start: one cohesive library

Use a single project when one consumer-facing capability has no technology-specific boundary, independent contracts, or separately useful test support.

```text
src/
  IX.Modularity.Feature/
test/
  IX.Modularity.Feature.Tests/
```

Role assignments: `Library`, `Test`. Do not pre-create `Contracts`, `Abstractions`, or `Adapter` projects merely to resemble a preferred architecture diagram.

### Split a real technology boundary

Split when an implementation needs a vendor/transport/storage dependency that the core capability must not impose on every consumer.

```text
src/
  IX.Modularity.Feature/
  IX.Modularity.Feature.Abstractions/
  IX.Modularity.Feature.Adapters.Vendor/
  IX.Modularity.Feature.Integrations.Vendor/
test/
  IX.Modularity.Feature.Tests/
  IX.Modularity.Feature.Adapters.Vendor.Tests/
```

`Adapters.Vendor` implements the contract owned by `Abstractions` or `Feature`. `Integrations.Vendor` is optional and owns reusable registration/facade work. The application remains the final composition root.

### Split an independently shared contract

Add `Contracts` when multiple packages or external consumers must exchange a stable semantic model without taking the full implementation.

```text
src/
  IX.Modularity.Feature.Contracts/
  IX.Modularity.Feature/
  IX.Modularity.Feature.Adapters.Transport/
  IX.Modularity.Feature.Testing/
test/
  IX.Modularity.Feature.Tests/
  IX.Modularity.Architecture.Tests/
```

The contract package must remain small and portable because each added dependency becomes a consumer commitment. Add `Testing` only when it provides stable value to other packages or external consumers; otherwise keep helpers local to `Test`.

### Compiler tooling

Create compiler-tooling projects only for a separately distributable compiler-time concern:

```text
src/
  IX.Modularity.Feature/
  IX.Modularity.Feature.Analyzers/
  IX.Modularity.Feature.Generators/
test/
  IX.Modularity.Feature.Analyzers.Tests/
  IX.Modularity.Feature.Generators.Tests/
```

Keep analyzer/generator dependencies and loading behavior isolated from normal library runtime assets. A normal library must not depend on an analyzer or source-generator project as a runtime project reference.

## Dependency matrix

The matrix applies to direct **source/project references**. `Yes` means the role may reference the target role when it needs the target's public contract; `No` means it must not. External package references remain subject to [dependency policy](dependency-policy.md) and [architectural rules](architectural-rules.md).

| From \\ To | Library | Contracts | Abstractions | Adapter | Integration | Testing | Analyzer | SourceGenerator | Test | ArchitectureTest |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Library | Yes | Yes | Yes | No | No | No | No | No | No | No |
| Contracts | No | Yes | Yes | No | No | No | No | No | No | No |
| Abstractions | No | No | Yes | No | No | No | No | No | No | No |
| Adapter | Yes | Yes | Yes | No | No | No | No | No | No | No |
| Integration | Yes | Yes | Yes | Yes | No | No | No | No | No | No |
| Testing | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | No | No |
| Analyzer | No | Yes | Yes | No | No | No | Yes | Yes | No | No |
| SourceGenerator | No | Yes | Yes | No | No | No | Yes | Yes | No | No |
| Test | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| ArchitectureTest | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes |

Notes:

- `Library` may reference only `Library`, `Contracts`, and `Abstractions`. It may depend on `Contracts` or `Abstractions` only when those are independently necessary. If it owns its only contract, keep that type in the library until a real boundary appears.
- `Contracts` may reference only `Contracts` and `Abstractions`. `Abstractions` may reference only `Abstractions`.
- `Adapter` depends inward on the capability it implements. It may reference `Library` when the library owns the port; it does not make `Library` reference the adapter.
- `Integration` is the only production role that may reference an adapter. It remains reusable composition support, not the application's final composition root.
- `Testing`, `Test`, and `ArchitectureTest` never become production dependencies or package dependencies required by production consumers.
- The permissions in the matrix do not permit cycles. The complete project-reference graph remains acyclic, including test projects.
- `Analyzer` and `SourceGenerator` may share portable contracts/abstractions, but must not take a runtime dependency on `Library`, `Adapter`, or `Integration`.
- A `Test` project may reference its production subject and testing/tooling dependencies. An `ArchitectureTest` may also inspect `Test` assemblies when that is necessary to enforce test-structure rules. Neither permission authorizes a reverse production dependency.
- Add explicit `ProjectReference` items to the architecture-test project when compiled-assembly rules are introduced for a production library. The repository-structure rules discover project files independently; do not retain an unmatched wildcard reference in an otherwise empty `src/` tree.

## Naming and release guidance

- Place production projects below `src/` and test projects below `test/`.
- Name the assembly and package after the consumer-facing capability. Append a role suffix only where it clarifies a separately consumable boundary, such as `.Contracts`, `.Abstractions`, `.Adapters.<Technology>`, `.Integrations.<Product>`, `.Testing`, `.Analyzers`, or `.Generators`.
- Treat a project reference as a design-time implementation relationship. Decide package boundaries and package dependencies independently, using REP and CRP from [architecture terminology](terminology.md).
- Pack only roles intended for external consumption. `Test` and `ArchitectureTest` are always non-packable. A `Testing` project is packable only when external consumers need the support it contains.
- Every packable project follows the repository's existing package metadata, public API baseline, Central Package Management, lock-file, and package-validation policies.

## Sources

- [Microsoft, NuGet package compatibility rules](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/nuget-package-compatibility-rules) — Accessed 2026-07-27.
- [Microsoft, API compatibility tools](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/overview) — Accessed 2026-07-27.
- [Microsoft, Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) — Accessed 2026-07-27.
- [Microsoft, source generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) and [Roslyn analyzers](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix) — Accessed 2026-07-27.
