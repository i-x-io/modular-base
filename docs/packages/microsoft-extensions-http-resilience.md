# Microsoft.Extensions.Http.Resilience

## Catalog entry

`Microsoft.Extensions.Http.Resilience` **10.8.0** — direct catalog package; HTTP-specific resilience handlers for `IHttpClientFactory`, built on the Microsoft resilience/Polly integration.

## Decision and scope

Use as the default resilience integration for outbound `HttpClient` dependencies. It provides HTTP-aware handlers; it does not determine whether a particular request is safe to retry.

## Recommended registration and use

Attach a standard or custom resilience handler to a named/typed `HttpClient` at the composition root. Start with the standard handler only after reviewing its defaults for the dependency; remove existing resilience handlers before replacing a policy.

## Enterprise implementation guidance

Name clients by remote dependency, set request/attempt/total time budgets, and narrowly classify retriable status codes and exceptions. Restrict retries to idempotent or otherwise deduplicated operations. Add dependency-specific telemetry and health checks instead of hiding an outage with large retry counts.

## Integration with the catalog

This is the HTTP-specific companion to `microsoft-extensions-resilience.md` and is preferred over generic `polly.md` wiring for `HttpClient`. `polly-extensions.md` remains appropriate for non-HTTP dependencies.

## Security, performance, AOT, trimming, and operations

Handlers can generate extra traffic during an outage; bound attempts and budgets. Keep authorization/request content replayability in mind before enabling retry or hedging. Observe handler telemetry through the host's logging/metering configuration. Publish and exercise the configured client in the intended trim/AOT mode.

## Avoid

Do not apply the same handler twice, retry non-replayable/unsafe requests, or share one generic policy across dependencies with different availability and latency requirements.

## Verification checklist

- Test retryable/non-retryable status codes, transport errors, cancellation, and budget expiry.
- Assert each named client has exactly its intended handler chain.
- Verify telemetry identifies the remote dependency and resilience outcome.

## Sources

- https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience/10.8.0 (Accessed 2026-07-27)
- https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience (Accessed 2026-07-27)
- https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Http.Resilience (Accessed 2026-07-27)
