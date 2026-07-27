# Microsoft.Extensions.Resilience

## Catalog entry

`Microsoft.Extensions.Resilience` **10.8.0** — direct catalog package; Microsoft.Extensions integration that enriches Polly resilience telemetry with metadata and exception summaries.

## Decision and scope

Use when non-HTTP resilience pipelines need consistent host telemetry and exception enrichment. It complements Polly; it is not a resilience strategy by itself.

## Recommended registration and use

Register resilience pipeline services at the composition root and add the resilience enricher where its telemetry context is required. Keep strategy construction in `polly.md`/`polly-extensions.md` and configure logging/metering through the host.

## Enterprise implementation guidance

Standardize dependency names and request metadata, define which exception details are safe to expose to telemetry, and verify cardinality before adding custom enrichers. Treat observability configuration as production code and test it with controlled faults.

## Integration with the catalog

`polly.md` supplies the strategies; `polly-extensions.md` supplies DI registration. `microsoft-extensions-http-resilience.md` adds the HTTP-specific handler and HTTP metrics enrichment.

## Security, performance, AOT, trimming, and operations

Exception summaries and metadata can contain sensitive values; apply the application logging policy before export. Enrichers add operational value but also allocation/cardinality cost. Validate telemetry and package behavior in the actual trimmed/NativeAOT publish mode.

## Avoid

Do not treat enriched telemetry as a substitute for a bounded policy, attach raw request secrets as metadata, or create unbounded metric dimensions.

## Verification checklist

- Induce a handled fault and assert the expected log/metric metadata.
- Inspect telemetry for secrets and high-cardinality values.
- Confirm exception summarization preserves the diagnostic signal required by operations.

## Sources

- https://www.nuget.org/packages/Microsoft.Extensions.Resilience/10.8.0 (Accessed 2026-07-27)
- https://learn.microsoft.com/en-us/dotnet/core/resilience/ (Accessed 2026-07-27)
- https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Resilience (Accessed 2026-07-27)
