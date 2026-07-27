# Microsoft.Extensions.Resilience

## Catalog entry

`Microsoft.Extensions.Resilience` **10.8.0** — direct catalog package; Microsoft.Extensions integration that enriches Polly resilience telemetry with metadata and exception summaries.

- **Owner:** IX
- **Last reviewed:** 2026-07-27
**Review trigger:** package or Polly version changes, target-framework changes, or .NET resilience telemetry/options changes.

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

### Configuration reference

| Setting | Purpose | Default behavior | Production guidance | Reload | Sensitive | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| `AddResilienceEnricher()` | Adds resilience telemetry enrichment | Not registered automatically | Register once in the composition root | Restart | Exception summaries can be sensitive | Base Polly telemetry remains unenriched |
| Exception summarization | Produces bounded exception metadata | Framework/package-defined | Review redaction and cardinality before export | Registration/config dependent | Yes | Missing or over-detailed summaries reduce signal quality |
| Pipeline/dependency names | Correlates telemetry | Supplied by integration | Keep stable, low-cardinality, and free of tenant/URL data | Pipeline rebuild | No | Dashboards fragment or cardinality grows |

### Upgrade and rollback

Upgrade with the matching Microsoft.Extensions train and compatible Polly packages. Verify option binding and telemetry event/attribute names against dashboards before rollout. Roll back the central pins and telemetry configuration together, keeping dashboards able to read both shapes during rolling deployment.

## Integration with the catalog

`polly.md` supplies the strategies; `polly-extensions.md` supplies DI registration and lookup. `microsoft-extensions-http-resilience.md` adds the HTTP-specific handler. Keep telemetry enrichment here and retry ownership in exactly one of those execution integrations.

Use the [resilience selection guidance](../package-guidance/package-selection.md#resilience-and-retry-ownership) and [`Microsoft.Extensions.Resilience` supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-resilience).

## Security, performance, AOT, trimming, and operations

Exception summaries and request metadata may contain secrets, personal data, internal hosts, or query strings. Apply redaction and access-control policy before export, and sample high-volume successful telemetry while retaining final failures and protective events. Enrichers add allocation and export cost; measure that cost and cap cardinality.

Telemetry must remain diagnostic when exporters are unavailable: resilience execution should not depend on a remote observability backend. Validate metadata, exception summarization, and package behavior in the actual trimmed/NativeAOT publish mode.

Verify that resilience events carry stable pipeline/dependency identifiers and bounded exception summaries, and watch event volume/cardinality after rollout. Never export secrets, payloads, full URLs, tenant/user IDs, or raw exception text without an approved redaction policy.

### Troubleshooting

| Symptom | Likely cause | Diagnostic | Correction | Retry? |
| --- | --- | --- | --- | --- |
| Dashboards have no enriched fields | Enricher not registered or wrong pipeline integration | Inspect service registration and a known resilience event | Register once at composition root and verify exporter mapping | Not applicable |
| Telemetry cardinality spikes | Dynamic pipeline/dependency names or unbounded exception metadata | Group attributes by source and inspect top cardinalities | Replace dynamic values with stable names and bounded summaries | Not applicable |
| Alerts change after upgrade | Event severity/name/attribute semantic change | Compare pre/post version event fixtures | Update configuration/dashboard or roll back companion versions | Not applicable |

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
