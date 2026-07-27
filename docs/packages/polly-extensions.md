# Polly.Extensions

## Catalog entry

`Polly.Extensions` **8.7.0** — companion catalog package; `IServiceCollection` registration and lookup integration for Polly resilience pipelines.

## Decision and scope

Use when a service needs a centrally configured, named pipeline through dependency injection. Keep the key and retry ownership part of the application dependency contract. This package manages pipelines; it does not make a repeated operation safe.

## Recommended registration and use

With central package management, reference the companion package without a project-level version:

```xml
<ItemGroup>
  <PackageReference Include="Polly.Extensions" />
</ItemGroup>
```

Register each pipeline once at the composition root. Do not call `Build()` inside `AddResiliencePipeline`; the provider constructs and caches the pipeline:

```csharp
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Retry;

internal static class PipelineKeys
{
    internal const string Inventory = "inventory";
}

builder.Services.AddResiliencePipeline(PipelineKeys.Inventory, pipeline =>
{
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<IOException>(),
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<IOException>(),
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 20,
            BreakDuration = TimeSpan.FromSeconds(15)
        })
        .AddTimeout(TimeSpan.FromSeconds(2));
});

public sealed class InventoryGateway(
    ResiliencePipelineProvider<string> pipelines)
{
    public ValueTask RefreshAsync(CancellationToken cancellationToken)
    {
        ResiliencePipeline pipeline =
            pipelines.GetPipeline(PipelineKeys.Inventory);

        return pipeline.ExecuteAsync(
            static async token => await RefreshInventoryAsync(token),
            cancellationToken);
    }

    private static ValueTask RefreshInventoryAsync(
        CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;
}
```

Keyed DI is also available with `GetRequiredKeyedService<ResiliencePipeline>(key)`. Prefer provider injection when a component selects among several application-owned keys. Use a generic `ResiliencePipeline<T>` registration when result predicates are part of the strategy contract.

## Enterprise implementation guidance

Centralize keys in an application-owned type and register one pipeline per remote dependency or distinct operational contract. Make strategy ordering visible in composition-root code. Keep retry ownership unique: a gateway using a retrying DI pipeline must not call an HTTP client that already owns retries unless the outer pipeline deliberately contains no retry.

Common workflow:

1. Define the pipeline key beside the dependency abstraction.
2. Register strategies and options once during service composition.
3. Inject `ResiliencePipelineProvider<TKey>` into the gateway, not throughout business logic.
4. Resolve the pipeline by key and pass the caller token into `ExecuteAsync`.
5. Test every key and strategy contract with a deterministic failing fake.

Configuration reload is opt-in and should be used only when the host explicitly binds and validates reloadable strategy options. Treat a policy change as an operational change: validate bounds, observe its effect, and retain a rollback value.

When a registration callback creates a disposable rate limiter or similar resource, call `context.OnPipelineDisposed(...)` in the overload that supplies `ResiliencePipelineBuilderContext`. This prevents discarded resources from surviving a dynamic reload.

## Integration with the catalog

This is the DI companion to `polly.md`. Use `microsoft-extensions-resilience.md` for telemetry enrichment and `microsoft-extensions-http-resilience.md` for `HttpClient` handlers. Do not register a retrying DI pipeline around the HTTP handler by default; choose one retry owner.

## Security, performance, AOT, trimming, and operations

The provider caches pipelines, preserving breaker and limiter state and avoiding per-call construction. Pipeline keys can select materially different failure and capacity behavior, so do not derive them from untrusted input. Keep keys and telemetry labels low-cardinality and free of tenant IDs, URLs, or secrets.

Validate options before activation, bound retries/timeouts/queues, and ensure reload cannot introduce unlimited work. Test the deployed trim/NativeAOT artifact if configuration binding, DI, or strategy construction is reflection-sensitive.

## Avoid

Do not scatter literal keys, register unrelated contracts under one ambiguous key, call `Build()` in the registration callback, construct a provider per request, resolve an untyped pipeline when result predicates are required, or use dynamic keys supplied by users. Do not add an outer retry to a dependency that already retries.

## Verification checklist

- [ ] Confirm the centrally managed package resolves `Polly.Extensions` `8.7.0`.
- [ ] Build the service provider with validation and resolve every registered key.
- [ ] Assert handled/unhandled faults, timeout, breaker behavior, cancellation, and maximum attempt count for each contract.
- [ ] Verify the same key reuses pipeline state rather than creating a circuit per call.
- [ ] Confirm unknown keys fail clearly and cannot be selected from untrusted input.
- [ ] Inspect telemetry for the stable dependency key and verify no duplicate retry layer exists.

## Sources

- [NuGet: Polly.Extensions 8.7.0](https://www.nuget.org/packages/Polly.Extensions/8.7.0) (Accessed 2026-07-27)
- [Polly getting started: dependency injection](https://www.pollydocs.org/getting-started.html#dependency-injection) (Accessed 2026-07-27)
- [Polly advanced dependency injection](https://www.pollydocs.org/advanced/dependency-injection.html) (Accessed 2026-07-27)
- [Polly pipelines and dependency injection](https://github.com/App-vNext/Polly/blob/main/docs/advanced/dependency-injection.md) (Accessed 2026-07-27)
