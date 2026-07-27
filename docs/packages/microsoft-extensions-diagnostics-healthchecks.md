# Microsoft.Extensions.Diagnostics.HealthChecks

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Health-check registration and service implementation | Approved observability implementation |

## Decision and scope

Use this package to register and execute application/dependency health checks. It implements the contracts from `HealthChecks.Abstractions`; an HTTP endpoint is supplied by the web host, not by this package alone.

## Recommended registration and use

Register a small set of tagged checks at the composition root. Separate liveness (the process can run) from readiness (it can safely receive traffic) and use dependency checks only for readiness. Set explicit per-check timeouts and return diagnostic data that is safe for the intended audience.

## Enterprise implementation guidance

Make probes cheap, bounded, and non-mutating. Align tags with orchestrator probe semantics and alerting ownership. Health reporting is an operational contract: version probe routes, document status codes, and test degraded/unhealthy behavior before rollout.

## Integration with the catalog

[HealthChecks.Abstractions](microsoft-extensions-diagnostics-healthchecks-abstractions.md) defines custom checks. Register through [DependencyInjection](microsoft-extensions-dependencyinjection.md); provide endpoints from the ASP.NET Core host. Use [Hosting](microsoft-extensions-hosting.md) for worker lifecycle readiness.

## Security, performance, AOT, trimming, and operations

Do not expose dependency names, connection data, exceptions, or secrets to unauthenticated callers. Apply network restrictions or authorization to detailed health endpoints. Avoid fan-out probes that can amplify an outage, and bound cancellation/timeouts. Prefer explicit check types over reflection-based discovery for AOT/trimming clarity.

## Avoid

- Do not use liveness to test every remote dependency.
- Do not run write operations or costly queries in a probe.
- Do not expose the detailed response publicly by default.

## Verification checklist

- Liveness and readiness have distinct tags/routes and expected status behavior.
- Checks time out, honor cancellation, and do not reveal secrets.
- Orchestrator and monitoring integrations are exercised under dependency failure.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.HealthChecks) (Accessed 2026-07-27)
- [Health checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0) (Accessed 2026-07-27)
