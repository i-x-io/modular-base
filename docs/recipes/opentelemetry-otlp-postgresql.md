# OpenTelemetry traces and metrics with PostgreSQL and OTLP

## Problem and boundary

This recipe gives an ASP.NET Core service one host-owned OpenTelemetry pipeline for inbound HTTP traces and metrics, outbound HTTP traces and metrics, .NET runtime metrics, application telemetry, Npgsql command traces and metrics, and OTLP export. Instrumentation packages create or subscribe to signals, the OpenTelemetry SDK samples and batches them, `Npgsql.OpenTelemetry` bridges Npgsql diagnostics, and the OTLP exporter sends telemetry to a collector or compatible backend. The collector owns routing, retries beyond the process, enrichment policy, and backend credentials.

Logs are intentionally outside this recipe. Adding trace and metric exporters does not export `ILogger` records; configure log export separately only after redaction, filtering, retention, and cost policy are defined.

## Required packages

Use central package management in the ASP.NET Core host. The following Web SDK
block is a consuming-application example using centrally managed versions:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Npgsql" />
    <PackageReference Include="Npgsql.OpenTelemetry" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
  </ItemGroup>
</Project>
```

The hosting extension owns DI lifecycle and provider disposal. The instrumentation packages enable only the named boundaries; they are not exporters. `Npgsql` remains the database driver, while `Npgsql.OpenTelemetry` supplies its OpenTelemetry builder extensions. The OTLP package supplies signal-specific exporters.

## Define stable application telemetry

Keep source and meter names stable and keep metric dimensions bounded:

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;

internal static class ServiceTelemetry
{
    internal const string Name = "IX.CatalogApi";
    internal static readonly ActivitySource ActivitySource = new(Name);
    internal static readonly Meter Meter = new(Name);
    internal static readonly Counter<long> Searches =
        Meter.CreateCounter<long>("catalog.searches", unit: "{search}");
}
```

Libraries may create `ActivitySource` and `Meter` instances, but the application composition root decides which names the SDK listens to. Do not create SDK providers or exporters inside a reusable library. Instrument names are long-lived telemetry contracts; use attributes such as a small result category, not raw search text, document IDs, tenant IDs, or exception messages.

## Compose one host-owned pipeline

Register the application sources, automatic instrumentation, Npgsql instrumentation, resources, and one exporter per enabled signal:

```csharp
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Catalog")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Catalog is required.");

builder.Services.AddSingleton(_ =>
    new NpgsqlDataSourceBuilder(connectionString).Build());

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: builder.Environment.ApplicationName,
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString()))
    .WithTracing(tracing => tracing
        .AddSource(ServiceTelemetry.Name)
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = context =>
                !context.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(ServiceTelemetry.Name)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddNpgsqlInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();

app.MapGet("/catalog/{id:long}", async (
    long id,
    NpgsqlDataSource dataSource,
    CancellationToken cancellationToken) =>
{
    using var activity = ServiceTelemetry.ActivitySource.StartActivity(
        "catalog.lookup");
    activity?.SetTag("catalog.lookup.kind", "by_id");

    await using var command = dataSource.CreateCommand(
        "SELECT title FROM catalog_items WHERE id = $1");
    command.Parameters.AddWithValue(id);
    var title = await command.ExecuteScalarAsync(cancellationToken);

    var outcome = title is null ? "not_found" : "found";
    ServiceTelemetry.Searches.Add(1,
        new KeyValuePair<string, object?>("catalog.search.outcome", outcome));

    return title is null
        ? Results.NotFound()
        : Results.Ok(new { id, title = (string)title });
});

app.Run();
```

The host creates one trace provider and one metric provider, and the hosting extension disposes them during graceful shutdown. Inbound ASP.NET Core instrumentation establishes the current request activity. The application child activity adds only a stable operation kind, Npgsql emits a database child span, and the counter records one bounded outcome. Parameterized SQL keeps the identifier out of SQL text.

`AddAspNetCoreInstrumentation()` and `AddHttpClientInstrumentation()` are called once per relevant signal because traces and metrics have separate provider builders. `AddNpgsql()` enables Npgsql tracing; `AddNpgsqlInstrumentation()` enables its metrics. Runtime instrumentation is metric-only. Each signal-specific `AddOtlpExporter()` uses the OpenTelemetry environment-variable contract and does not enable logs. Do not combine these calls with cross-cutting `UseOtlpExporter()`; the upstream SDK rejects mixed exporter registration.

Filtering `/health` from traces reduces routine span volume, but the endpoint's ASP.NET Core metrics remain available. Keep readiness and dependency-health design separate from telemetry export health: a collector outage should not make a healthy service unavailable.

## Configure OTLP outside code

