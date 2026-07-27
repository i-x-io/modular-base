# Npgsql.OpenTelemetry

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `Npgsql.OpenTelemetry` |
| Pinned version | `10.0.3` |
| Status | Approved catalog dependency |
| Role | OpenTelemetry tracing integration for Npgsql commands and connection activity |

## Decision and scope

Use this package to subscribe an OpenTelemetry tracer provider to Npgsql activities. It instruments database work; service identity, sampling, exporters, and retention remain application observability decisions.

## Recommended registration and use

```csharp
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("catalog-api"))
    .AddNpgsql()
    .AddConsoleExporter()
    .Build();
```

## Enterprise implementation guidance

Use the same tracer provider for the service’s application and HTTP instrumentation. Replace the console exporter with the deployment’s approved exporter. Npgsql tracing was introduced in Npgsql 9 and is documented as experimental, so validate emitted detail and dashboard queries after driver upgrades.

## Integration with the catalog

Use [Npgsql](npgsql.md) for data-source configuration and the catalog’s `OpenTelemetry` and `OpenTelemetry.Extensions.Hosting` packages for provider lifecycle and application-wide instrumentation.

## Security, performance, AOT, trimming, and operations

`NpgsqlDataSourceBuilder.ConfigureTracing` can filter command spans, customize names, add tags, and disable time-to-first-read or physical-connection-open spans.

```csharp
dataSourceBuilder.ConfigureTracing(options => options
    .ConfigureCommandFilter(command =>
        !command.CommandText.StartsWith("COMMIT", StringComparison.OrdinalIgnoreCase)));
```

Do not set span names to raw SQL in production unless SQL content is classified safe: SQL can contain sensitive literals and creates high-cardinality telemetry. Prefer stable operation names, approved tags, and sampling appropriate to database volume.

## Avoid

- Do not rely on the console exporter beyond local diagnosis.
- Do not add sensitive values or raw unclassified SQL to span names or tags.
- Do not assume experimental trace detail is a stable external contract.

## Verification checklist

- [ ] An Npgsql span is visible under an application request trace.
- [ ] Production exporter, sampling, service name, and resource attributes are configured.
- [ ] Span names and tags pass the data-classification and cardinality review.

## Sources

- [Npgsql tracing with OpenTelemetry](https://www.npgsql.org/doc/diagnostics/tracing.html)
- [Npgsql.OpenTelemetry package on NuGet](https://www.nuget.org/packages/Npgsql.OpenTelemetry/10.0.3)

Accessed 2026-07-27.
