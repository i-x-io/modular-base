# OpenTelemetry.Exporter.OpenTelemetryProtocol

## Catalog entry

`<PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.17.0" />`

**Role:** OTLP exporter for OpenTelemetry .NET traces, metrics, and logs. **Status:** approved central-catalog dependency; use it when a managed OpenTelemetry Collector or backend accepts OTLP.

## Decision and scope

OTLP is the OpenTelemetry wire protocol. This package exports already-collected SDK signals; it does not instrument application code or replace a collector. Prefer an OpenTelemetry Collector between applications and observability backends so routing, retries, credential handling, sampling/tail-sampling, and vendor-specific translation remain outside application processes.

## Recommended registration and use

With central package management, add a versionless application reference:

```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
</ItemGroup>
```

Use a secure collector endpoint and inject headers through deployment configuration, not source. The exporter supports gRPC and HTTP/protobuf; select the protocol that the collector ingress explicitly supports.

```csharp
using OpenTelemetry;
using OpenTelemetry.Exporter;

builder.Services.AddOpenTelemetry()
    .UseOtlpExporter(
        OtlpExportProtocol.Grpc,
        new Uri("https://otel-collector.internal:4317"));
```

`UseOtlpExporter` is the hosted, all-signal workflow: call it once and do not combine it with signal-specific `AddOtlpExporter` calls. It enables providers for all three signals, but only registered sources/meters and enabled logger categories produce telemetry. When the same overload is used with `OtlpExportProtocol.HttpProtobuf`, it treats the URL as a base and appends the signal path automatically.

A common deployment-owned configuration is:

```text
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_EXPORTER_OTLP_ENDPOINT=https://otel-collector.internal:4318/
OTEL_EXPORTER_OTLP_HEADERS=authorization=Bearer%20<injected-secret>
OTEL_EXPORTER_OTLP_TIMEOUT=10000
```

Do not commit the real header value. General variables configure all signals; `UseOtlpExporter` also supports signal-specific overrides such as `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT`. Code-set `OtlpExporterOptions` values take precedence over environment/configuration values, so prefer environment-driven settings when deployment portability is the goal.

## Enterprise implementation guidance

- Terminate or validate TLS at the collector ingress. Use a hostname covered by trusted certificates; do not disable certificate validation to make an internal endpoint work.
- Store authorization tokens/headers in a secret manager and rotate them. Scope credentials to telemetry ingestion and do not reuse application database or user credentials.
- Prefer per-signal endpoint/protocol settings only when the collector architecture requires them; otherwise keep the deployment configuration uniform and auditable.
- Tune `OTEL_BSP_MAX_QUEUE_SIZE`, `OTEL_BSP_MAX_EXPORT_BATCH_SIZE`, scheduling delay, export timeout, and metric export interval only from measured traffic and failure tests. Larger buffers increase memory and shutdown-drain time; they do not make delivery durable.
- Monitor export failures, drops, queue pressure, collector availability, and certificate-expiry events. Design bounded loss behavior instead of unbounded retries in application processes.

## Integration with the catalog

- Register with hosted applications through [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md).
- The SDK package [OpenTelemetry](opentelemetry.md) owns processors, sampling, and providers.
- Instrumentations create the data that this package exports; exporter configuration must not introduce duplicate instrumentation.

## Security, performance, AOT, trimming, and operations

OTLP payloads can contain sensitive telemetry. Encrypt in transit, authenticate ingestion, restrict network egress, redact before export, and enforce retention/access policies at the collector/backend. gRPC commonly uses port `4317` and HTTP/protobuf `4318` by convention, but configuration must use the actual deployment endpoint rather than assuming either port. The exporter documents certificate environment variables for mTLS on .NET 8+; validate the CA, client certificate/key permissions, rotation, protocol, and header formatting with a non-production collector before rollout. Test trim/AOT publishing with the exact exporter configuration.

## Avoid

- Do not send OTLP in clear text across untrusted networks.
- Do not hard-code API keys, `Authorization` values, or collector URLs containing credentials.
- Do not assume HTTP/protobuf and gRPC use identical endpoint paths.
- Do not call `UseOtlpExporter` more than once or mix it with signal-specific `AddOtlpExporter` registration.
- Do not export directly to a vendor endpoint when the approved architecture requires a collector.

## Verification checklist

- [ ] The selected protocol and endpoint match the collector’s published OTLP receiver configuration.
- [ ] TLS validation succeeds with the production trust chain; no certificate-validation bypass exists.
- [ ] Ingestion credentials come from the deployment secret mechanism and are absent from source, logs, and diagnostics.
- [ ] Trace, metric, and log delivery is validated with known test telemetry.
- [ ] Export failure, retry/drop, queue, and certificate-expiry signals are monitored.
- [ ] Graceful shutdown is long enough for the configured batch queues and export timeouts to drain within the service termination budget.

## Sources

Accessed 2026-07-27:

- [OpenTelemetry OTLP exporter 1.17.0 on NuGet](https://www.nuget.org/packages/OpenTelemetry.Exporter.OpenTelemetryProtocol/1.17.0)
- [OpenTelemetry .NET OTLP exporter source](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol)
- [OpenTelemetry .NET OTLP exporter setup and configuration](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md)
- [OpenTelemetry .NET exporter guidance](https://opentelemetry.io/docs/languages/dotnet/exporters/)
- [OTLP specification](https://opentelemetry.io/docs/specs/otlp/)
- [OpenTelemetry Collector configuration](https://opentelemetry.io/docs/collector/configuration/)
