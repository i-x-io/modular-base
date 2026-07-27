# Polly

## Catalog entry

`Polly` **8.7.0** — direct catalog package; composable resilience pipelines for retry, timeout, circuit breaker, rate limiter, hedging, and fallback strategies.

- **Owner:** IX
- **Last reviewed:** 2026-07-27
**Review trigger:** `Polly` version changes, target-framework changes, or resilience strategy/telemetry semantic changes.

## Decision and scope

Use for explicit transient-fault policies at a named non-HTTP dependency boundary. It does not make an operation safe to repeat or replace capacity, correctness, durable messaging, and back-pressure design. For `HttpClient`, prefer the HTTP-specific catalog integration.

## Recommended registration and use

With central package management, add the versionless project reference:

```xml
<ItemGroup>
  <PackageReference Include="Polly" />
</ItemGroup>
```

Build a pipeline once and reuse it. Strategies execute in the order added, outermost to innermost. In this example the total timeout covers the complete operation, retry can handle an inner attempt timeout, the circuit breaker observes dependency attempts, and the inner rate limiter controls every attempt:

```csharp
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
    .AddTimeout(TimeSpan.FromSeconds(8))
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder()
            .Handle<IOException>()
            .Handle<TimeoutRejectedException>(),
        MaxRetryAttempts = 2,
        Delay = TimeSpan.FromMilliseconds(200),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        ShouldHandle = new PredicateBuilder()
            .Handle<IOException>()
            .Handle<TimeoutRejectedException>(),
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 20,
        BreakDuration = TimeSpan.FromSeconds(15)
    })
    .AddConcurrencyLimiter(permitLimit: 50, queueLimit: 0)
    .AddTimeout(TimeSpan.FromSeconds(2))
    .Build();

await pipeline.ExecuteAsync(
    static async token => await CallDependencyAsync(token),
    cancellationToken);
```

Use predicates for known transient exceptions or results rather than `Handle<Exception>()`. Pass the supplied token into every asynchronous API. A `TimeoutRejectedException` comes from Polly's timeout strategy; caller cancellation remains cancellation and should not be retried.

## Enterprise implementation guidance

Start from the caller's deadline and allocate a total budget, maximum attempts, attempt duration, and retry delays that fit inside it. Decide whether the breaker should count each attempt or each complete logical execution, then place it inside or outside retry deliberately. Similarly, place a rate limiter inside retry when every dependency attempt consumes capacity; place it outside when the protected resource is the complete logical operation.

Common production workflow:

1. Define one pipeline owner and one stable dependency name.
2. Classify transient faults and prove the operation is idempotent or deduplicated.
3. Add a total timeout, then the smallest necessary set of strategies.
4. Load-test retry amplification, breaker sampling thresholds, rate-limit rejection, and recovery.
5. Observe final outcomes and strategy events; tune from measured latency and failure distributions.

Pipelines are thread-safe and intended for reuse. Use `CircuitBreakerStateProvider` only for observation and `CircuitBreakerManualControl` only for an explicit operational isolation workflow; do not make normal request logic branch on breaker state before execution.

### Configuration reference

| Setting | Purpose | Default behavior | Production guidance | Reload | Sensitive | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| `ShouldHandle` | Selects transient outcomes | Strategy-specific predicate | Handle the narrowest proven transient set | Pipeline rebuild | No | Too broad retries permanent faults; too narrow misses recovery |
| Retry attempts/delay/backoff/jitter | Controls repeated work | Strategy defaults | Bound attempts and use jitter for shared dependencies | Pipeline rebuild | No | Exhaustion returns/throws final outcome |
| Attempt/overall timeout | Bounds work | Only present when configured | Pass caller tokens and fit all budgets hierarchically | Pipeline rebuild | No | `TimeoutRejectedException`/cancellation |
| Breaker ratio/window/throughput/duration | Controls circuit state | Strategy defaults | Calibrate against real traffic and test low-volume behavior | Pipeline rebuild | No | Open circuit rejects calls |
| Limiter permits/queue | Bounds concurrency | Strategy-specific | Prefer no or tightly bounded queue | Pipeline rebuild | No | Rejection under saturation |

### Upgrade and rollback

For v7-to-v8 migration, translate policy wraps into ordered resilience pipelines and use documented interoperability only as a temporary tested boundary. For patch upgrades, fault-test handled outcomes, strategy order, attempt counts, cancellation, and telemetry. Roll back compatible Polly companion pins and strategy configuration together.

