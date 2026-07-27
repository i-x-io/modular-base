# Microsoft.Extensions.Http.Resilience

## Catalog entry

`Microsoft.Extensions.Http.Resilience` **10.8.0** — direct catalog package; HTTP-specific resilience handlers for `IHttpClientFactory`, built on the Microsoft resilience/Polly integration.

## Decision and scope

Use as the default resilience integration for outbound `HttpClient` dependencies. It provides HTTP-aware handlers; it does not determine whether a particular request is safe to retry. Give one layer ownership of retries: an HTTP client using this handler should not also be wrapped in a Polly retry decorator or application-level retry loop.

## Recommended registration and use

With central package management, add a versionless project reference:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
</ItemGroup>
```

Register a typed or named client at the composition root. The standard handler contains, from outermost to innermost, a rate limiter, total timeout, retry, circuit breaker, and per-attempt timeout. Its retry strategy applies to every HTTP method by default, so explicitly disable retry for unsafe methods unless the dependency offers an idempotency or deduplication contract:

```csharp
builder.Services
    .AddHttpClient<CatalogClient>(client =>
    {
        client.BaseAddress = new Uri("https://catalog.example/");
    })
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.DisableForUnsafeHttpMethods();
        options.Retry.MaxRetryAttempts = 2;
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(8);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
        options.CircuitBreaker.MinimumThroughput = 20;
    });

public sealed class CatalogClient(HttpClient client)
{
    public Task<HttpResponseMessage> GetProductAsync(
        string id,
        CancellationToken cancellationToken) =>
        client.GetAsync($"products/{Uri.EscapeDataString(id)}", cancellationToken);
}
```

The standard rate limiter bounds concurrent executions. When dependency-specific behavior is required, use one `AddResilienceHandler` call and add only the needed strategies there. If defaults were installed with `ConfigureHttpClientDefaults`, call `RemoveAllResilienceHandlers()` before adding the replacement handler.

## Enterprise implementation guidance

Name clients by remote dependency and make the time budget hierarchical: caller deadline > total request timeout > per-attempt timeout, with enough room for the chosen delays. Keep `HttpClient.Timeout` longer than the resilience total timeout, or leave timeout ownership with the resilience handler, so callers see one intentional budget rather than overlapping timers.

Treat a retry as another request to the dependency. Retry only transient results, cap attempts, use jitter, and honor `Retry-After` where the dependency contract allows it. A circuit breaker should shed calls during a sustained fault, while the rate limiter protects local and remote capacity; neither replaces admission control at the application boundary. POST-like operations require an idempotency key or durable deduplication before retry is enabled.

Typical workflow:

1. Assign a typed/named client and one resilience-handler owner per remote dependency.
2. Measure the dependency latency distribution, then set attempt and total budgets.
3. Disable unsafe-method retries; opt individual operations back in only with a replay contract.
4. Load-test retry amplification, circuit transitions, and rate-limit rejection before rollout.
5. Alert on final outcomes and breaker/rate-limit events, not on every retry in isolation.

## Integration with the catalog

This is the HTTP-specific companion to `microsoft-extensions-resilience.md` and is preferred over generic `polly.md` wiring for `HttpClient`. `polly-extensions.md` remains appropriate for non-HTTP dependencies. Do not combine this package with a second retry registered through those packages on the same call path.

## Security, performance, AOT, trimming, and operations

Handlers can multiply traffic during an outage; bound attempts, queued work, and total duration. Request bodies, authorization headers, streams, and signatures may not be safe or possible to replay. Never log credentials or payloads from retry callbacks. Use low-cardinality dependency and pipeline names, export Polly/.NET resilience telemetry through the host, and correlate one logical request across attempts.

Roll out policy changes by dependency, watch request volume and tail latency, and keep a quick configuration rollback. Publish and exercise the configured client in the intended trim/NativeAOT mode.

## Avoid

Do not stack resilience handlers, combine handler retries with caller retries, retry non-replayable or unsafe requests, use an unbounded queue, or share one generic policy across dependencies with different availability and latency requirements. Do not use circuit-breaker state as a readiness signal; an open circuit can be a healthy protective response to a remote outage.

## Verification checklist

- [ ] Confirm the project uses the centrally managed `10.8.0` version and resolves one handler chain per client.
- [ ] Test retryable and non-retryable status codes, transport errors, caller cancellation, attempt timeout, and total-budget expiry.
- [ ] Prove unsafe methods are not retried unless an idempotency/deduplication test covers them.
- [ ] Exercise circuit open, half-open recovery, concurrency rejection, and dependency recovery under controlled faults.
- [ ] Verify the maximum dependency calls per logical operation and ensure no outer retry multiplies that count.
- [ ] Inspect logs and metrics for dependency identity, final outcome, secrets, and bounded cardinality.

## Sources

- [NuGet: Microsoft.Extensions.Http.Resilience 10.8.0](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience/10.8.0) (Accessed 2026-07-27)
- [Microsoft Learn: Build resilient HTTP apps](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience) (Accessed 2026-07-27)
- [.NET Extensions: Microsoft.Extensions.Http.Resilience source and README](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Http.Resilience) (Accessed 2026-07-27)
