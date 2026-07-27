# OpenTelemetry.Instrumentation.Runtime

## Catalog entry

`<PackageVersion Include="OpenTelemetry.Instrumentation.Runtime" Version="1.17.0" />`

**Role:** .NET runtime metrics instrumentation. **Status:** approved central-catalog dependency for services where runtime resource/GC/thread-pool telemetry supports operational decisions.

## Decision and scope

Use this package for runtime metrics, not trace spans. It exposes runtime health signals such as garbage collection, allocation, thread-pool, exception, and process/runtime metrics according to the package’s supported set. It does not replace application business metrics, request instrumentation, host health checks, or profiling.

## Recommended registration and use

With central package management, add a versionless application reference:

```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
</ItemGroup>
```

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddRuntimeInstrumentation());
```

Export the metrics through the same governed pipeline as application metrics. Establish a dashboard and alert policy before enabling broad runtime signal collection so teams know which measurements drive action.

On .NET 9 and later, the package registers the runtime’s built-in `System.Runtime` meter rather than recreating those measurements. Available metric names and attributes therefore depend on the target runtime as well as the package version. A useful investigation workflow is:

1. Confirm the metric exists for the deployed .NET version and has been observed after startup (some GC measurements require a collection first).
2. Correlate allocation/heap/GC pause signals with request throughput and latency.
3. Correlate thread-pool queueing and worker counts with CPU/container throttling.
4. Use a profiler, trace, dump, or load test to validate the hypothesis before changing GC or thread-pool settings.

## Enterprise implementation guidance

- Use runtime metrics with request rate/latency, container limits, and infrastructure metrics to diagnose saturation. A single GC or thread-pool number is not a root-cause diagnosis.
- Retain dimensions supplied by the instrumentation only when they are bounded and useful; do not add per-process, per-request, or per-tenant metric labels.
- Set alert thresholds from observed baselines and service objectives, not generic absolute values copied across workloads.
- Use SDK views to drop metrics that have no operational consumer or to set intentional histogram aggregation; keep the export interval aligned with alert latency needs and backend cost.
- Version dashboards and alert rules alongside service operational ownership.

## Integration with the catalog

- Configure through [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md) with the [OpenTelemetry](opentelemetry.md) SDK.
- Pair with [ASP.NET Core](opentelemetry.instrumentation.aspnetcore.md) and [HTTP](opentelemetry.instrumentation.http.md) metrics when those workloads apply.
- Send to the collector via [OpenTelemetry.Exporter.OpenTelemetryProtocol](opentelemetry.exporter.opentelemetryprotocol.md).

## Security, performance, AOT, trimming, and operations

Runtime metrics reveal process behavior and can expose deployment topology through resource attributes; protect access as operationally sensitive data. Metric collection adds overhead, so validate cardinality, scrape/export frequency, and application impact under realistic load. Native-AOT and trimming compatibility is composition-specific; test the production publish profile and package version.

## Avoid

- Do not add runtime instrumentation to tracing; it is configured on `WithMetrics`.
- Do not use GC counters as a substitute for heap dumps, profiles, or a memory-leak investigation.
- Do not make per-instance ephemeral identifiers metric dimensions.
- Do not enable metrics without retention, dashboard, and alert ownership.
- Do not assume a missing GC metric is an instrumentation failure before the process has performed a collection or before checking runtime-version availability.

## Verification checklist

- [ ] Runtime metrics arrive with the correct stable service resource attributes.
- [ ] The dashboard correlates runtime signals with workload and infrastructure metrics.
- [ ] Labels are bounded and the backend shows no unexpected series growth.
- [ ] Load testing quantified collection/export overhead.
- [ ] Alert thresholds and escalation ownership are documented.
- [ ] Metric availability and names were verified on the production target framework/runtime, not inferred from a different .NET version.

## Sources

Accessed 2026-07-27:

- [OpenTelemetry runtime instrumentation 1.17.0 on NuGet](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Runtime/1.17.0)
- [Runtime instrumentation source](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Runtime)
- [Runtime instrumentation setup and metric availability](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Runtime/README.md)
- [.NET built-in runtime metrics](https://learn.microsoft.com/dotnet/core/diagnostics/built-in-metrics-runtime)
- [OpenTelemetry .NET metrics](https://opentelemetry.io/docs/languages/dotnet/metrics/)