Set exporter destination and protocol at deployment time:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=https://otel-collector.internal:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_RESOURCE_ATTRIBUTES=deployment.environment.name=production,service.namespace=ix
```

The generic endpoint applies to both signal-specific exporters. Prefer workload identity, a local collector, or a secret-injected `OTEL_EXPORTER_OTLP_HEADERS` value when authentication is required; do not commit headers or print them. For HTTP/protobuf, verify the endpoint/base-path rules for the selected exporter configuration. Environment variables shown here are deployment values, not a `.env` file to add to source control.

## Failure modes and operations

| Symptom | Likely boundary | Observation and safe response |
| --- | --- | --- |
| No custom spans or metrics | Listener registration | Confirm `AddSource(ServiceTelemetry.Name)` and `AddMeter(ServiceTelemetry.Name)` exactly match the static names and that sampling permits the trace. |
| HTTP spans exist but database spans do not | Npgsql instrumentation | Confirm the host references `Npgsql.OpenTelemetry`, calls `AddNpgsql()`, and uses a supported Npgsql version. |
| Runtime metrics absent | Metric pipeline | Confirm `AddRuntimeInstrumentation()` is on `WithMetrics`, the metric reader is active, and the backend query uses current instrument names. |
| Export errors or missing batches | Endpoint/auth/network | Inspect OpenTelemetry self-diagnostics and collector receiver logs; verify protocol, TLS trust, endpoint path, headers, and egress. Do not fail readiness solely because export is unavailable. |
| High memory/CPU or backend cost | Volume/cardinality | Inspect sampling, batch queue pressure, metric series count, endpoint traffic, and custom attributes. Remove unbounded attributes before raising capacity. |
| Duplicate spans or metrics | Multiple providers/instrumentation | Search startup for duplicate `AddOpenTelemetry`, exporter, or instrumentation registration and remove the extra owner. |

Observe exporter failures, dropped spans/metric points, batch queue pressure, export duration, collector receiver rejections, sampling rate, service cardinality, and process shutdown time. The SDK's in-process queue is not durable storage; a prolonged collector outage can lose telemetry. Keep exporters asynchronous/batched, send to a nearby collector, and size queues from measured traffic without allowing telemetry to exhaust application memory.

Do not record connection strings, database parameters, request/response bodies, authorization headers, cookies, complete URLs/query strings, raw SQL values, user identifiers, tenant identifiers, or exception messages as metric dimensions. Review the actual semantic-convention attributes emitted by pinned instrumentation and backend transformations before production rollout.

## Verification checklist

Authoring evidence:

- [x] The Web SDK sample compiled in a temporary `net10.0` project with the catalog's pinned OpenTelemetry and Npgsql package graph.
- [x] The application was not connected to PostgreSQL or an OTLP collector; database telemetry and export delivery were not integration-tested during authoring.

Consuming-application checks:

- [ ] Start the service with a disposable PostgreSQL database and test collector; observe one inbound span, application child span, Npgsql child span, HTTP/runtime metrics, and the custom counter.
- [ ] Verify service name/version/environment resource attributes in the backend and define deployment-instance identity at the platform layer.
- [ ] Exercise exporter unavailability, invalid TLS/authentication, collector throttling, queue saturation, and graceful shutdown without changing service readiness.
- [ ] Confirm sampling and cardinality budgets with representative traffic and verify that sensitive values are absent from spans, metrics, collector logs, and backend indexes.
- [ ] Confirm `/health` trace filtering behaves as intended while health metrics and application availability remain observable.
- [ ] Ensure exactly one provider/exporter owner per signal and document collector-side retry, buffering, routing, and retention.

## Related guides

- [OpenTelemetry](../packages/opentelemetry.md)
- [OpenTelemetry.Extensions.Hosting](../packages/opentelemetry.extensions.hosting.md)
- [OpenTelemetry.Exporter.OpenTelemetryProtocol](../packages/opentelemetry.exporter.opentelemetryprotocol.md)
- [OpenTelemetry.Instrumentation.AspNetCore](../packages/opentelemetry.instrumentation.aspnetcore.md)
- [OpenTelemetry.Instrumentation.Http](../packages/opentelemetry.instrumentation.http.md)
- [OpenTelemetry.Instrumentation.Runtime](../packages/opentelemetry.instrumentation.runtime.md)
- [Npgsql.OpenTelemetry](../packages/npgsql.opentelemetry.md)
- [OpenTelemetry package composition](../package-guidance/package-selection.md#opentelemetry-composition)

## Primary sources

Accessed 2026-07-27.

- [OpenTelemetry .NET ASP.NET Core trace setup](https://opentelemetry.io/docs/languages/dotnet/traces/getting-started-aspnetcore/)
- [OpenTelemetry .NET exporters](https://opentelemetry.io/docs/languages/dotnet/exporters/)
- [OpenTelemetry OTLP exporter 1.17.0 README](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.17.0/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md)
- [OpenTelemetry .NET resources](https://opentelemetry.io/docs/languages/dotnet/resources/)
- [Npgsql tracing diagnostics](https://www.npgsql.org/doc/diagnostics/tracing.html)
- [Npgsql metrics diagnostics](https://www.npgsql.org/doc/diagnostics/metrics.html)
- [OpenTelemetry.Exporter.OpenTelemetryProtocol 1.17.0 on NuGet](https://www.nuget.org/packages/OpenTelemetry.Exporter.OpenTelemetryProtocol/1.17.0)
- [Npgsql.OpenTelemetry 10.0.3 on NuGet](https://www.nuget.org/packages/Npgsql.OpenTelemetry/10.0.3)
