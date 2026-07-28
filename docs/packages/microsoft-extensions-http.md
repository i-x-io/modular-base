# Microsoft.Extensions.Http

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | `IHttpClientFactory`, named/typed client registration, and handler pooling | Direct; approved outbound HTTP integration |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, HttpClientFactory, handler lifetime, DNS, or networking guidance change |

## Decision and scope

Use this package for DI-integrated outbound HTTP clients. It provides factory-managed handler pooling and named/typed client configuration; it does not define business API contracts or a resilience policy by itself.

## Recommended registration and use

With Central Package Management, `PackageReference` entries omit `Version` because `Directory.Packages.props` is the package-version authority. `ProjectReference` entries express source-project dependencies; the consuming solution remains responsible for keeping those dependencies within its intended boundaries:

```xml
<PackageReference Include="Microsoft.Extensions.Http" />
```

Use a typed client for a focused external API adapter and keep request/response mapping at that boundary:

```csharp
using System.Net.Http.Json;

var configuredBaseUrl = builder.Configuration["Catalog:BaseUrl"];
if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var catalogBaseAddress) ||
    catalogBaseAddress.Scheme != Uri.UriSchemeHttps ||
    catalogBaseAddress.UserInfo.Length != 0 ||
    !string.Equals(
        catalogBaseAddress.Host,
        "catalog.internal.example",
        StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Catalog:BaseUrl must be the approved HTTPS catalog endpoint.");
}

builder.Services
    .AddHttpClient<CatalogClient>(client =>
    {
        client.BaseAddress = catalogBaseAddress;
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Orders/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false
    });

public sealed class CatalogClient(HttpClient httpClient)
{
    public async Task<Product?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<Product>(
            $"products/{productId}", cancellationToken);
    }
}

public sealed record Product(Guid Id, string Name);
```

Use a named client when several consumers share one external-service configuration. Inject `IHttpClientFactory` into a singleton and call `CreateClient("catalog")` for each operation; factory-created clients are intended to be short-lived and are safe to dispose. Typed clients are transient/short-lived and must not be captured by a singleton. Configure base address, headers, timeout, and handlers in one owned registration.

This package does not add retries, circuit breakers, rate limiting, or hedging. Add those through [Microsoft.Extensions.Http.Resilience](microsoft-extensions-http-resilience.md), and define service-specific policies there. Keep per-attempt timeout and total operation timeout distinct.

### Client configuration reference

| Setting | Purpose | Default behavior | Production guidance | Reload | Sensitivity | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| `BaseAddress` | Defines relative-request origin | Unset | Require an absolute HTTPS URI and validate the trust boundary | Recreate client/configuration | Host/query may be sensitive | Relative requests fail without a base address |
| `HttpClient.Timeout` | Bounds client-side request duration | 100 seconds | Set an end-to-end budget and distinguish caller cancellation | Recreate client | Not sensitive | Throws cancellation-style exception on timeout |
| Handler lifetime | Rotates pooled primary handlers | Factory default | Align with DNS/network behavior; request fresh clients from the factory | Registration-time | Not sensitive | Too long can retain stale DNS; too short churns pools |
| `PooledConnectionLifetime` | Rotates individual pooled connections | Infinite unless configured | Use when DNS rotation must occur within a bounded period | Handler rebuild | Not sensitive | Stale endpoints persist until connections rotate |
| `MaxConnectionsPerServer` | Bounds per-origin HTTP/1.1 concurrency | Handler/platform default | Set from upstream and application concurrency budgets | Handler rebuild | Not sensitive | Low limits queue; no practical bound can amplify bursts |

## Enterprise implementation guidance

Give every external service a stable client name, ownership, endpoint configuration, authentication mechanism, concurrency limit, resilience budget, and telemetry. A common workflow is:

1. Validate an absolute HTTPS base address at startup using the options layer.
2. Register one named or typed client and attach only service-specific delegating handlers.
3. Propagate the caller's cancellation token and set an end-to-end deadline.
4. Apply resilience in `Microsoft.Extensions.Http.Resilience`; retry only methods and failures that the remote API contract declares safe.
5. Map non-success responses into the adapter's domain-facing result or exception model and record sanitized latency/outcome telemetry.

Authentication handlers may attach short-lived credentials per request, but must not store request-specific state on pooled handlers. Use idempotency keys where the remote contract supports them; a retry policy alone cannot make a write idempotent.

### Upgrade and rollback

Upgrade with the target framework and deliberately coordinate `Microsoft.Extensions.Http.Resilience` when it owns resilience. Re-run DNS rotation, handler lifetime, timeout/cancellation, connection-pool, authentication, cookie, and idempotency tests against representative upstream behavior. No data migration is required. Roll back the package and client configuration together; do not retain a newly introduced handler or resilience pipeline with an older factory registration.

