# OpenTelemetry.Instrumentation.AspNetCore

> **Owner:** `IX`
> **Last reviewed:** `2026-07-27`
> **Review trigger:** Review when the instrumentation version, ASP.NET Core runtime metrics/diagnostics, HTTP semantic conventions, or target framework changes.

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

### Configuration reference

| Option | Purpose and default behavior | Production guidance | Reload, sensitivity, and failure behavior |
| --- | --- | --- | --- |
| `Filter` | Suppresses trace instrumentation for requests returning `false`; sampling occurs first and metrics are unaffected. | Exclude only documented low-value routes and preserve independent health evidence. | Fixed when the provider is built; restart to apply. Exceptions or overly broad logic create observability gaps. |
| `RecordException` | Adds an exception event in addition to error status when enabled. | Enable only after classifying exception message/stack content and payload cost. | Fixed at provider construction. Exception data can contain secrets/PII and increase export volume. |
| `EnrichWithHttpRequest` / `EnrichWithHttpResponse` / `EnrichWithException` | Adds custom span attributes at lifecycle points. | Prefer response enrichment and finite, application-owned values. | Fixed at provider construction. Enricher exceptions/work add request overhead; never copy arbitrary headers, bodies, or route values. |
| `OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_DISABLE_URL_QUERY_REDACTION` | When `true`, disables default query-value redaction. | Leave unset/false. Any exception requires security approval and compensating redaction. | Read during instrumentation setup; restart to apply. Enabling can disclose credentials and personal data. |
| Metric views / `IHttpMetricsTagsFeature` | Selects/aggregates built-in ASP.NET Core metrics and can add request-duration tags. | Use views to drop unwanted instruments and enrichment only for bounded values. | Configure at startup. High-cardinality tags cause backend cost/saturation rather than an application error. |

### Operational signals and troubleshooting

The primary outputs are server activities and `http.server.request.duration` (seconds), plus the .NET runtime’s additional built-in ASP.NET Core meters on supported target frameworks.

| Symptom | Inspect | Safe action | Retry suitability |
| --- | --- | --- | --- |
| No inbound span | Tracing provider, `.AddAspNetCoreInstrumentation()`, sampler, `Filter`, and SDK diagnostics | Emit a request to an included route and correct registration/filter/sampling. | Not a transient request failure; do not replay unsafe requests for telemetry. |
| Metrics exist but traces do not (or vice versa) | Separate tracing/metrics registrations; remember trace `Filter` does not filter metrics | Configure each intended signal and align dashboard scope with exclusions. | Not retryable. |
| Duplicate server spans | Manual middleware/activity, duplicate SDK registration, or agent auto-instrumentation plus library instrumentation | Choose one automatic instrumentation owner; retain manual spans only for distinct domain work. | Not retryable. |
| Route/series explosion or sensitive URL data | Exported `http.route`/URL attributes, custom enrichment, query-redaction override, backend series counts | Restore redaction, use route templates and finite tags, delete/restrict exposed backend data per incident policy. | Not retryable; treat disclosure as an incident. |
| Trace duration differs from application work | Span/metric lifecycle, response timing, streaming/body work, middleware ordering | Add an application span/metric for the distinct full workflow rather than redefining HTTP telemetry. | Not retryable. |

### Upgrade and rollback

Upgrade with the aligned OpenTelemetry family and the deployed ASP.NET Core target framework. Before rollout, compare the pinned instrumentation README, HTTP semantic conventions, and the target runtime’s built-in meters; snapshot span/metric names, units, status/error attributes, route cardinality, filter behavior, and exception events. Update dashboards/SLO queries before canarying representative success, 4xx, 5xx, exception, streaming, and excluded-route requests. Roll back the aligned packages and dashboard/configuration changes together if schemas, cardinality, or request overhead regress. A package rollback does not remove already exported sensitive/high-cardinality data.

## Integration with the catalog

- Requires hosted SDK registration from [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md) and the SDK [OpenTelemetry](opentelemetry.md).
- Pair with [OpenTelemetry.Instrumentation.Http](opentelemetry.instrumentation.http.md) for outgoing dependencies; they represent opposite sides of different requests and should not be considered duplicates.
- Use [OpenTelemetry.Exporter.OpenTelemetryProtocol](opentelemetry.exporter.opentelemetryprotocol.md) to deliver data to a collector.
- See the catalog-wide [OpenTelemetry composition decision](../package-guidance/package-selection.md#opentelemetry-composition), the [OTLP observability recipe](../recipes/opentelemetry-otlp-postgresql.md), and the [ASP.NET Core instrumentation supply-chain entry](../package-guidance/supply-chain.md#opentelemetry-instrumentation-aspnetcore).

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
- [ASP.NET Core instrumentation 1.17.0 source](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/Instrumentation.AspNetCore-1.17.0/src/OpenTelemetry.Instrumentation.AspNetCore)
- [ASP.NET Core instrumentation 1.17.0 setup and options](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.AspNetCore-1.17.0/src/OpenTelemetry.Instrumentation.AspNetCore/README.md)
- [Using instrumentation libraries with OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/libraries/)
- [Manual instrumentation for OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/instrumentation/)
