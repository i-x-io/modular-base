# OpenTelemetry

> **Owner:** `IX`
> **Last reviewed:** `2026-07-27`
> **Review trigger:** Review when the OpenTelemetry SDK version, target framework, signal specification, or provider/processor behavior changes.

## Catalog entry

`<PackageVersion Include="OpenTelemetry" Version="1.17.0" />`

**Role:** the OpenTelemetry .NET SDK package. It supplies SDK builders, providers, processors, samplers, resources, and the in-process pipeline for traces, metrics, and logs. **Status:** approved central-catalog dependency; add it to an application only when that application configures the SDK directly or through the hosting extensions.

## Decision and scope

Use this package for SDK behavior. It is not an exporter, protocol implementation, or instrumentation package. `OpenTelemetry.Api` is the lightweight API surface that libraries use to create `ActivitySource`, `Meter`, and logging-facing telemetry; applications use this SDK package to collect and process that telemetry. The catalog intentionally keeps both packages at the same version.

## Recommended registration and use

With central package management, add a versionless application reference; the catalog supplies `1.17.0`:

```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry" />
</ItemGroup>
```

For ASP.NET Core or Generic Host applications, configure the SDK through `OpenTelemetry.Extensions.Hosting`; see [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md). Register application source and meter names explicitly, and make those names stable public telemetry contracts.

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;

internal static class Telemetry
{
    internal const string Name = "ModularBase.Orders";
    internal static readonly ActivitySource ActivitySource = new(Name);
    internal static readonly Meter Meter = new(Name);
}
```

The host configuration must call `.AddSource(Telemetry.Name)` and `.AddMeter(Telemetry.Name)` for these signals to be collected. Do not create a provider in a library.

For a short-lived, non-hosted process, own and dispose the provider in the process composition root. Disposal is the normal final flush path; do not dispose it while work is still producing telemetry.

```csharp
using OpenTelemetry;
using OpenTelemetry.Trace;

using TracerProvider provider = Sdk.CreateTracerProviderBuilder()
    .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.10)))
    .AddSource(Telemetry.Name)
    .Build();

