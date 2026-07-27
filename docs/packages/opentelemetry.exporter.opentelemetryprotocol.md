# OpenTelemetry.Exporter.OpenTelemetryProtocol

> **Owner:** `IX`
> **Last reviewed:** `2026-07-27`
> **Review trigger:** Review when the OTLP exporter version, collector receiver, transport/security policy, target framework, or OTLP specification changes.

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

### Configuration reference

Signal-specific `..._TRACES_...`, `..._METRICS_...`, and `..._LOGS_...` variables override the corresponding all-signal setting. Code configuration takes precedence over environment/configuration values.

| Setting | Purpose and default behavior | Production guidance | Reload, sensitivity, and failure behavior |
| --- | --- | --- | --- |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Base collector URI; protocol defaults determine the transport and signal paths. | Set the exact TLS endpoint published by the receiver; for HTTP/protobuf, understand whether an API expects a base or signal-specific URL. | Captured when the exporter is built; restart to apply. Endpoint topology may be sensitive. Invalid/unreachable URIs prevent export and surface SDK diagnostics. |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | Selects `grpc` or `http/protobuf`. | Match collector ingress and proxy capabilities explicitly. | Restart to apply. A mismatch commonly produces connection, HTTP status, or protocol errors rather than automatic negotiation. |
| `OTEL_EXPORTER_OTLP_HEADERS` | Adds comma-separated, URL-encoded request metadata. | Inject scoped ingestion credentials from a secret manager. | Restart to apply; secret. Bad encoding or expired credentials causes rejected exports—never log the full value. |
| `OTEL_EXPORTER_OTLP_TIMEOUT` | Bounds an export attempt in milliseconds. | Keep below the service shutdown budget and tune from collector latency. | Restart to apply. Too low increases timeout loss; too high prolongs drain/failure detection. |
| `OTEL_EXPORTER_OTLP_COMPRESSION` | Selects `none` or `gzip`. | Enable only after measuring CPU versus network savings and receiver support. | Restart to apply. Unsupported/mismatched configuration fails export. |
| `OTEL_BSP_*` / `OTEL_BLRP_*` | Bounds trace/log batch delay, timeout, queue size, and batch size. | Keep queues bounded and batch size no larger than queue size; load-test outage behavior. | Fixed with processor construction. Queues hold potentially sensitive telemetry in memory and overflow drops data. |
| `OTEL_METRIC_EXPORT_INTERVAL` / `OTEL_METRIC_EXPORT_TIMEOUT` | Controls periodic metric export cadence and timeout. | Balance alert latency, backend cost, and termination drain. | Fixed with reader construction. A timeout longer than interval can create persistent export pressure. |
| `OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE` | Requests cumulative, delta, or low-memory behavior where supported. | Match collector/backend expectations before changing. | Restart to apply. A mismatch can alter rates/deltas and dashboard meaning. |

### Operational signals and troubleshooting

The exporter reports internal failures through the OpenTelemetry .NET SDK `EventSource`; collect those diagnostics alongside collector receiver/exporter metrics, backend ingestion evidence, process memory, and shutdown timing. Never add the authorization header value to diagnostics.

| Symptom | Likely cause and diagnostics | Safe corrective action | Retry suitability |
| --- | --- | --- | --- |
| `Unauthenticated`, `PermissionDenied`, HTTP 401/403 | Missing/expired/malformed header; inspect collector ingress logs and credential metadata without printing the token. | Correct URL-encoding, secret injection, scope, and rotation. | Retry only after credentials/configuration changes. |
| `Unimplemented`, HTTP 404/415, protocol parse error | gRPC sent to HTTP/protobuf ingress, wrong HTTP signal path, or incompatible proxy. | Align protocol, base/signal endpoint, and receiver configuration. | Not transient; do not amplify with retries. |
| Deadline/connection failures | Collector unavailable, DNS/TLS/proxy failure, or timeout too low; correlate SDK diagnostics with collector and network health. | Restore the route/trust chain, then tune timeout from measured latency. Never disable TLS validation. | Bounded exporter retry is suitable for transient failures; application operations must not be replayed. |
| Gaps during load/outage | Batch queue overflow, collector throttling, backend limit, or process shutdown before drain. | Reduce volume/cardinality, scale the collector, and tune bounded queues from load tests. | Queues/retry are not durable delivery; accept/document bounded loss. |
| Metrics become rates/levels incorrectly | Temporality or aggregation expectation changed. | Align exporter preference, collector processors, backend queries, and dashboards as one rollout. | Not retryable; this is a semantic configuration error. |

### Upgrade and rollback

Upgrade the exporter with the aligned OpenTelemetry family and verify the collector/backend supports the selected OTLP protocol, compression, metric temporality, histogram aggregation, TLS/mTLS, and per-signal endpoint behavior. Review 1.17-era changes to timeout handling, retry/drop behavior, and batch-option precedence; run an outage, authentication-failure, overload, and graceful-shutdown test before canary rollout. Compare exported schemas, volume, CPU/memory, and rejection/drop counts. Roll back exporter and SDK packages together when transport or processor behavior regresses, and restore the previous collector/dashboard configuration if temporality or aggregation changed. Telemetry queued in memory cannot be recovered by rollback.

## Integration with the catalog

- Register with hosted applications through [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md).
- The SDK package [OpenTelemetry](opentelemetry.md) owns processors, sampling, and providers.
- Instrumentations create the data that this package exports; exporter configuration must not introduce duplicate instrumentation.
- See the catalog-wide [OpenTelemetry composition decision](../package-guidance/package-selection.md#opentelemetry-composition), the [OTLP observability recipe](../recipes/opentelemetry-otlp-postgresql.md), and the [exporter supply-chain entry](../package-guidance/supply-chain.md#opentelemetry-exporter-opentelemetryprotocol).

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
- [OpenTelemetry .NET OTLP exporter 1.17.0 source](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.17.0/src/OpenTelemetry.Exporter.OpenTelemetryProtocol)
- [OpenTelemetry .NET OTLP exporter 1.17.0 setup and configuration](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.17.0/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md)
- [OpenTelemetry .NET 1.17.0 release notes](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.17.0)
- [OpenTelemetry .NET exporter guidance](https://opentelemetry.io/docs/languages/dotnet/exporters/)
- [OTLP specification](https://opentelemetry.io/docs/specs/otlp/)
- [OpenTelemetry Collector configuration](https://opentelemetry.io/docs/collector/configuration/)
