# Service Results and Exception Policy Design

## Status

Approved design for implementation planning on 2026-07-28.

This decision makes FluentResults the required outcome model for externally visible service operations. Expected business and application outcomes are represented as `Result` or `Result<T>` values with stable, coded error objects. Exceptions remain reserved for cancellation, programming and contract faults, broken invariants, corrupt state, and unexpected technical or infrastructure failures.

## Goals

- Make service contracts explicit about success and expected failure.
- Prevent raw values, Boolean success flags, nullable failure signals, and ordinary exceptions from becoming service-layer business outcome channels.
- Give callers stable machine-readable business error codes without coupling library contracts to HTTP or another transport.
- Preserve exception type, stack, and causal information for unexpected failures.
- Reject broad exception swallowing and string-only failures through compiler diagnostics.
- Apply the policy consistently to service interfaces and public concrete service methods.
- Keep enforcement compatible with the repository-owned analyzer package and project-role system.

## Non-goals

- This policy does not prohibit exceptions throughout the repository.
- It does not convert technical failures, cancellation, or programming defects into failed results.
- It does not define HTTP status codes, endpoint response envelopes, global exception handlers, or application hosting policy.
- It does not require properties, events, constructors, private helpers, or non-service types to return FluentResults values.
- It does not introduce a repository-specific result abstraction around FluentResults.
- It does not make human-readable error messages a compatibility contract.
- It does not automatically rewrite existing service signatures or exception handling.

## Scope and terminology

A **service type** uses the repository analyzer's existing semantic classification:

- an externally visible class whose name ends in `Service`; or
- a class that directly or indirectly implements an interface named using the `I*Service` convention; or
- an externally visible interface named using the `I*Service` convention.

A **service operation** is an externally visible ordinary method declared by a service type. Constructors, property accessors, indexers, event accessors, operators, private helpers, and generated code are outside this return-shape rule.

An **expected outcome** is a caller-actionable business or application decision that is part of ordinary control flow, such as validation failure, a missing business object, a rejected state transition, or a conflict with a documented rule.

An **exceptional failure** is cancellation, a programming or contract fault, a broken invariant, corrupt state, or an unexpected technical or infrastructure failure that the current layer cannot translate completely and honestly into a documented expected outcome.

An **error code** is an immutable lowercase snake-case identifier owned by a concrete business error type. It is the machine-readable compatibility contract for the failure; the message is explanatory text only.

## Normative service outcome policy

Externally visible service operations MUST return one of these shapes:

- `FluentResults.Result`
- `FluentResults.Result<T>`
- `Task<FluentResults.Result>`
- `Task<FluentResults.Result<T>>`
- `ValueTask<FluentResults.Result>`
- `ValueTask<FluentResults.Result<T>>`

The generic wrappers and result types MUST be the actual framework types, resolved by symbol identity rather than source spelling. Aliases and fully qualified names therefore behave consistently.

Service operations MUST NOT expose raw `T`, `Task<T>`, `ValueTask<T>`, `bool`, nullable values, tuples, or exceptions as the representation of an expected business outcome. A stream or callback contract is not silently exempt: if a future service genuinely needs another result shape, that shape requires an explicit architecture decision before it is introduced.

When an interface owns a service operation, its implementation method is not reported a second time for the same return contract. Additional externally visible methods declared only on a concrete service remain in scope.

Changing an existing public operation from a raw return type to `Result` is a breaking public API change. Such migrations require the repository's normal compatibility and semantic-versioning process.

## Coded business errors

An expected failed result MUST contain one or more concrete business error objects derived from `FluentResults.Error`. String-only calls such as `Result.Fail("customer not found")` are prohibited. Direct construction of the unclassified base `Error` type for a business failure is also prohibited.

Each concrete business error type MUST expose a stable declaration in this form:

