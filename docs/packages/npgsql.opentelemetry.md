# Npgsql.OpenTelemetry

> **Owner:** `IX` · **Last reviewed:** `2026-07-27` · **Review trigger:** Npgsql/OpenTelemetry version, database semantic-convention, exporter-policy, or target-framework change.

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

- Reference the centrally pinned instrumentation package. The application must also reference its chosen OpenTelemetry hosting and exporter packages:

```xml
<ItemGroup>
  <PackageReference Include="Npgsql.OpenTelemetry" />
</ItemGroup>
```

In a hosted service, let dependency injection own the tracer-provider lifecycle and send telemetry through the deployment's approved exporter:

```csharp
services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("catalog-api"))
    .WithTracing(tracing => tracing
        .AddNpgsql()
        .AddOtlpExporter());
```

For a short-lived local diagnostic program, an explicitly owned provider is also valid:

```csharp
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("catalog-api"))
    .AddNpgsql()
    .AddConsoleExporter()
    .Build();
```

## Enterprise implementation guidance

Use the same tracer provider for the service’s application and HTTP instrumentation so database spans become children of incoming request spans. Replace the console exporter with the deployment’s approved exporter, configure batching and sampling for expected command volume, and flush on graceful shutdown. Npgsql tracing was introduced in Npgsql 9; Npgsql 10 aligned command tracing and metrics with OpenTelemetry semantic conventions, so dashboard queries and alert rules must be reviewed during a 9-to-10 upgrade.

Instrument a representative workflow end to end: incoming HTTP request, application activity, Npgsql command, and exporter delivery. Confirm error status and duration behavior with a deliberately failed command in a non-production environment. Telemetry is diagnostic evidence, not a retry or auditing mechanism.

### Upgrade and rollback

Upgrade `Npgsql.OpenTelemetry` with its matching Npgsql release and compatible OpenTelemetry SDK/exporters. Diff semantic-convention attribute and metric names, then exercise successful, failed, cancelled, and pooled commands and update dashboards/alerts before rollout. Telemetry has no database migration, but collector queries and retention rules are operational state. Roll back the package set and dashboard/query changes together; keep temporary dual-compatible queries during a staged rollout when mixed application versions emit different schemas.

## Integration with the catalog

Use [Npgsql](npgsql.md) for data-source configuration and the catalog’s `OpenTelemetry` and `OpenTelemetry.Extensions.Hosting` packages for provider lifecycle and application-wide instrumentation. See the [OpenTelemetry/PostgreSQL recipe](../recipes/opentelemetry-otlp-postgresql.md), [PostgreSQL data-access selection](../package-guidance/package-selection.md#postgresql-data-access), and the [supply-chain entry](../package-guidance/supply-chain.md#npgsql-opentelemetry).

## Security, performance, AOT, trimming, and operations

`NpgsqlDataSourceBuilder.ConfigureTracing` can filter command spans, customize names, add tags, and disable time-to-first-read or physical-connection-open spans. Apply this configuration before building the same long-lived data source used by the application:

```csharp
dataSourceBuilder.ConfigureTracing(options => options
    .ConfigureCommandFilter(command =>
        !command.CommandText.StartsWith("COMMIT", StringComparison.OrdinalIgnoreCase)));
```

Do not set span names to raw SQL in production unless SQL content is classified safe: SQL can contain sensitive literals and creates high-cardinality telemetry. Prefer stable operation names, approved tags, and sampling appropriate to database volume.

Exporters can add latency, memory pressure, and network traffic. Monitor dropped spans and exporter failures, keep attributes bounded, and never attach parameter values, connection strings, tokens, tenant secrets, or full result data. Treat database names, server addresses, SQL text, and exception messages according to the organization's telemetry data classification and retention policy.

Expected signals include command spans and physical-connection activity plus Npgsql meters for command duration/failures and pool used/idle/waiting connections. If spans are missing, verify the provider is built once, `.AddNpgsql()` is attached to that provider, sampling retains the trace, and exporter delivery succeeds. If cardinality or cost spikes, inspect custom span names/tags and SQL capture before increasing sampling limits; telemetry failures are not a reason to retry database writes.

## Avoid

- Do not rely on the console exporter beyond local diagnosis.
- Do not add sensitive values or raw unclassified SQL to span names or tags.
- Do not assume experimental trace detail is a stable external contract.
- Do not create a tracer provider or exporter for every request.
- Do not use head sampling rules that accidentally discard all error or high-latency evidence without an explicit policy.

## Verification checklist

- [ ] An Npgsql span is visible under an application request trace.
- [ ] Production exporter, sampling, service name, and resource attributes are configured.
- [ ] Span names and tags pass the data-classification and cardinality review.
- [ ] Graceful shutdown flushes telemetry and exporter failures/dropped spans are monitored.
- [ ] Dashboards and alerts use the Npgsql 10 semantic-convention names and tags.

## Sources

- [Npgsql tracing with OpenTelemetry](https://www.npgsql.org/doc/diagnostics/tracing.html)
- [Npgsql OpenTelemetry metrics](https://www.npgsql.org/doc/diagnostics/metrics.html)
- [Npgsql 10 tracing and metrics changes](https://www.npgsql.org/doc/release-notes/10.0.html#tracing-and-metrics-have-been-changed-to-align-with-the-opentelemetry-standard)
- [OpenTelemetry .NET ASP.NET Core setup](https://opentelemetry.io/docs/languages/dotnet/getting-started/)
- [Npgsql.OpenTelemetry package on NuGet](https://www.nuget.org/packages/Npgsql.OpenTelemetry/10.0.3)

Accessed 2026-07-27.
