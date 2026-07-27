# Polly

## Catalog entry

`Polly` **8.7.0** — direct catalog package; composable resilience pipelines for retry, timeout, circuit breaker, rate limiter, hedging, and fallback strategies.

## Decision and scope

Use for explicit transient-fault policies at a named dependency boundary. It does not make an operation safe to repeat or replace capacity, correctness, and back-pressure design.

## Recommended registration and use

Build a named, reusable `ResiliencePipeline` once and execute the bounded operation through it. Compose strategies intentionally: a retry that handles timeout rejections belongs outside the timeout it retries. Use `Polly.Extensions` when DI registration/provider lookup is needed.

## Enterprise implementation guidance

Classify handled outcomes narrowly, set time budgets from the caller outward, respect cancellation, and align retries with idempotency. Instrument pipeline events and use circuit state/metrics for operational decisions, not just exceptions.

## Integration with the catalog

Use `polly-extensions.md` for DI pipelines. For `HttpClient`, prefer `microsoft-extensions-http-resilience.md`; it applies standardized HTTP-aware strategies. `mailkit.md` needs outbox semantics before any send retry.

## Security, performance, AOT, trimming, and operations

Pipelines are intended for reuse; constructing them per operation adds work and fragments state. Retries amplify load and can expose duplicate side effects. Configure telemetry with the host logging/metering pipeline. Validate the exact strategies in trimmed/NativeAOT publishing as part of release verification.

## Avoid

Do not retry validation/authentication failures, use infinite retries, stack overlapping timeouts without a budget, or wrap non-idempotent writes blindly.

## Verification checklist

- Test handled and unhandled outcomes, cancellation, and total time budget.
- Test circuit-open behavior and recovery under controlled faults.
- Verify retry does not duplicate externally visible side effects.

## Sources

- https://www.nuget.org/packages/Polly/8.7.0 (Accessed 2026-07-27)
- https://www.pollydocs.org/ (Accessed 2026-07-27)
- https://github.com/App-vNext/Polly/blob/main/docs/pipelines/index.md (Accessed 2026-07-27)
