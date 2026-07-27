# Microsoft.Extensions.Http

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | `IHttpClientFactory`, named/typed client registration, and handler pooling | Approved outbound HTTP integration |

## Decision and scope

Use this package for DI-integrated outbound HTTP clients. It provides factory-managed handler pooling and named/typed client configuration; it does not define business API contracts or a resilience policy by itself.

## Recommended registration and use

With Central Package Management, add a versionless project reference:

```xml
<PackageReference Include="Microsoft.Extensions.Http" />
```

Use a typed client for a focused external API adapter and keep request/response mapping at that boundary:

```csharp
using System.Net.Http.Json;

builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Catalog:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Orders/1.0");
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

## Enterprise implementation guidance

Give every external service a stable client name, ownership, endpoint configuration, authentication mechanism, concurrency limit, resilience budget, and telemetry. A common workflow is:

1. Validate an absolute HTTPS base address at startup using the options layer.
2. Register one named or typed client and attach only service-specific delegating handlers.
3. Propagate the caller's cancellation token and set an end-to-end deadline.
4. Apply resilience in `Microsoft.Extensions.Http.Resilience`; retry only methods and failures that the remote API contract declares safe.
5. Map non-success responses into the adapter's domain-facing result or exception model and record sanitized latency/outcome telemetry.

Authentication handlers may attach short-lived credentials per request, but must not store request-specific state on pooled handlers. Use idempotency keys where the remote contract supports them; a retry policy alone cannot make a write idempotent.

## Integration with the catalog

Register through [DependencyInjection](microsoft-extensions-dependencyinjection.md). Bind client settings using [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md) and validate them with [Options](microsoft-extensions-options.md). Use [Logging.Abstractions](microsoft-extensions-logging-abstractions.md) and health checks for dependency telemetry, not per-request health probing.

## Security, performance, AOT, trimming, and operations

Never accept unvalidated base addresses or forward sensitive headers across trust boundaries. Handler pooling means handlers can outlive a request; do not store request-specific state on them. Avoid factory-managed clients when the application requires cookies: pooled `CookieContainer` instances can share cookies between unrelated consumers, and handler recycling discards stored cookies. Configure handler/connection lifetime deliberately for DNS changes, and request a new factory client instead of retaining one beyond its handler lifetime. Bound concurrency (`MaxConnectionsPerServer` where appropriate) to avoid bursts of HTTP/1.1 connections. Avoid logging credentials or sensitive response bodies. The registration model is static; reflection-based client discovery must be tested for trim/AOT.

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
