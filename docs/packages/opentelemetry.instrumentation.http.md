# OpenTelemetry.Instrumentation.Http

## Catalog entry

`<PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.17.0" />`

**Role:** instrumentation for outbound `HttpClient` requests and their standard HTTP client telemetry. **Status:** approved central-catalog dependency for services that make HTTP dependencies.

## Decision and scope

This package captures client spans and HTTP metrics for outbound `HttpClient` traffic. It differs from ASP.NET Core instrumentation, which captures inbound server requests. In a service making an HTTP call, the local outbound client span and the remote service’s inbound server span are both expected; that is distributed-trace propagation, not duplication.

## Recommended registration and use

With central package management, add a versionless application reference:

```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
</ItemGroup>
```

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics.AddHttpClientInstrumentation());
```

Use `IHttpClientFactory`/typed clients for application HTTP clients, and let the instrumentation propagate trace context. Configure request filtering only with an explicit policy—for example, to suppress local collector/exporter calls if they would otherwise create self-observability noise.

For tracing on modern .NET, filter with `FilterHttpRequestMessage` and enrich only with low-cardinality data derived from an approved dependency map:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddHttpClientInstrumentation(options =>
    {
        options.FilterHttpRequestMessage = request =>
            request.RequestUri?.Host != "otel-collector.internal";
        options.EnrichWithHttpResponseMessage = (activity, response) =>
            activity.SetTag("app.dependency.result", response.IsSuccessStatusCode
                ? "success"
                : "failure");
    }));
```

The filter runs after sampling has been invoked and applies to trace instrumentation, not metric selection. On .NET 9+, `HttpClient` supplies native trace attributes, while this package remains useful for SDK propagation plus filtering/enrichment. On .NET 8+, built-in `HttpClient` metrics are enabled by `AddHttpClientInstrumentation`; use SDK views when a built-in metric must be dropped or its aggregation changed.

## Enterprise implementation guidance

- Name and configure HTTP clients by dependency. Apply timeouts, authentication, retry/circuit-breaker policy, and telemetry policy intentionally rather than emitting opaque generic traffic.
- Preserve W3C trace-context propagation unless an interoperability boundary requires a documented propagator choice.
- Do not record sensitive request/response bodies, secrets, authorization headers, or full query strings. Prefer route/host/method/status attributes that remain bounded.
- Keep the default URL query redaction enabled. Do not set `OTEL_DOTNET_EXPERIMENTAL_HTTPCLIENT_DISABLE_URL_QUERY_REDACTION=true` unless a security review approves the resulting disclosure risk.
- Coordinate retry telemetry with resilience policy: each attempt can be a meaningful dependency event, but dashboards must distinguish request-level outcomes from attempt-level activity.

## Integration with the catalog

- [OpenTelemetry.Instrumentation.AspNetCore](opentelemetry.instrumentation.aspnetcore.md) observes inbound server traffic.
- [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md) configures the hosted provider.
- [OpenTelemetry.Exporter.OpenTelemetryProtocol](opentelemetry.exporter.opentelemetryprotocol.md) transports the emitted data.
- Do not use this package for Npgsql database calls; use `Npgsql.OpenTelemetry` and `.AddNpgsql()`.

## Security, performance, AOT, trimming, and operations

Outbound HTTP attributes can expose customer paths, API keys in URLs, and internal topology. Apply redaction close to instrumentation/export before data leaves the process. Exclude exporter traffic only after confirming it prevents recursion/noise without hiding a genuine dependency. The documented client span/metric duration ends when response headers are read, not after the response body is consumed; use an application span/metric when full body processing time is the operation being measured. Keep dimensions bounded and test the composed application for trim/AOT behavior.

## Avoid

- Do not add a manual span around every `HttpClient.SendAsync` merely to repeat the instrumentation span.
- Do not disable propagation to hide identifiers; redact/limit data instead.
- Do not use HTTP instrumentation as server instrumentation.
- Do not create metric labels from complete URLs, request IDs, or exception text.
- Do not copy exception stack traces into span tags; exception recording can materially increase payload size and disclose internals.

## Verification checklist

- [ ] One outbound request creates the expected client span and metrics.
- [ ] A downstream compatible service receives the trace context and joins the same trace.
- [ ] Sensitive headers, query strings, and bodies are absent from exported telemetry.
- [ ] Retry metrics/spans are understood at both attempt and logical-operation level.
- [ ] Exporter/collector calls are intentionally handled to avoid accidental recursion or noise.
- [ ] Dashboards distinguish time-to-response-headers from full response-body consumption where that difference matters.

## Sources

Accessed 2026-07-27:

- [OpenTelemetry HTTP instrumentation 1.17.0 on NuGet](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Http/1.17.0)
- [HTTP instrumentation source](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Http)
- [HTTP instrumentation setup, filters, enrichment, and metrics](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Http/README.md)
- [Using instrumentation libraries with OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/libraries/)
- [OpenTelemetry context propagation](https://opentelemetry.io/docs/concepts/context-propagation/)
