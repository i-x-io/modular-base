# Microsoft.Extensions.Diagnostics.HealthChecks

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Health-check registration and service implementation | Direct; approved observability implementation |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, health-check execution/publishing, or orchestrator probe change |

## Decision and scope

Use this package to register and execute application/dependency health checks. It implements the contracts from `HealthChecks.Abstractions`; an HTTP endpoint is supplied by the web host, not by this package alone.

## Recommended registration and use

With Central Package Management, reference the implementation package without repeating its version:

```xml
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" />
```

Register a small set of tagged checks at the composition root. The ASP.NET Core host supplies `MapHealthChecks` through its shared framework; this package supplies the registration and execution services:

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddCheck<PaymentsHealthCheck>(
        "payments",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(2));

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Run();
```

The liveness endpoint above proves that the process can answer without calling remote dependencies. Readiness runs only checks tagged `ready` and can remove an instance from service when a dependency is unavailable. Keep endpoint paths and tags stable because deployment manifests and monitors consume them as an API.

### Execution configuration reference

| Setting | Purpose | Default behavior | Production guidance | Reload | Sensitivity | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| Check name/tags | Stable identity and readiness/liveness selection | Application-defined | Use bounded stable names and explicit tags | Registration-time | Names can reveal topology | Duplicate/incorrect filters can omit or conflate checks |
| `Timeout` | Bounds one check execution | No per-check timeout unless supplied | Set below the platform probe timeout and honor cancellation | Registration-time | Not sensitive | Timed-out check is reported unhealthy with timeout context |
| `FailureStatus` | Maps a failed check to degraded/unhealthy | `Unhealthy` | Reserve `Degraded` for traffic-safe impairment | Registration-time | Not sensitive | Changes overall aggregate status and routing decisions |
| Publisher period/delay/predicate | Controls background publication | Framework defaults apply | Keep publication separate from probe traffic and bound backend work | Options-dependent; treat as host configuration | Endpoint/credentials can be secret | Slow publishers can overlap or delay telemetry |

## Enterprise implementation guidance

Make probes cheap, bounded, and non-mutating. Align tags with orchestrator probe semantics and alerting ownership. A common workflow is:

1. Start with a process-only liveness endpoint.
2. Add only traffic-critical dependencies to readiness, each with an explicit timeout.
3. Keep deep diagnostics behind authorization or a management network; leave public probe responses minimal.
4. Configure the orchestrator's initial delay, period, timeout, and failure threshold so startup and brief dependency faults do not cause restart loops.
5. Exercise healthy, degraded, unhealthy, timeout, and recovery transitions before rollout.

By default the middleware maps `Healthy` and `Degraded` to HTTP 200 and `Unhealthy` to HTTP 503. If status-code mappings are changed, document and test the contract with every load balancer, orchestrator, and monitor that consumes it. Use `IHealthCheckPublisher` for an intentional periodic push workflow; configure its predicate, period, and aggregate timeout separately from request-driven endpoints.

### Upgrade and rollback

Upgrade with `Diagnostics.HealthChecks.Abstractions` and re-run readiness/liveness filtering, timeout, cancellation, concurrent execution, publisher, and degraded-state tests. Coordinate probe contract changes with deployment manifests and alerting before rollout. No data migration is required. Roll back the package and probe configuration together; preserve endpoint paths and response semantics during a rolling deployment.

## Integration with the catalog

[HealthChecks.Abstractions](microsoft-extensions-diagnostics-healthchecks-abstractions.md) defines custom checks. Register through [DependencyInjection](microsoft-extensions-dependencyinjection.md); provide endpoints from the ASP.NET Core host. Use [Hosting](microsoft-extensions-hosting.md) for worker lifecycle readiness.

Use the [abstraction-versus-runtime selection guide](../package-guidance/package-selection.md#microsoft-abstractions-and-runtime-implementations) and the [options validation, reload, and health recipe](../recipes/options-validation-reload-health.md). See the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-diagnostics-healthchecks).

## Security, performance, AOT, trimming, and operations

Do not expose dependency names, connection data, exceptions, or secrets to unauthenticated callers. Apply network restrictions or authorization to detailed health endpoints; `RequireHost` alone is not a security boundary because the Host header can be spoofed. Avoid fan-out probes that can amplify an outage, and bound cancellation/timeouts. Limit concurrent network work and do not make a health request depend on another service's health endpoint. Prefer explicit check types over reflection-based discovery for AOT/trimming clarity.

### Operational signals

| Signal | Meaning/action | Privacy/cardinality rule |
| --- | --- | --- |
| Overall status and status per stable check name | Drives probe response, dashboards, and dependency ownership | Keep names bounded; do not include tenant/resource IDs |
| Check duration and timeout count | Detects slow dependencies and probe-budget exhaustion | Record check name/status only; redact exception data |
| Degraded/unhealthy transition count | Supports alerting on state changes instead of high-volume polling logs | Alert on stable transitions and duration, not every probe request |
| Publisher failure/duration | Shows telemetry-backend or publisher saturation independently from application readiness | Never publish credentials or full check-data dictionaries |

### Troubleshooting

| Symptom | Likely causes and diagnostics | Safe corrective action | Retry suitability |
| --- | --- | --- | --- |
| Readiness fails while liveness is healthy | A tagged dependency check failed or timed out; inspect per-check status/duration and dependency telemetry | Restore dependency or shed traffic; keep liveness independent unless the process itself is irrecoverable | Platform readiness polling is sufficient; avoid nested retry storms |
| Probe endpoint times out | One check ignores cancellation, check timeout exceeds platform budget, or too many expensive checks run concurrently | Bound every external check, honor cancellation, and move deep diagnostics out of traffic probes | Retrying an unchanged saturated check adds load |
| Healthy dependency reported unhealthy | Wrong endpoint/credential, overly strict threshold, DNS/TLS issue, or check executed in the wrong tag set | Correct validated configuration or threshold; do not disable certificate validation | Retry only transient dependency failures within the next probe interval |
| Publisher emits no data | Predicate excludes checks, publisher throws, or backend is unavailable; inspect publisher logs/duration | Fix predicate/backend and keep publisher failure from masking probe state | Bounded exporter retry may be appropriate; never block readiness on it |

## Avoid

- Do not use liveness to test every remote dependency.
- Do not run write operations or costly queries in a probe.
- Do not expose the detailed response publicly by default.

## Verification checklist

- [ ] The project has a versionless package reference and restores catalog version `10.0.10`.
- [ ] Liveness and readiness have distinct routes, predicates, and expected HTTP status behavior.
- [ ] Every dependency check has a bounded timeout, honors cancellation, and returns only sanitized data.
- [ ] Authorization or network policy protects any endpoint that returns detailed diagnostics.
- [ ] Orchestrator and monitoring integrations are exercised through dependency failure and recovery without a restart loop.

## Sources

- [NuGet: Microsoft.Extensions.Diagnostics.HealthChecks 10.0.10](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.HealthChecks/10.0.10) (Accessed 2026-07-27)
- [Health checks in ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0) (Accessed 2026-07-27)
- [HealthCheckServiceCollectionExtensions API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.healthcheckservicecollectionextensions?view=net-10.0-pp) (Accessed 2026-07-27)
