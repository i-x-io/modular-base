# Observability and operability

## Purpose

Ensure libraries expose useful operational signals without leaking sensitive data, forcing an application logging stack, or creating avoidable runtime cost.

## Canonical definitions

### Operational signals

Logging records diagnostic events. Metrics measure numeric observations. Tracing correlates operations across boundaries. Structured logging carries named fields rather than formatted text. Source-generated logging uses `LoggerMessage` attributes to generate efficient structured logging methods.

## Related and contrasting terms

Logs are not an audit trail by default. Exceptions record failure information but do not replace an operational event. `ILogger` is an abstraction; a logging provider is application/infrastructure configuration.

Unexpected exceptions may be logged once at the boundary with enough context to act. Intermediate reusable layers should not log and rethrow the same exception unless each event adds independently actionable context. Never copy exception text, secrets, or sensitive payloads into caller-facing result messages; record safe error codes and structured context instead.

## Normative rules

- Libraries accept or depend on `ILogger` abstractions only where operational signals are part of the capability; they do not configure providers or build a service provider.
- Use source-generated logging for reusable, high-frequency, or parameterized log messages; use stable event IDs and named placeholders.
- Message templates are stable contracts: do not concatenate, interpolate dynamically, or log secrets/credentials/PII without an approved redaction policy.
- Keep logging, metric, and trace cardinality bounded and document any consumer-visible instrumentation.

## Library-focused examples

```csharp
[LoggerMessage(EventId = 10, Level = LogLevel.Information,
    Message = "Processed library item {ItemId}")]
private static partial void LogItemProcessed(ILogger logger, string itemId);
```

The method gives the compiler a stable template and avoids per-call parsing/allocation in common paths.

## Anti-patterns

Calling `BuildServiceProvider` in a reusable library, interpolating a log template, creating an unbounded metric label from input, and logging a token or full request body are forbidden.

## Review questions

- Which operational question does this signal answer?
- Are names, event IDs, and cardinality stable and safe?
- Is the logging dependency an abstraction rather than application configuration?

## Analyzer and build enforcement

`CA1848` and `CA2254` are errors, enforcing source-generated logging use and static templates where the rules apply. `CA1200` is error to prevent global namespace pollution. Logging policy complements security review; it does not classify data automatically.

## Authoritative references

- [Compile-time logging source generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
- [Logging guidance](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)

## Last research/access date

2026-07-27.
