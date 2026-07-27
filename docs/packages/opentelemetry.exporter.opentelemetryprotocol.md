# OpenTelemetry.Exporter.OpenTelemetryProtocol

## Catalog entry

`<PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.17.0" />`

**Role:** OTLP exporter for OpenTelemetry .NET traces, metrics, and logs. **Status:** approved central-catalog dependency; use it when a managed OpenTelemetry Collector or backend accepts OTLP.

## Decision and scope

OTLP is the OpenTelemetry wire protocol. This package exports already-collected SDK signals; it does not instrument application code or replace a collector. Prefer an OpenTelemetry Collector between applications and observability backends so routing, retries, credential handling, sampling/tail-sampling, and vendor-specific translation remain outside application processes.

## Recommended registration and use

Use a secure collector endpoint and inject headers through deployment configuration, not source. The exporter supports gRPC and HTTP/protobuf; select the protocol that the collector ingress explicitly supports.

```csharp
using OpenTelemetry;
using OpenTelemetry.Exporter;

builder.Services.AddOpenTelemetry()
    .UseOtlpExporter(
        OtlpExportProtocol.Grpc,
        new Uri("https://otel-collector.internal:4317"));
```

For HTTP/protobuf, use the collector’s documented endpoint. The official exporter documentation distinguishes base endpoints from signal-specific paths; verify whether the configured endpoint includes `/v1/traces`, `/v1/metrics`, or `/v1/logs` for the exact options/protocol used. Configure headers with `OTEL_EXPORTER_OTLP_HEADERS` or `OtlpExporterOptions.Headers` only from a secret-bearing deployment configuration source.

## Enterprise implementation guidance

- Terminate or validate TLS at the collector ingress. Use a hostname covered by trusted certificates; do not disable certificate validation to make an internal endpoint work.
- Store authorization tokens/headers in a secret manager and rotate them. Scope credentials to telemetry ingestion and do not reuse application database or user credentials.
- Prefer per-signal endpoint/protocol settings only when the collector architecture requires them; otherwise keep the deployment configuration uniform and auditable.
- Monitor export failures, drops, queue pressure, collector availability, and certificate-expiry events. Design bounded loss behavior instead of unbounded retries in application processes.

## Integration with the catalog

- Register with hosted applications through [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md).
- The SDK package [OpenTelemetry](opentelemetry.md) owns processors, sampling, and providers.
- Instrumentations create the data that this package exports; exporter configuration must not introduce duplicate instrumentation.

## Security, performance, AOT, trimming, and operations

OTLP payloads can contain sensitive telemetry. Encrypt in transit, authenticate ingestion, restrict network egress, redact before export, and enforce retention/access policies at the collector/backend. gRPC commonly uses port `4317` and HTTP/protobuf `4318` by convention, but configuration must use the actual deployment endpoint rather than assuming either port. Validate protocol, TLS trust, and header formatting with a non-production collector before rollout. Test trim/AOT publishing with the exact exporter configuration.

## Avoid

- Do not send OTLP in clear text across untrusted networks.
- Do not hard-code API keys, `Authorization` values, or collector URLs containing credentials.
- Do not assume HTTP/protobuf and gRPC use identical endpoint paths.
- Do not export directly to a vendor endpoint when the approved architecture requires a collector.

## Verification checklist

- [ ] The selected protocol and endpoint match the collector’s published OTLP receiver configuration.
- [ ] TLS validation succeeds with the production trust chain; no certificate-validation bypass exists.
- [ ] Ingestion credentials come from the deployment secret mechanism and are absent from source, logs, and diagnostics.
- [ ] Trace, metric, and log delivery is validated with known test telemetry.
- [ ] Export failure, retry/drop, queue, and certificate-expiry signals are monitored.

## Sources

Accessed 2026-07-27:

- https://www.nuget.org/packages/OpenTelemetry.Exporter.OpenTelemetryProtocol/1.17.0
- https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol
- https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md
- https://opentelemetry.io/docs/specs/otlp/
- https://opentelemetry.io/docs/collector/configuration/
