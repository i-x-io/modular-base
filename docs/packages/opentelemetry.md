# OpenTelemetry

## Catalog entry

`<PackageVersion Include="OpenTelemetry" Version="1.17.0" />`

**Role:** the OpenTelemetry .NET SDK package. It supplies SDK builders, providers, processors, samplers, resources, and the in-process pipeline for traces, metrics, and logs. **Status:** approved central-catalog dependency; add it to an application only when that application configures the SDK directly or through the hosting extensions.

## Decision and scope

Use this package for SDK behavior. It is not an exporter, protocol implementation, or instrumentation package. `OpenTelemetry.Api` is the lightweight API surface that libraries use to create `ActivitySource`, `Meter`, and logging-facing telemetry; applications use this SDK package to collect and process that telemetry. The catalog intentionally keeps both packages at the same version.

## Recommended registration and use

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

## Enterprise implementation guidance

- Define `service.name`, `service.version`, and deployment-environment resource attributes once at the application composition root. `service.name` identifies the emitting service; it is not a per-request, tenant, user, pod, or database value.
- Use parent-based ratio sampling for normal production traffic and an intentionally documented policy for error retention. Sampling changes what is available for diagnosis; it is not a data-redaction control.
- Keep metric label cardinality bounded. Never use request IDs, user IDs, email addresses, full URLs, SQL text, opaque tokens, or unbounded exception messages as metric dimensions.
- Treat telemetry as sensitive production data. Filter/redact headers, query strings, route values, database statements, and exception data before export, and restrict collector/backend access accordingly.

## Integration with the catalog

- [OpenTelemetry.Api](opentelemetry.api.md) is for instrumentation authors; it has no SDK/exporting pipeline.
- [OpenTelemetry.Exporter.OpenTelemetryProtocol](opentelemetry.exporter.opentelemetryprotocol.md) exports configured SDK signals to a collector or backend using OTLP.
- [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md) owns hosted-provider startup and shutdown.
- Add only the required instrumentations: [ASP.NET Core](opentelemetry.instrumentation.aspnetcore.md), [HTTP](opentelemetry.instrumentation.http.md), and [runtime](opentelemetry.instrumentation.runtime.md).
- `Npgsql.OpenTelemetry` integrates Npgsql activities and meters. Add its instrumentation to the same provider; do not also invent an `ActivityListener` for Npgsql.

## Security, performance, AOT, trimming, and operations

Keep exporters asynchronous and bounded. Batch processing and collector-side buffering are preferable to synchronous exporting on request paths. Exporter outages must be observable through collector and application health/diagnostic signals, but must not turn into unbounded queues or request latency.

This catalog does not claim the complete observability stack is Native-AOT or trimming safe. Test the exact application, exporter, and instrumentation combination under publish trimming/AOT. Avoid runtime discovery and reflection-based custom telemetry registration in AOT-sensitive applications; use explicit source, meter, and instrumentation registration.

## Avoid

- Do not reference the SDK from reusable domain libraries merely to emit spans; reference `OpenTelemetry.Api` and expose stable source/meter names instead.
- Do not create more than one provider for the same hosted application container.
- Do not put PII, credentials, bearer tokens, raw SQL parameters, or high-cardinality identifiers into tags, baggage, logs, or metric labels.
- Do not treat a sampler as a privacy boundary or an exporter as durable audit storage.

## Verification checklist

- [ ] All cataloged OpenTelemetry packages remain at `1.17.0` in `Directory.Packages.props`.
- [ ] The application registers every application `ActivitySource` and `Meter` by its stable name.
- [ ] A resource has a stable, deployment-approved `service.name` and no high-cardinality resource attributes.
- [ ] Sampling, tag redaction, and exporter failure behavior have an operational owner and test coverage.
- [ ] The exact publish mode used in production has been tested for trim/AOT warnings and runtime startup.

## Sources

Accessed 2026-07-27:

- https://www.nuget.org/packages/OpenTelemetry/1.17.0
- https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry
- https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/trace/customizing-the-sdk/README.md
- https://opentelemetry.io/docs/concepts/resources/
- https://opentelemetry.io/docs/concepts/sampling/
