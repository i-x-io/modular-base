# OpenTelemetry.Instrumentation.AspNetCore

## Catalog entry

`<PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.17.0" />`

**Role:** ASP.NET Core server instrumentation for inbound HTTP telemetry. **Status:** approved central-catalog dependency for hosted ASP.NET Core services; it is not required by worker-only hosts.

## Decision and scope

Enable this package once to produce server spans and ASP.NET Core HTTP metrics for inbound requests. It is complementary to the HTTP instrumentation package, which captures outgoing `HttpClient` dependencies. The instrumentation observes ASP.NET Core’s diagnostics; it does not replace application spans for domain operations.

## Recommended registration and use

With central package management, add a versionless application reference:

```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
</ItemGroup>
```

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());
```

Configure it at application startup through [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md), before the host starts. Use framework route templates rather than raw request paths in telemetry; avoid collecting query strings or request/response bodies.

Filtering is a trace-instrumentation workflow, not a metrics filter or sampler. A common policy suppresses a dedicated high-volume liveness route while retaining failure evidence through health-check logs/metrics or a separately observed readiness route; document the resulting observability gap.

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation(options =>
    {
        options.Filter = context =>
            !context.Request.Path.StartsWithSegments("/health/live");
        options.EnrichWithHttpResponse = (activity, response) =>
            activity.SetTag("app.endpoint.group", response.StatusCode >= 500
                ? "server-error"
                : "normal");
    }));
```

Keep enrichment values to an approved finite set. The trace filter runs after sampling has been invoked, so use SDK sampling for trace-volume policy rather than treating `Filter` as a replacement sampler.

## Enterprise implementation guidance

- Ensure reverse-proxy forwarding and trusted-proxy configuration are correct before relying on client/network attributes. Do not trust arbitrary forwarded headers from the public internet.
- Filter health, metrics, readiness, liveness, static-content, and other high-volume endpoints deliberately when their telemetry is not useful. Document exclusions because they affect SLO denominator calculations.
- Use route-level attributes and application-owned low-cardinality tags. Redact user-controlled route values and headers.
- Unhandled exceptions already set span status to error. Enable `RecordException` only when the additional exception event is required and its message/stack data has passed the telemetry data review.
- Correlate incoming server spans to application logs through the standard logging pipeline; do not hand-copy trace IDs into every message.

## Integration with the catalog

- Requires hosted SDK registration from [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md) and the SDK [OpenTelemetry](opentelemetry.md).
- Pair with [OpenTelemetry.Instrumentation.Http](opentelemetry.instrumentation.http.md) for outgoing dependencies; they represent opposite sides of different requests and should not be considered duplicates.
- Use [OpenTelemetry.Exporter.OpenTelemetryProtocol](opentelemetry.exporter.opentelemetryprotocol.md) to deliver data to a collector.

## Security, performance, AOT, trimming, and operations

Incoming URLs and headers are attacker-controlled. Never enable capture of sensitive headers/body content without explicit redaction and a data-classification review. Endpoint filters must be measured because excluding traffic can distort latency/error-rate dashboards. Test the actual instrumentation version under trimming/AOT; maintain explicit registration and avoid configuration based on reflection/discovery.

## Avoid

- Do not register it twice through two different extensions or auto-instrumentation plus manual SDK setup.
- Do not tag spans with raw URLs, query strings, authorization headers, session cookies, user IDs, or request bodies.
- Do not treat it as outgoing dependency instrumentation; use the HTTP package for that.
- Do not filter failures just because the endpoint is noisy.
- Do not use `EnrichWithHttpRequest`/`EnrichWithHttpResponse` to copy arbitrary headers or route values into spans.

## Verification checklist

- [ ] A representative inbound request creates one server span and expected HTTP metrics.
- [ ] Health/static endpoints are intentionally included or excluded and dashboards account for that choice.
- [ ] Routes are low-cardinality templates, not raw parameterized paths.
- [ ] Reverse-proxy and forwarded-header trust settings are reviewed.
- [ ] No duplicate server spans appear in a trace.
- [ ] Enrichment and exception-event settings have bounded attributes and an explicit data-classification decision.

## Sources

Accessed 2026-07-27:

- [OpenTelemetry ASP.NET Core instrumentation 1.17.0 on NuGet](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNetCore/1.17.0)
- [ASP.NET Core instrumentation source](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore)
- [ASP.NET Core instrumentation setup and options](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/README.md)
- [Using instrumentation libraries with OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/libraries/)
- [Manual instrumentation for OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/instrumentation/)
