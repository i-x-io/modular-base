# Microsoft.Extensions.Diagnostics.HealthChecks

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Health-check registration and service implementation | Approved observability implementation |

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

## Enterprise implementation guidance

Make probes cheap, bounded, and non-mutating. Align tags with orchestrator probe semantics and alerting ownership. A common workflow is:

1. Start with a process-only liveness endpoint.
2. Add only traffic-critical dependencies to readiness, each with an explicit timeout.
3. Keep deep diagnostics behind authorization or a management network; leave public probe responses minimal.
4. Configure the orchestrator's initial delay, period, timeout, and failure threshold so startup and brief dependency faults do not cause restart loops.
5. Exercise healthy, degraded, unhealthy, timeout, and recovery transitions before rollout.

By default the middleware maps `Healthy` and `Degraded` to HTTP 200 and `Unhealthy` to HTTP 503. If status-code mappings are changed, document and test the contract with every load balancer, orchestrator, and monitor that consumes it. Use `IHealthCheckPublisher` for an intentional periodic push workflow; configure its predicate, period, and aggregate timeout separately from request-driven endpoints.

## Integration with the catalog

[HealthChecks.Abstractions](microsoft-extensions-diagnostics-healthchecks-abstractions.md) defines custom checks. Register through [DependencyInjection](microsoft-extensions-dependencyinjection.md); provide endpoints from the ASP.NET Core host. Use [Hosting](microsoft-extensions-hosting.md) for worker lifecycle readiness.

## Security, performance, AOT, trimming, and operations

Do not expose dependency names, connection data, exceptions, or secrets to unauthenticated callers. Apply network restrictions or authorization to detailed health endpoints; `RequireHost` alone is not a security boundary because the Host header can be spoofed. Avoid fan-out probes that can amplify an outage, and bound cancellation/timeouts. Limit concurrent network work and do not make a health request depend on another service's health endpoint. Prefer explicit check types over reflection-based discovery for AOT/trimming clarity.

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
