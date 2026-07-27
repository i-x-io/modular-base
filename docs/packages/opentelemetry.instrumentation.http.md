# OpenTelemetry.Instrumentation.Http

## Catalog entry

`<PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.17.0" />`

**Role:** instrumentation for outbound `HttpClient` requests and their standard HTTP client telemetry. **Status:** approved central-catalog dependency for services that make HTTP dependencies.

## Decision and scope

This package captures client spans and HTTP metrics for outbound `HttpClient` traffic. It differs from ASP.NET Core instrumentation, which captures inbound server requests. In a service making an HTTP call, the local outbound client span and the remote service’s inbound server span are both expected; that is distributed-trace propagation, not duplication.

## Recommended registration and use

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics.AddHttpClientInstrumentation());
```

Use `IHttpClientFactory`/typed clients for application HTTP clients, and let the instrumentation propagate trace context. Configure request filtering only with an explicit policy—for example, to suppress local collector/exporter calls if they would otherwise create self-observability noise.

## Enterprise implementation guidance

- Name and configure HTTP clients by dependency. Apply timeouts, authentication, retry/circuit-breaker policy, and telemetry policy intentionally rather than emitting opaque generic traffic.
- Preserve W3C trace-context propagation unless an interoperability boundary requires a documented propagator choice.
- Do not record sensitive request/response bodies, secrets, authorization headers, or full query strings. Prefer route/host/method/status attributes that remain bounded.
- Coordinate retry telemetry with resilience policy: each attempt can be a meaningful dependency event, but dashboards must distinguish request-level outcomes from attempt-level activity.

## Integration with the catalog

- [OpenTelemetry.Instrumentation.AspNetCore](opentelemetry.instrumentation.aspnetcore.md) observes inbound server traffic.
- [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md) configures the hosted provider.
- [OpenTelemetry.Exporter.OpenTelemetryProtocol](opentelemetry.exporter.opentelemetryprotocol.md) transports the emitted data.
- Do not use this package for Npgsql database calls; use `Npgsql.OpenTelemetry` and `.AddNpgsql()`.

## Security, performance, AOT, trimming, and operations

Outbound HTTP attributes can expose customer paths, API keys in URLs, and internal topology. Apply redaction close to instrumentation/export before data leaves the process. Exclude exporter traffic only after confirming it prevents recursion/noise without hiding a genuine dependency. Keep dimensions bounded and test the composed application for trim/AOT behavior.

## Avoid

- Do not add a manual span around every `HttpClient.SendAsync` merely to repeat the instrumentation span.
- Do not disable propagation to hide identifiers; redact/limit data instead.
- Do not use HTTP instrumentation as server instrumentation.
- Do not create metric labels from complete URLs, request IDs, or exception text.

## Verification checklist

- [ ] One outbound request creates the expected client span and metrics.
- [ ] A downstream compatible service receives the trace context and joins the same trace.
- [ ] Sensitive headers, query strings, and bodies are absent from exported telemetry.
- [ ] Retry metrics/spans are understood at both attempt and logical-operation level.
- [ ] Exporter/collector calls are intentionally handled to avoid accidental recursion or noise.

## Sources

Accessed 2026-07-27:

- https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Http/1.17.0
- https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Http
- https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Http/README.md
- https://opentelemetry.io/docs/concepts/context-propagation/