```csharp
public sealed class CustomerNotFoundError : Error
{
    public const string Code = "customer_not_found";

    public CustomerNotFoundError()
        : base("The requested customer was not found.")
    {
    }
}
```

The `Code` field MUST be `public const string`. Its value MUST match `^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$`. Codes MUST describe the error category and MUST NOT contain customer identifiers, tenant identifiers, localized text, timestamps, or other dynamic values. Once published, a code is a compatibility contract and MUST NOT be repurposed for a different meaning.

The concrete error type and its `Code` field provide the authoritative machine-readable identity. An error MAY duplicate the code into FluentResults metadata when a consumer needs metadata-based traversal, but metadata population is not required by this design and does not replace the typed error contract.

Messages SHOULD be safe, concise, and useful to a caller, but callers MUST branch on the concrete error type or stable code rather than message text. Sensitive diagnostic detail belongs in protected telemetry or the preserved exception chain, not in a business error message.

Results MAY contain multiple errors. Boundaries MUST preserve all meaningful `Result.Errors` rather than arbitrarily reducing a multi-error result to its first error.

Direct calls that create failed results are mechanically enforceable. Indirect or generic failure factories that the analyzer cannot prove safe require a narrow, justified suppression at the call site. A suppression is not permission to use string-only or uncoded failures.

## Exception policy

Expected validation, domain, and application decisions MUST be returned as coded failed results. Examples include ordinary invalid input, a requested entity not existing, an already-completed operation, or a documented business conflict.

An exception MAY be translated into a failed result only when all of these conditions hold:

- the caught exception type is specific;
- the exception represents a fully understood, documented, caller-actionable outcome at that boundary;
- the translation does not hide cancellation, corrupt state, a programming defect, or an unknown infrastructure condition; and
- the produced failure uses a concrete coded business error.

`OperationCanceledException` and derived cancellation exceptions MUST propagate as cancellation and MUST NOT be translated into failed results.

Unexpected infrastructure failures, programming faults, contract violations, corrupt state, and broken invariants MUST propagate as exceptions. Code MAY throw explicitly for these genuine exceptional conditions. The thrown type and message must remain appropriate to the violated technical contract, and exception-based preconditions should use established .NET guard patterns.

A broad `catch (Exception)` or untyped `catch` MAY add safe, structured, independently actionable context and then use a bare `throw;`. Every reachable path in that catch body MUST terminate in a bare rethrow. It MUST NOT return success, return a failed result, throw a replacement exception, discard the exception, or use `throw exception;`, because those forms either swallow the failure or damage its original stack.

Catching a specific exception does not automatically justify translation. Reviewers must still verify that it represents a documented expected outcome. Catching `OperationCanceledException` solely to perform local cleanup and then using a bare rethrow remains valid.

## State consistency and failure atomicity

Services MUST protect their invariants regardless of whether completion is represented by a result or an exception.

- Transactional or externally visible state changes MUST be committed only after the operation has established success.
- A failed result MUST NOT leave undocumented partial state.
- An exception MUST NOT leave the component in a state that falsely appears successful.
- Cleanup and ownership rules MUST work for success, failed results, exceptions, and cancellation.
- Idempotency and retry behavior remain explicit behavioral contracts; returning `Result` does not make an operation automatically safe to retry.

## Boundary behavior

Reusable libraries return typed results and propagate exceptional failures. An outer application or transport boundary is responsible for mapping business error types or codes to HTTP responses, messages, UI states, or another delivery protocol. That mapping must remain outside the reusable service and contract layers.

Libraries MUST NOT introduce HTTP status codes, controller result types, global exception middleware, logging-provider configuration, or transport-specific envelopes to implement this policy.

An unexpected exception may be logged once at the boundary that has enough context to act on it. Intermediate library layers SHOULD NOT log and rethrow the same exception unless each log adds independently actionable context. Logs MUST remain structured and must not expose secrets or sensitive payloads.

## Analyzer diagnostics

