# Microsoft.Extensions.Resilience

## Catalog entry

`Microsoft.Extensions.Resilience` **10.8.0** — direct catalog package; Microsoft.Extensions integration that enriches Polly resilience telemetry with metadata and exception summaries.

## Decision and scope

Use when non-HTTP resilience pipelines need consistent host telemetry and exception enrichment. It complements Polly; it is not a resilience strategy, retry provider, or HTTP handler by itself.

## Recommended registration and use

With central package management, reference the package without repeating its catalog version:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Resilience" />
</ItemGroup>
```

Register the enricher once at the composition root, alongside the host's logging and telemetry configuration:

```csharp
builder.Services.AddResilienceEnricher();
```

`AddResilienceEnricher` adds resilience metadata to Polly telemetry. When an `IExceptionSummarizer` is already registered, the enricher can add the summarized exception information as well. Configure actual retry, timeout, circuit-breaker, and rate-limiter behavior through a single owned Polly pipeline; adding the enricher does not change those strategies.

## Enterprise implementation guidance

Define a small, stable vocabulary for dependency name, operation name, pipeline name, and final outcome. Put identifiers with unbounded values—tenant IDs, full URLs, record IDs, exception messages, and request content—in traces or protected logs only when policy permits, never in metric dimensions.

A common rollout workflow is:

1. Register the host telemetry providers and optional exception summarizer.
2. Register `AddResilienceEnricher` once.
3. Execute controlled retry, timeout, rate-limit, and circuit-open scenarios.
4. Confirm one logical operation can be followed across attempts without duplicating retry ownership.
5. Establish dashboards and alerts for final failure rate, breaker state transitions, rejected executions, and latency-budget exhaustion.

Version and review telemetry conventions with the resilience policy. Operators should be able to answer which dependency failed, which pipeline handled it, how many attempts occurred, and whether the final outcome escaped to the caller.

## Integration with the catalog

`polly.md` supplies the strategies; `polly-extensions.md` supplies DI registration and lookup. `microsoft-extensions-http-resilience.md` adds the HTTP-specific handler. Keep telemetry enrichment here and retry ownership in exactly one of those execution integrations.

## Security, performance, AOT, trimming, and operations

Exception summaries and request metadata may contain secrets, personal data, internal hosts, or query strings. Apply redaction and access-control policy before export, and sample high-volume successful telemetry while retaining final failures and protective events. Enrichers add allocation and export cost; measure that cost and cap cardinality.

Telemetry must remain diagnostic when exporters are unavailable: resilience execution should not depend on a remote observability backend. Validate metadata, exception summarization, and package behavior in the actual trimmed/NativeAOT publish mode.

## Avoid

Do not treat enriched telemetry as a bounded resilience policy, add raw request bodies or credentials as metadata, use exception messages as metric tags, create per-request pipeline names, or alert on every retry without considering the final outcome. Do not register a second retry simply to obtain more telemetry.

## Verification checklist

- [ ] Confirm the project resolves the centrally managed `10.8.0` package.
- [ ] Induce a handled transient fault and assert dependency, pipeline, attempt, and final-outcome context.
- [ ] Exercise timeout, rate-limit rejection, and circuit-open paths, not only retry success.
- [ ] Inspect exported telemetry for secrets, personal data, full URLs, and high-cardinality dimensions.
- [ ] Verify exception summarization retains the approved diagnostic signal without exposing raw sensitive details.
- [ ] Disable or interrupt the exporter and confirm resilience execution remains correct.

## Sources

- [NuGet: Microsoft.Extensions.Resilience 10.8.0](https://www.nuget.org/packages/Microsoft.Extensions.Resilience/10.8.0) (Accessed 2026-07-27)
- [.NET resilience overview](https://learn.microsoft.com/en-us/dotnet/core/resilience/) (Accessed 2026-07-27)
- [.NET Extensions: Microsoft.Extensions.Resilience source and README](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Resilience) (Accessed 2026-07-27)
- [Polly telemetry documentation](https://www.pollydocs.org/advanced/telemetry.html) (Accessed 2026-07-27)
