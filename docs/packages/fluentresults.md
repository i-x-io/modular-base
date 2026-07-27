# FluentResults

## Catalog entry

`FluentResults` **4.0.0** — direct catalog package; result-pattern types that model successful values and expected failures.

## Decision and scope

Use for expected, caller-actionable outcomes across application boundaries. Do not use it to conceal programming faults, cancellation, or infrastructure failures that need normal exception/telemetry handling.

## Recommended registration and use

Return `Result` for no-value outcomes and `Result<T>` for successful values. Inspect `IsSuccess`/`IsFailed` before reading `Value`; map a small, application-owned error taxonomy at the API boundary.

## Enterprise implementation guidance

Keep error messages safe for clients and retain diagnostic detail in structured logs. Define one mapping from domain failures to transport status/problem details. Preserve error codes rather than making callers parse prose.

## Integration with the catalog

Use `fluentvalidation.md` for request validation; translate validation failures into the application result contract at the boundary. `polly.md` reports resilience outcomes; it should not be substituted for domain results.

## Security, performance, AOT, trimming, and operations

Avoid recording secrets or customer data in errors that can cross process boundaries. `Value` throws for failed results; prefer explicit branching. No package AOT/trimming guarantee is documented; validate the actual published workload.

## Avoid

Do not wrap every exception as a generic failure, return successful results with error payloads, or use `ValueOrDefault` when a missing value would be ambiguous.

## Verification checklist

- Test success, expected failure, and unexpected exception paths.
- Assert API error mapping has stable codes and safe messages.
- Verify failed `Result<T>` paths never access `Value`.

## Sources

- https://www.nuget.org/packages/FluentResults/4.0.0 (Accessed 2026-07-27)
- https://github.com/altmann/FluentResults (Accessed 2026-07-27)