The repository analyzer will add three stable diagnostics. They default to warning in the distributed analyzer package and are promoted to error by this repository's `.editorconfig`.

### IXM3001: Service operation must return FluentResults

`IXM3001` reports an externally visible service operation whose return type is not one of the approved synchronous or asynchronous result shapes.

The diagnostic:

- uses the existing service classification and external-visibility rules;
- resolves FluentResults, `Task`, and `ValueTask` by metadata identity;
- reports the interface declaration when an interface owns the contract;
- avoids a duplicate report on the corresponding implementation method;
- still reports extra public methods declared only by a concrete service;
- excludes constructors, properties, indexers, events, operators, private helpers, ordinary non-service implementations, and generated code;
- deduplicates partial declarations; and
- tolerates missing references, syntax errors, and incomplete compilations without crashing.

### IXM3002: Business failure must use a coded error

`IXM3002` reports direct FluentResults failure creation in repository-owned production code when the failure:

- uses a string-only overload;
- directly creates or passes the unclassified `FluentResults.Error` base type;
- uses a concrete `Error` subtype without a `public const string Code`; or
- uses a code value that is empty or does not follow lowercase snake case.

The diagnostic follows conversions and common overload forms through semantic symbols rather than matching method text. It validates direct, statically knowable failure construction. It does not pretend to prove arbitrary factory internals or runtime collections; those paths are governed by code review and narrowly justified suppressions.

### IXM3003: Broad exception catch must rethrow

`IXM3003` reports a broad `catch (Exception)` or untyped catch unless every reachable path ends with the original exception being propagated by a bare `throw;`.

Logging or cleanup statements before the bare rethrow are permitted. A return statement, normal fall-through, replacement throw, `throw caughtException;`, or conditional path that can swallow the failure is prohibited. Catch filters do not exempt a broad catch from this structural rule.

Specific exception catches are not blanket-rejected by `IXM3003`. Existing compiler and analyzer rules plus code review govern stack preservation, cancellation, and the semantic validity of specific exception translation.

## Analyzer implementation constraints

The analyzer assembly MUST NOT take a runtime dependency on FluentResults. It recognizes the relevant types and APIs through Roslyn symbols and metadata names. This preserves the analyzer-only package layout and prevents consumer dependency leakage.

The implementation MUST enable concurrent analysis, honor cancellation, exclude generated code, deduplicate partial declarations, and remain fail-safe for malformed or incomplete source. The diagnostics require stable titles, categories, messages, descriptions, help links, release entries, taxonomy entries, and individual documentation pages. Version `0.1.0` will not add code fixes for these rules.

## Dependency and project-role policy

FluentResults becomes an intentional public dependency for project roles that may declare or implement services:

- `Library`
- `Contracts`
- `Abstractions`
- `Adapter`
- `Integration`

Allowing FluentResults in `Contracts` and `Abstractions` is a deliberate exception to their usual package-neutral posture because the result shape is part of the public service contract. The exception is narrow: it does not authorize infrastructure, hosting, transport, persistence, or logging implementation dependencies in those roles.

The central package catalog remains the version authority. Package-role architecture tests and documentation must be updated together so this exception is explicit and testable.

## Documentation and configuration changes

Implementation must update the authoritative locations together:

- analyzer descriptors, supported-diagnostic registration, and release metadata;
- analyzer taxonomy and analyzer index;
- one diagnostic page for each of `IXM3001`, `IXM3002`, and `IXM3003`;
- `.editorconfig` repository severities;
- the FluentResults package guide;
- architecture pages covering service contracts, errors, exceptions, failure atomicity, public API evolution, and project-role dependencies;
- terminology links for result, expected failure, exceptional failure, error code, and failure atomicity; and
- architecture documentation validation expectations and fixed inventories.

Normative prose must distinguish mechanical enforcement from semantic review. The analyzer can validate shapes and statically visible patterns; reviewers validate whether a failure is genuinely expected, whether a specific exception translation is honest, whether messages are safe, and whether state transitions are failure-atomic.

