# OpenTelemetry.Instrumentation.Runtime

> **Owner:** `IX`
> **Last reviewed:** `2026-07-27`
> **Review trigger:** Review when the instrumentation version, target .NET runtime, built-in `System.Runtime` meter, runtime metric conventions, or export policy changes.

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

### Configuration and signal reference

This package has no endpoint or credential settings. Its material configuration is whether the meter is enabled, which metric streams/views are exported, and the metric reader’s interval/temporality.

| Configuration/signal | Purpose and default behavior | Production guidance | Reload, sensitivity, and failure behavior |
| --- | --- | --- | --- |
| `AddRuntimeInstrumentation()` | Enables runtime metrics; on .NET 9+ it registers the built-in `System.Runtime` meter. | Register once on the hosted metric pipeline. | Pipeline shape is fixed at provider construction; restart to apply. Duplicate meters/providers can duplicate export. |
| Metric views | Drop instruments or change aggregation/tag selection. | Retain only actionable metrics and preserve units/types expected by dashboards. | Configure before provider build. An incompatible view can suppress a stream or change its meaning. |
| Reader export interval/timeout | Determines observation/export cadence and alert delay. | Match SLO detection needs, backend cost, and collector capacity. | Fixed with the reader. Short intervals add collection/export pressure; long intervals delay detection. |
| Allocation and GC collection/pause metrics | Expose allocation pressure, heap/fragmentation, generation collections, and pause behavior; some values appear only after GC. | Correlate with throughput, latency, container memory, and profiles. | Operationally sensitive, not secret. Missing pre-GC values may be expected. |
| Thread-pool queue/thread metrics | Expose queued work and worker availability. | Correlate sustained queueing with CPU throttling, blocking, dependency latency, and request rate. | Do not alert on an isolated sample; runtime/target-framework availability varies. |
| Exceptions/JIT/assembly metrics | Expose runtime exception, compilation, and load behavior where the target runtime supports them. | Use trends and workload correlation; avoid treating exception count alone as an incident. | Metric names/availability can change with target runtime; dashboards must be version-aware. |

### Troubleshooting

| Symptom | Inspect | Safe action | Retry suitability |
| --- | --- | --- | --- |
| No runtime metrics | `.WithMetrics(...AddRuntimeInstrumentation())`, exporter/reader, resource, target framework, SDK diagnostics | Verify one known `System.Runtime` metric on the deployed runtime and correct pipeline/export registration. | Restart after configuration correction; workload replay is irrelevant. |
| Some GC metrics are absent | Whether a GC has occurred and whether that metric exists for the target runtime | Generate representative load in a non-production test and consult the exact runtime metric list. Do not force full GC in production to populate a dashboard. | Wait for natural observation; not an exporter retry case. |
| Metric names disappear after target-framework/package upgrade | .NET 9+ built-in meter names/attributes versus older package-produced names; dashboard queries and views | Update queries/views as an intentional schema migration and canary both telemetry and alerts. | Not transient. |
| High metric volume/backend cost | Multiple providers, duplicate collection, export interval, resource/added tag cardinality | Keep one provider, remove unconsumed streams with views, lengthen measured cadence, and eliminate unbounded custom resource/tags. | Not retryable; fix configuration. |
| GC/thread-pool alert fires without latency impact | Request rate/latency, CPU/container throttling, dependencies, profiles/traces | Validate the hypothesis with correlated evidence before tuning runtime knobs. | Do not restart/retry solely from one runtime metric. |

### Upgrade and rollback

Upgrade with the aligned OpenTelemetry metric pipeline and the production target framework. Runtime metrics are especially target-runtime-dependent: snapshot names, instrument types, units, attributes, aggregation, and dashboard/alert queries; then load-test allocation, GC, thread-pool saturation, exception, and steady-state overhead. On .NET 9+, explicitly account for delegation to the built-in `System.Runtime` meter. Canary telemetry cost and alert behavior before broad rollout. Roll back package/runtime/dashboard changes as a coordinated unit if metric contracts or overhead regress; a package-only rollback may not restore old metrics when the target runtime itself changed.

## Integration with the catalog

- Configure through [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md) with the [OpenTelemetry](opentelemetry.md) SDK.
- Pair with [ASP.NET Core](opentelemetry.instrumentation.aspnetcore.md) and [HTTP](opentelemetry.instrumentation.http.md) metrics when those workloads apply.
- Send to the collector via [OpenTelemetry.Exporter.OpenTelemetryProtocol](opentelemetry.exporter.opentelemetryprotocol.md).
- See the catalog-wide [OpenTelemetry composition decision](../package-guidance/package-selection.md#opentelemetry-composition), the [OTLP observability recipe](../recipes/opentelemetry-otlp-postgresql.md), and the [runtime instrumentation supply-chain entry](../package-guidance/supply-chain.md#opentelemetry-instrumentation-runtime).

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
- [Runtime instrumentation 1.17.0 source](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/Instrumentation.Runtime-1.17.0/src/OpenTelemetry.Instrumentation.Runtime)
- [Runtime instrumentation 1.17.0 setup and metric availability](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.Runtime-1.17.0/src/OpenTelemetry.Instrumentation.Runtime/README.md)
- [.NET built-in runtime metrics](https://learn.microsoft.com/dotnet/core/diagnostics/built-in-metrics-runtime)
- [OpenTelemetry .NET metrics](https://opentelemetry.io/docs/languages/dotnet/metrics/)