## Integration with the catalog

Use `polly-extensions.md` for DI-managed named pipelines. For `HttpClient`, prefer `microsoft-extensions-http-resilience.md`, which applies standardized HTTP-aware strategies. `microsoft-extensions-resilience.md` enriches Polly telemetry. A call path must have a single retry owner across these integrations. `mailkit.md` needs outbox semantics before any send retry.

Use the [resilience selection guidance](../package-guidance/package-selection.md#resilience-and-retry-ownership), [resilient typed-HTTP-client recipe](../recipes/resilient-typed-httpclient.md), and [`Polly` supply-chain entry](../package-guidance/supply-chain.md#polly).

## Security, performance, AOT, trimming, and operations

Retries amplify work and may duplicate side effects; never include secrets, payloads, or unbounded identifiers in callbacks or metric tags. Reject excess work promptly or use a deliberately bounded queue. A breaker is per pipeline instance, so reuse pipelines to preserve meaningful state. Log final failure at the application boundary and record retry/breaker/rate-limit events as structured telemetry to avoid log storms.

When a DI-managed pipeline constructs a custom rate limiter or another disposable resource, use the `AddResiliencePipelineContext<TKey>` registration overload and call `context.OnPipelineDisposed(...)`; see `polly-extensions.md`. This is especially important for dynamic reload, where old pipelines are discarded. Validate the exact strategy set in trimmed/NativeAOT publishing and exercise failure paths in the published artifact.

Operational signals should cover execution attempts, retry events/delay, timeout events, breaker state/rejections, limiter rejections/queue delay, and final dependency outcome with stable pipeline identity. Exclude payloads, secrets, full URLs, user/tenant IDs, and high-cardinality exception text.

### Troubleshooting

| Symptom | Likely cause | Diagnostic | Correction | Retry? |
| --- | --- | --- | --- | --- |
| Attempt count exceeds contract | Nested pipelines or broad retry predicate | Trace one logical operation and enumerate owners | Keep one retry owner and narrow `ShouldHandle` | Not until corrected |
| Breaker rejects unexpectedly | Shared/incorrect pipeline key or thresholds | Inspect pipeline identity, throughput, sampled faults, and state | Separate contracts or recalibrate using load tests | Do not retry the same open circuit |
| Timeouts ignore caller expectations | Token not propagated or budgets overlap | Identify cancellation source and strategy order | Pass caller token and establish total/attempt budgets | Only if safe and budget remains |
| Limiter memory/latency grows | Queue is too large or unbounded | Inspect permit, queue depth, rejection, and wait duration | Use immediate/tightly bounded rejection and shed load | Only after capacity becomes available |

## Avoid

Do not retry validation, authorization, permanent business failures, or caller cancellation. Do not use infinite retries, `Handle<Exception>()` as a default, overlapping uncoordinated timeouts, unbounded limiter queues, per-call pipeline construction, or blind retries around non-idempotent writes. Do not stack a Polly retry around an HTTP resilience handler.

## Verification checklist

- [ ] Confirm the centrally managed dependency resolves `Polly` `8.7.0`.
- [ ] Test handled and unhandled faults, caller cancellation, attempt timeout, and total-budget expiry.
- [ ] Assert the exact maximum dependency calls for one logical operation.
- [ ] Exercise circuit open, rejected execution, half-open probing, and recovery under controlled faults.
- [ ] Saturate the rate limiter and verify immediate or bounded rejection without hidden queue growth.
- [ ] Verify the pipeline is reused and telemetry contains stable dependency/pipeline identity without secrets.

## Sources

- [NuGet: Polly 8.7.0](https://www.nuget.org/packages/Polly/8.7.0) (Accessed 2026-07-27)
- [Polly getting started](https://www.pollydocs.org/getting-started.html) (Accessed 2026-07-27)
- [Polly resilience pipelines](https://www.pollydocs.org/pipelines/index.html) (Accessed 2026-07-27)
- [Polly retry strategy](https://www.pollydocs.org/strategies/retry.html) (Accessed 2026-07-27)
- [Polly circuit-breaker strategy](https://www.pollydocs.org/strategies/circuit-breaker.html) (Accessed 2026-07-27)
- [Polly rate-limiter strategy](https://www.pollydocs.org/strategies/rate-limiter.html) (Accessed 2026-07-27)
