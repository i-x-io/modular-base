# Microsoft.Extensions.Caching.StackExchangeRedis

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Redis-backed `IDistributedCache` implementation | Approved for application composition |

## Decision and scope

Use this package when a process-independent cache is required and Redis is the selected shared-cache service. It is an implementation package; application code should depend on `IDistributedCache`, not Redis-specific types, unless it intentionally needs Redis capabilities outside the cache contract.

## Recommended registration and use

Register the Redis implementation once at the composition root with `AddStackExchangeRedisCache`; use `IDistributedCache` in services. Set an explicit key namespace and cache expirations appropriate to the data. Keep cache reads and writes behind a domain/application cache abstraction where cache-key semantics are business-specific.

## Enterprise implementation guidance

Treat Redis as an optional performance dependency unless the feature explicitly requires shared state. Define eviction, invalidation, and outage behavior before adopting it. Use a managed identity or a secret provider for the connection configuration, and separate environments and tenants with intentional key prefixes.

## Integration with the catalog

Use configuration and validated options for Redis settings; see [Configuration.Abstractions](microsoft-extensions-configuration-abstractions.md), [Configuration.Binder](microsoft-extensions-configuration-binder.md), and [Options](microsoft-extensions-options.md). Register through [DependencyInjection](microsoft-extensions-dependencyinjection.md). Expose dependency health separately through [HealthChecks](microsoft-extensions-diagnostics-healthchecks.md).

## Security, performance, AOT, trimming, and operations

Use TLS and least-privilege Redis credentials; never cache secrets or authorization decisions without a short, explicit invalidation policy. Cache values are remote data: serialize defensively, bound value sizes, and record hit/miss/latency telemetry. This package does not remove the need to test Redis connection, eviction, failover, and timeout behavior. Its public registration APIs do not require reflection-based binding; options binding can affect trimming/AOT (see the Options guides).

## Avoid

- Do not use distributed cache as the source of truth or a cross-process lock.
- Do not make callers assume `Get` always succeeds; timeouts and outages are normal distributed-system failures.
- Do not use unbounded keys, values, or expirations.

## Verification checklist

- Redis endpoint, TLS, credentials, and key prefix are supplied by validated configuration.
- A cache miss, expiry, invalidation, and Redis outage have tested application behavior.
- No sensitive payload or authorization result is cached contrary to policy.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis) (Accessed 2026-07-27)
- [Distributed caching in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0) (Accessed 2026-07-27)