## Integration with the catalog

Register through [DependencyInjection](microsoft-extensions-dependencyinjection.md). Bind client settings using [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md) and validate them with [Options](microsoft-extensions-options.md). Use [Logging.Abstractions](microsoft-extensions-logging-abstractions.md) and health checks for dependency telemetry, not per-request health probing.

The [resilience selection guide](../package-guidance/package-selection.md#resilience-and-retry-ownership) defines the single retry owner; the [resilient typed `HttpClient` recipe](../recipes/resilient-typed-httpclient.md) shows the complete composition. See the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-http).

## Security, performance, AOT, trimming, and operations

Never accept unvalidated base addresses or forward sensitive headers across trust boundaries. Handler pooling means handlers can outlive a request; do not store request-specific state on them. Avoid factory-managed clients when the application requires cookies: pooled `CookieContainer` instances can share cookies between unrelated consumers, and handler recycling discards stored cookies. Configure handler/connection lifetime deliberately for DNS changes, and request a new factory client instead of retaining one beyond its handler lifetime. Bound concurrency (`MaxConnectionsPerServer` where appropriate) to avoid bursts of HTTP/1.1 connections. Avoid logging credentials or sensitive response bodies. The registration model is static; reflection-based client discovery must be tested for trim/AOT.

### Operational signals

| Signal | Meaning/action | Privacy/cardinality rule |
| --- | --- | --- |
| Request count, duration, active/queued requests, and connection-pool signals by client/origin | Detects latency, saturation, connection starvation, and concurrency mismatch | Use bounded client/service names; avoid raw URLs and query strings |
| Outcome by status-code class and exception category | Separates remote rejection, throttling, timeout, cancellation, DNS/TLS, and network failure | Do not record authorization headers, cookies, or response bodies |
| Handler/connection rotation and DNS-related failures | Validates lifetime configuration during endpoint changes | Record sanitized host/service name only |
| Resilience attempt/circuit signals | Shows retries, hedges, timeouts, and circuit state when the resilience package is present | Emit from the single resilience owner; never label by request ID or payload |

### Troubleshooting

| Symptom | Likely causes and diagnostics | Safe corrective action | Retry suitability |
| --- | --- | --- | --- |
| Requests use a stale endpoint | Long-lived handler/connection, retained factory client, or DNS/network change; inspect connection lifetime and client ownership | Request clients from the factory as designed and set deliberate handler/connection rotation | Retry on the same stale pool may not help |
| Socket/connection exhaustion | Per-request handlers, unbounded HTTP/1.1 concurrency, slow upstream, or undisposed responses | Use factory pooling, dispose responses/streams, and bound concurrency | More retries worsen exhaustion |
| Timeout or cancellation exception | End-to-end budget expired, caller canceled, pool queued, DNS/connect/TLS stalled, or upstream was slow | Correlate caller token, configured timeout, queue/connection, and upstream latency before tuning | Retry only transient, idempotent operations within the remaining budget |
| Cookies or credentials cross consumers | Pooled handler state or a delegating handler retained request-specific data | Do not use factory-managed cookie containers for isolation; attach credentials per request without mutable handler state | Never retry until isolation is corrected |
| Duplicate outbound attempts | Both application and resilience layers own retries, or write retry lacks idempotency | Select one retry owner and require upstream idempotency support for writes | Disable duplicate ownership; do not blindly retry unsafe methods |

## Avoid

- Do not create a new `HttpClient` handler per request.
- Do not separately register a typed client as a plain transient; that breaks its factory configuration.
- Do not inject a typed client into a singleton service.

## Verification checklist

- [ ] The project has a versionless package reference and restores catalog version `10.0.10`.
- [ ] Client names/types, validated HTTPS endpoints, authentication, timeout, and handler lifetime are tested.
- [ ] No singleton captures a typed or factory-created client beyond its intended lifetime.
- [ ] Resilience is configured only through the resilience integration, is idempotency-aware, and is tested under timeout, DNS, connection, throttling, and 5xx conditions.
- [ ] Concurrency is bounded and logs/traces redact credentials, query secrets, and sensitive payloads.

## Sources

- [NuGet: Microsoft.Extensions.Http 10.0.10](https://www.nuget.org/packages/Microsoft.Extensions.Http/10.0.10) (Accessed 2026-07-27)
- [Use IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) (Accessed 2026-07-27)
- [IHttpClientFactory troubleshooting](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory-troubleshooting) (Accessed 2026-07-27)
- [HttpClient guidelines](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines) (Accessed 2026-07-27)