// Run the process workload while provider remains alive.
```

## Enterprise implementation guidance

- Define `service.name`, `service.version`, and deployment-environment resource attributes once at the application composition root. `service.name` identifies the emitting service; it is not a per-request, tenant, user, pod, or database value.
- Use parent-based ratio sampling for normal production traffic and an intentionally documented policy for error retention. Sampling changes what is available for diagnosis; it is not a data-redaction control.
- Treat processor order as behavior. Use processors for cross-cutting span mutation or export, keep `OnStart`/`OnEnd` work small, and use batch export for network exporters; synchronous/simple export belongs in local diagnostics or narrowly justified tests.
- Keep metric label cardinality bounded. Never use request IDs, user IDs, email addresses, full URLs, SQL text, opaque tokens, or unbounded exception messages as metric dimensions.
- Treat telemetry as sensitive production data. Filter/redact headers, query strings, route values, database statements, and exception data before export, and restrict collector/backend access accordingly.

### SDK configuration reference

| Setting | Purpose and default behavior | Production guidance | Reload, sensitivity, and failure behavior |
| --- | --- | --- | --- |
| `OTEL_SERVICE_NAME` | Sets `service.name`; otherwise resource detection/fallback behavior applies. | Set one stable logical service name per deployable service. | Read while providers are built; restart to apply. Non-secret. A missing value can merge unrelated telemetry under an unintended name. |
| `OTEL_RESOURCE_ATTRIBUTES` | Adds comma-separated resource attributes; `OTEL_SERVICE_NAME` wins for `service.name`. | Keep values stable and bounded; define deployment/environment attributes centrally. | Read during provider creation; restart to apply. Do not include secrets or tenant/user identifiers. Malformed entries are diagnosed and ignored rather than becoming valid attributes. |
| `OTEL_TRACES_SAMPLER` / `OTEL_TRACES_SAMPLER_ARG` | Selects and configures the SDK sampler. | Prefer parent-based sampling with a measured ratio; test upstream sampling decisions. | Read during provider creation; restart to apply. Invalid values fall back according to SDK configuration behavior and emit SDK diagnostics. |
| `OTEL_SDK_DISABLED` | When `true`, builds no-op providers instead of the SDK pipeline. | Reserve for explicit emergency/diagnostic disablement with an owner and expiry. | Read at provider construction; restart to apply. Produces intentional telemetry loss, not a health-preserving fallback. |
| Processor/reader queue and interval settings | Bound batching, memory, latency, and shutdown drain. | Size from measured signal rate and the termination budget; keep network export batched. | Normally fixed when the pipeline is built. Larger queues may contain sensitive telemetry and still are not durable storage. |

### Operational signals and troubleshooting

| Symptom | Inspect | Safe action | Retry suitability |
| --- | --- | --- | --- |
| Custom spans or metrics never appear | Exact `.AddSource`/`.AddMeter` names, sampler decision, `OTEL_SDK_DISABLED`, exporter diagnostics | Align stable names, enable the intended provider, and emit a known test signal. | Retrying an operation does not repair registration and can duplicate side effects. |
| Telemetry stops or is dropped under load | SDK `EventSource` diagnostics, processor queue/export failures, process memory, collector availability | Reduce cardinality/volume, restore the exporter path, then tune bounded batch settings from load evidence. | SDK/exporter retries are appropriate only for transient transport failures; application requests should not be replayed for telemetry. |
| Duplicate spans/metrics | Provider count, duplicate instrumentation, manual spans around automatic instrumentation | Keep one hosted provider per signal and one owner for each operation boundary. | Not retryable; correct registration. |
| Shutdown loses the final batch | Host grace period, provider disposal, batch queue and export timeout | Let the host dispose providers and make the termination budget exceed the measured drain time. | A bounded `ForceFlush` is suitable at controlled shutdown/tests, never on request paths. |

### Upgrade and rollback

Keep `OpenTelemetry`, `OpenTelemetry.Api`, hosting, exporter, and cataloged instrumentations on the catalog’s aligned `1.17.0` release unless upstream compatibility explicitly permits otherwise. Before upgrading, read every intervening release note for semantic-convention, sampler, processor, resource, trim/AOT, and default changes; compile and load-test the complete pipeline, compare emitted names/attributes and series counts, and update dashboards before rollout. Roll out to a canary with exporter failures, dropped data, memory, and telemetry volume observed. Roll back all aligned OpenTelemetry packages together if provider startup, export, cardinality, or dashboard contracts regress; configuration-only rollback is insufficient when emitted schemas changed.

## Integration with the catalog

- [OpenTelemetry.Api](opentelemetry.api.md) is for instrumentation authors; it has no SDK/exporting pipeline.
- [OpenTelemetry.Exporter.OpenTelemetryProtocol](opentelemetry.exporter.opentelemetryprotocol.md) exports configured SDK signals to a collector or backend using OTLP.
- [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md) owns hosted-provider startup and shutdown.
- Add only the required instrumentations: [ASP.NET Core](opentelemetry.instrumentation.aspnetcore.md), [HTTP](opentelemetry.instrumentation.http.md), and [runtime](opentelemetry.instrumentation.runtime.md).
- `Npgsql.OpenTelemetry` integrates Npgsql activities and meters. Add its instrumentation to the same provider; do not also invent an `ActivityListener` for Npgsql.
- See the catalog-wide [OpenTelemetry composition decision](../package-guidance/package-selection.md#opentelemetry-composition), the [OTLP observability recipe](../recipes/opentelemetry-otlp-postgresql.md), and the [OpenTelemetry supply-chain entry](../package-guidance/supply-chain.md#opentelemetry).

## Security, performance, AOT, trimming, and operations

Keep exporters asynchronous and bounded. Batch processing and collector-side buffering are preferable to synchronous exporting on request paths. Exporter outages must be observable through collector and application health/diagnostic signals, but must not turn into unbounded queues or request latency.

This catalog does not claim the complete observability stack is Native-AOT or trimming safe. Test the exact application, exporter, and instrumentation combination under publish trimming/AOT. Avoid runtime discovery and reflection-based custom telemetry registration in AOT-sensitive applications; use explicit source, meter, and instrumentation registration.

## Avoid

- Do not reference the SDK from reusable domain libraries merely to emit spans; reference `OpenTelemetry.Api` and expose stable source/meter names instead.
- Do not create more than one provider for the same hosted application container.
- Do not call `ForceFlush` on request paths. Reserve it for bounded shutdown/test workflows where waiting for pending exports is explicitly required.
- Do not put PII, credentials, bearer tokens, raw SQL parameters, or high-cardinality identifiers into tags, baggage, logs, or metric labels.
- Do not treat a sampler as a privacy boundary or an exporter as durable audit storage.

## Verification checklist

- [ ] All cataloged OpenTelemetry packages remain at `1.17.0` in `Directory.Packages.props`.
- [ ] The application registers every application `ActivitySource` and `Meter` by its stable name.
- [ ] A resource has a stable, deployment-approved `service.name` and no high-cardinality resource attributes.
- [ ] Sampling, tag redaction, and exporter failure behavior have an operational owner and test coverage.
- [ ] Processor ordering and batch queue/export timeouts are load-tested against the expected telemetry rate.
- [ ] The exact publish mode used in production has been tested for trim/AOT warnings and runtime startup.

## Sources

Accessed 2026-07-27:

- [OpenTelemetry 1.17.0 on NuGet](https://www.nuget.org/packages/OpenTelemetry/1.17.0)
- [OpenTelemetry .NET SDK 1.17.0 source](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.17.0/src/OpenTelemetry)
- [Customizing the OpenTelemetry .NET 1.17.0 SDK](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.17.0/docs/trace/customizing-the-sdk/README.md)
- [OpenTelemetry .NET 1.17.0 tracing best practices](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.17.0/docs/trace/README.md)
- [OpenTelemetry .NET 1.17.0 release notes](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.17.0)
- [OpenTelemetry resource concepts](https://opentelemetry.io/docs/concepts/resources/)
- [OpenTelemetry .NET sampling](https://opentelemetry.io/docs/languages/dotnet/sampling/)
