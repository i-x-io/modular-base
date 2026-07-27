# Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Health-check contracts including `IHealthCheck`, `HealthCheckResult`, and `HealthCheckContext` | Approved library-facing abstraction |

## Decision and scope

Reference this package to implement a reusable custom health check without coupling it to a particular endpoint or host. It defines contracts only; use the companion implementation package to register and run checks.

## Recommended registration and use

Implement `IHealthCheck` for one observable dependency or capability, honor the supplied cancellation token, and return `Healthy`, `Degraded`, or `Unhealthy` according to a documented operational threshold. Register it in the application via [HealthChecks](microsoft-extensions-diagnostics-healthchecks.md).

## Enterprise implementation guidance

Give each check a stable registration name, tags, timeout, and owner. Report only sanitized diagnostics; retain detailed failure information in protected logs/telemetry. Make check work read-only and safe to execute concurrently.

## Integration with the catalog

The runner and registration APIs are in [HealthChecks](microsoft-extensions-diagnostics-healthchecks.md). Its lifetime dependencies come from [DependencyInjection](microsoft-extensions-dependencyinjection.md), and it commonly reflects [Hosting](microsoft-extensions-hosting.md) state.

## Security, performance, AOT, trimming, and operations

Health-check output can disclose topology or credentials. Keep `Data` values sanitized and protect detailed endpoint output. Use cancellation, short timeouts, and bounded concurrency. The abstraction has no reflection requirement; application-specific discovery mechanisms may not be AOT-safe.

## Avoid

- Do not throw expected dependency exceptions instead of returning a health result.
- Do not reuse one check for unrelated dependencies.
- Do not let a check mutate production state.

## Verification checklist

- Healthy, degraded, unhealthy, timeout, and cancellation results are tested.
- Diagnostic output is safe for the endpoint's audience.
- Registration tags and timeout match probe semantics.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions) (Accessed 2026-07-27)
- [IHealthCheck API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks.ihealthcheck) (Accessed 2026-07-27)
