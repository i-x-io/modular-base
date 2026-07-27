# OpenTelemetry.Instrumentation.Http

> **Owner:** `IX`
> **Last reviewed:** `2026-07-27`
> **Review trigger:** Review when the instrumentation version, .NET HttpClient diagnostics/metrics, HTTP semantic conventions, propagation policy, or target framework changes.

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

### Configuration reference

| Option | Purpose and default behavior | Production guidance | Reload, sensitivity, and failure behavior |
| --- | --- | --- | --- |
| `FilterHttpRequestMessage` | Suppresses trace instrumentation for requests returning `false`; sampler runs first and metrics are unaffected. | Use only for explicit noise/recursion policy, such as a known collector endpoint. | Fixed when the provider is built; restart to apply. Broad filters hide dependency failures. |
| `EnrichWithHttpRequestMessage` / `EnrichWithHttpResponseMessage` / `EnrichWithException` | Adds custom attributes at client-request lifecycle points. | Prefer response enrichment with finite dependency/result categories. | Fixed at construction. Accessing/copying content, headers, or full URIs may disclose secrets and add latency. |
| `RecordException` | Adds exception events when enabled, beyond status/error attributes. | Enable only after classifying exception content and payload cost. | Fixed at construction. Stack/message content can be sensitive and large. |
| `OTEL_DOTNET_EXPERIMENTAL_HTTPCLIENT_DISABLE_URL_QUERY_REDACTION` | When `true`, disables default query-value redaction. | Leave unset/false; do not use it as a debugging shortcut. | Read at setup; restart to apply. Enabling can export tokens and PII. |
| `HttpMetricsEnrichmentContext.AddCallback` / metric views | Adds bounded tags to built-in metrics or controls collection/aggregation. | Register enrichment per request only for a finite dependency taxonomy; use views to drop/change instruments. | Request callbacks run on the HTTP path. Unbounded tags create backend series pressure. |
| Process-wide propagator | Controls outbound context injection. | Keep W3C Trace Context unless an interoperability review requires another format. | Configure before providers/traffic. Propagated baggage is cross-boundary data and must contain no secrets. |

### Operational signals and troubleshooting

The principal outputs are client activities and `http.client.request.duration` (seconds), plus supported .NET built-in `System.Net.Http` metrics. Their duration ends when response headers are read, so body streaming/consumption requires separate application telemetry when operationally important.

| Symptom | Inspect | Safe action | Retry suitability |
| --- | --- | --- | --- |
| No client span | Tracer provider, `.AddHttpClientInstrumentation()`, sampler, filter, and SDK diagnostics | Exercise an included request and correct registration/filter/sampling. | Telemetry absence does not authorize replaying the dependency call. |
| Downstream trace is disconnected | Outbound `traceparent`, process-wide propagator, proxy/header stripping, downstream server instrumentation | Restore compatible propagation at the boundary; do not manually invent trace IDs. | Retry only if the application operation itself is safely retryable, not to repair past correlation. |
| Duplicate spans or excessive attempts | Duplicate/manual instrumentation, auto-instrumentation, and resilience retry attempt count | Keep one HTTP instrumentation owner and expose logical-operation versus attempt telemetry deliberately. | Govern retries through the resilience policy; never add telemetry retries. |
| URI/series cardinality or secret exposure | Query-redaction flag, enrichment callbacks, raw target tags, backend series counts | Restore redaction and bounded dependency categories; follow incident handling for already exported secrets. | Not retryable. |
| Span duration is shorter than user-perceived download | Header-read completion versus response-body consumption | Add a distinct application operation around body processing when needed. | Not retryable. |

### Upgrade and rollback

Upgrade with the aligned OpenTelemetry family and the deployed .NET runtime. Compare the target framework’s native HttpClient activities/metrics and the package’s pinned release behavior; verify propagation, URL redaction, filters/enrichment, exception recording, retry-attempt counts, units, status/error attributes, and response-header duration semantics. Canary against representative success, timeout, DNS/TLS, 4xx/5xx, redirected, retried, and streaming calls while watching overhead/cardinality. Roll back the aligned packages and related dashboard/configuration changes together if schemas, propagation, or overhead regress; rotate exposed credentials and purge/restrict telemetry separately if an upgrade disclosed query/header data.

## Integration with the catalog

- [OpenTelemetry.Instrumentation.AspNetCore](opentelemetry.instrumentation.aspnetcore.md) observes inbound server traffic.
- [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md) configures the hosted provider.
- [OpenTelemetry.Exporter.OpenTelemetryProtocol](opentelemetry.exporter.opentelemetryprotocol.md) transports the emitted data.
- Do not use this package for Npgsql database calls; use `Npgsql.OpenTelemetry` and `.AddNpgsql()`.
- See the catalog-wide [OpenTelemetry composition decision](../package-guidance/package-selection.md#opentelemetry-composition), the [OTLP observability recipe](../recipes/opentelemetry-otlp-postgresql.md), and the [HTTP instrumentation supply-chain entry](../package-guidance/supply-chain.md#opentelemetry-instrumentation-http).

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
- [HTTP instrumentation 1.17.0 source](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/Instrumentation.Http-1.17.0/src/OpenTelemetry.Instrumentation.Http)
- [HTTP instrumentation 1.17.0 setup, filters, enrichment, and metrics](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.Http-1.17.0/src/OpenTelemetry.Instrumentation.Http/README.md)
- [Using instrumentation libraries with OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/libraries/)
- [OpenTelemetry context propagation](https://opentelemetry.io/docs/concepts/context-propagation/)