## Verification strategy

Analyzer tests must cover at least:

- every allowed service return shape and representative rejected raw, nullable, Boolean, tuple, and asynchronous shapes;
- interface-owned methods, inherited service interfaces, indirect service implementations, extra concrete public methods, and ordinary non-service implementations;
- visibility boundaries, partial declarations, generated code, malformed source, missing dependencies, cancellation, and concurrent analyzer execution;
- string-only failures, direct base `Error`, missing or non-constant codes, empty codes, invalid casing and separators, dynamic values, valid coded errors, multiple errors, overload variations, and justified suppression;
- broad typed and untyped catches that fall through, return results, return success, throw replacements, use `throw exception;`, or conditionally swallow;
- broad catches that log or clean up and then use bare `throw;` on every path;
- specific exception translation and cancellation rethrow cases; and
- repository severity configuration for all three diagnostics.

Architecture tests must cover the new FluentResults role allow-list, package-guide consistency, diagnostic documentation and help links, analyzer taxonomy, release metadata, and documentation navigation.

Real compiler probes must demonstrate that:

- a correctly documented service returning a coded `Result<T>` builds;
- each invalid return shape fails with `IXM3001`;
- string-only and uncoded failures fail with `IXM3002`;
- a broad swallowed exception fails with `IXM3003`;
- a broad catch that safely logs and uses bare `throw;` builds;
- a specific understood exception can be translated to a coded result;
- cancellation is propagated; and
- an external consumer receives the same analyzer behavior from the packed analyzer-only NuGet package.

The complete repository verification remains the Makefile workflow: formatting verification, validation, Debug and Release builds and tests, audit, outdated-package check, SBOM generation, package creation, package-content inspection, and clean-consumer smoke tests.

## Security and operability considerations

Stable error codes reduce dependence on mutable or localized messages, but codes themselves must not disclose sensitive values. Result messages and logging properties must be safe for their intended audience. Unexpected exceptions retain their causal chains for protected diagnostics and must not be copied wholesale into caller-facing results.

Broad-catch enforcement prevents silent corruption and misleading success. Failure-atomic review protects against partial state changes. The policy intentionally leaves exception-to-transport mapping at the outer boundary, where disclosure, retry, and observability policy can be applied with full operational context.

## Alternatives considered

### Documentation-only policy

Documentation alone would express the distinction between outcomes and exceptions but would allow service signatures, string-only failures, and swallowed exceptions to drift. It was rejected because the selected rules have useful, deterministic semantic shapes that the repository analyzer can enforce.

### Blanket prohibition on throwing or catching exceptions

A blanket rule would incorrectly convert cancellation, programming faults, broken invariants, and unexpected infrastructure failures into ordinary business results. It would also prevent safe logging or cleanup followed by a stack-preserving rethrow. It was rejected in favor of enforcing the boundary between expected outcomes and exceptional failures.

### Repository-owned result wrapper

Wrapping FluentResults would add another abstraction, conversion surface, and compatibility contract without providing a requirement that FluentResults cannot meet. It was rejected; service APIs intentionally expose FluentResults.

## Acceptance criteria

The implementation is complete when:

- all externally visible service methods use an approved FluentResults return shape;
- ordinary interface implementations remain exempt unless independently classified as services;
- direct business failures use concrete coded errors and string-only failures fail the build;
- broad catches cannot swallow, replace, or convert unexpected exceptions;
- cancellation and unexpected technical failures remain exception paths;
- FluentResults is explicitly allowed only in the approved project roles;
- diagnostics, documentation, configuration, package metadata, release tracking, and indexes remain synchronized;
- analyzer, architecture, compiler-smoke, package, and clean-consumer tests cover the policy; and
- the full Makefile verification pipeline passes without unrelated changes or temporary probe files.
