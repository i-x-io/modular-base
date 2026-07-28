# Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Health-check contracts including `IHealthCheck`, `HealthCheckResult`, and `HealthCheckContext` | Direct; approved library-facing abstraction |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, public health-check contract, or status-model change |

## Decision and scope

Reference this package to implement a reusable custom health check without coupling it to a particular endpoint or host. It defines contracts only; use the companion implementation package to register and run checks.

## Recommended registration and use

With Central Package Management, a reusable library references the contract package without a version:

```xml
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions" />
```

Implement `IHealthCheck` for one observable dependency or capability and pass the supplied cancellation token through every asynchronous call:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

public interface IPaymentsProbe
{
    Task<bool> CanAcceptTrafficAsync(CancellationToken cancellationToken);
}

public sealed class PaymentsHealthCheck(IPaymentsProbe probe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var available = await probe.CanAcceptTrafficAsync(cancellationToken);

        return available
            ? HealthCheckResult.Healthy()
            : new HealthCheckResult(
                context.Registration.FailureStatus,
                description: "Payments dependency is unavailable.");
    }
}
```

The library owns the check contract and implementation only. The consuming application owns its stable name, failure status, tags, timeout, endpoint exposure, and any retry policy through [HealthChecks](microsoft-extensions-diagnostics-healthchecks.md). Return `Healthy`, `Degraded`, or `Unhealthy` according to a documented operational threshold.

## Enterprise implementation guidance

Give each check a stable registration name, tags, timeout, and owner at the composition root. Report only sanitized diagnostics; retain detailed failure information in protected logs/telemetry. Make check work read-only and safe to execute concurrently. Treat expected negative state as a result. Let unexpected exceptions reach the runner only when the application's registered `FailureStatus` and protected telemetry behavior are the intended contract; never swallow cancellation.

For reusable packages, document the dependency queried, expected latency, permissions required, possible statuses, and whether concurrent calls are supported. Do not make the abstraction package depend on ASP.NET Core endpoint middleware or a concrete logging provider.

### Upgrade and rollback

Keep the abstraction aligned with the concrete HealthChecks implementation used by the host. Recompile custom checks and verify status, duration, exception, and data handling against the new runtime. No state migration is required. Roll back custom-check libraries with their host when contract compatibility fails; keep externally exposed probe semantics stable.

## Integration with the catalog

The runner and registration APIs are in [HealthChecks](microsoft-extensions-diagnostics-healthchecks.md). Its lifetime dependencies come from [DependencyInjection](microsoft-extensions-dependencyinjection.md), and it commonly reflects [Hosting](microsoft-extensions-hosting.md) state.

Use the [abstraction-versus-runtime selection guide](../package-guidance/package-selection.md#microsoft-abstractions-and-runtime-implementations) to keep reusable checks independent from host execution. See the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-diagnostics-healthchecks-abstractions).

## Security, performance, AOT, trimming, and operations

Health-check output can disclose topology or credentials. Keep descriptions and `Data` values sanitized and protect detailed endpoint output. Use cancellation, short application-owned registration timeouts, and bounded concurrency. A cancellation token signals that work must stop; do not translate cancellation into a stale healthy result. The abstraction has no reflection requirement; application-specific discovery mechanisms may not be AOT-safe.

## Avoid

- Do not throw expected dependency exceptions instead of returning a health result.
- Do not reuse one check for unrelated dependencies.
- Do not let a check mutate production state.

## Verification checklist

- [ ] The reusable project has only the versionless abstractions reference and restores catalog version `10.0.10`.
- [ ] Healthy, degraded/unhealthy, exception, timeout, and cancellation behavior is tested.
- [ ] The implementation passes cancellation to dependency calls and performs no production mutation.
- [ ] Descriptions and `Data` are safe for the endpoint's audience.
- [ ] The consuming application owns the registration name, failure status, tags, timeout, and endpoint mapping.

## Sources

- [NuGet: Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions 10.0.10](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions/10.0.10) (Accessed 2026-07-27)
- [IHealthCheck API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks.ihealthcheck?view=net-10.0-pp) (Accessed 2026-07-27)
- [HealthCheckResult API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks.healthcheckresult?view=net-10.0-pp) (Accessed 2026-07-27)
