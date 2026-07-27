# Microsoft.Extensions.Caching.StackExchangeRedis

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Redis-backed `IDistributedCache` implementation | Approved for application composition |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, StackExchange.Redis dependency, or Redis service/platform change |

## Decision and scope

Use this package when a process-independent cache is required and Redis is the selected shared-cache service. It is an implementation package; application code should depend on `IDistributedCache`, not Redis-specific types, unless it intentionally needs Redis capabilities outside the cache contract.

## Recommended registration and use

Register the Redis implementation once at the composition root with `AddStackExchangeRedisCache`; use `IDistributedCache` in services. Set an explicit key namespace and cache expirations appropriate to the data. Keep cache reads and writes behind a domain/application cache abstraction where cache-key semantics are business-specific.

Reference the centrally pinned provider package without a version:

```xml
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" />
```

Register the implementation with a secret-managed connection string and an environment/application prefix. `InstanceName` prefixes keys; it does not create or isolate a Redis database.

```csharp
using Microsoft.Extensions.Caching.Distributed;

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration
        .GetConnectionString("Redis")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:Redis is required.");
    options.InstanceName = "orders:production:";
});

public sealed class ProductCache(IDistributedCache cache)
{
    public async Task<string?> GetAsync(
        string productId, CancellationToken cancellationToken)
    {
        string key = $"product:{productId}";
        return await cache.GetStringAsync(key, cancellationToken);
    }

    public Task PutAsync(
        string productId, string json, CancellationToken cancellationToken) =>
        cache.SetStringAsync(
            $"product:{productId}",
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            },
            cancellationToken);

    public Task InvalidateAsync(
        string productId, CancellationToken cancellationToken) =>
        cache.RemoveAsync($"product:{productId}", cancellationToken);
}
```

A common cache-aside workflow is: build a versioned, bounded key; read; on a miss load the source of truth; serialize with an explicit schema; write with an expiration; and invalidate or version the key after a successful source-of-truth update. This package does not provide stampede suppression, tagging, or transactional cache/database updates.

### Configuration reference

| Setting | Purpose | Default behavior | Production guidance | Reload | Sensitivity | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| `Configuration` / `ConfigurationOptions` | Redis endpoints, TLS, credentials, and client policy | No usable remote cache until configured | Prefer structured `ConfigurationOptions` when client policy must be explicit; source credentials from a secret provider | Registration is effectively static; rebuild/restart the client | Secret | Connection/authentication failures surface on cache operations |
| `InstanceName` | Prefixes every cache key | No prefix | Use a stable application/environment prefix; do not treat it as access isolation | Restart/rebuild registration | May expose application/environment names | Changing it produces misses against the previous namespace |
| Entry expiration | Bounds cached-value lifetime | Caller may omit expiration | Require an explicit TTL policy and jitter hot keys | Per write | Business-sensitive when it reveals retention | Missing/incorrect TTL can create stale data or synchronized refill load |

## Enterprise implementation guidance

- Treat Redis as an optional performance dependency unless the feature explicitly requires shared state. Define miss, eviction, stale-data, deserialization, invalidation, and outage behavior before adoption.
- Use a managed identity when supported or obtain credentials from a secret provider. Separate applications, environments, and tenants with intentional prefixes plus service-side access boundaries; a prefix alone is not a security boundary.
- Add randomized expiration jitter for high-volume keys and coordinate concurrent fills where a miss burst could overload the source of truth.
- Version keys or serialized envelopes when payload schemas change. Bound key length, value size, TTL, and serialization work before accepting caller-controlled identifiers or payloads.

### Upgrade and rollback

Upgrade `Microsoft.Extensions.Caching.StackExchangeRedis` with the target framework and review its transitive StackExchange.Redis version, connection defaults, timeout behavior, and Redis server compatibility. Before rollout, verify serialization compatibility and mixed-version key reads in a staging Redis instance; deploy consumers gradually while keeping key format stable. Roll back the package and application together. If the new release wrote an incompatible payload, restore the previous key schema or invalidate only the affected namespace; do not flush a shared Redis database.

## Integration with the catalog

Use configuration and validated options for Redis settings; see [Configuration.Abstractions](microsoft-extensions-configuration-abstractions.md), [Configuration.Binder](microsoft-extensions-configuration-binder.md), and [Options](microsoft-extensions-options.md). Register through [DependencyInjection](microsoft-extensions-dependencyinjection.md). Expose dependency health separately through [HealthChecks](microsoft-extensions-diagnostics-healthchecks.md).

See [package-selection guidance for Microsoft abstractions and implementations](../package-guidance/package-selection.md#microsoft-abstractions-and-runtime-implementations) and the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-caching-stackexchangeredis).

## Security, performance, AOT, trimming, and operations

Use TLS, private network controls where available, credential rotation, and least-privilege Redis credentials. Never place connection strings in checked-in JSON, and never cache secrets or authorization decisions without a short, explicit invalidation policy. Treat cached bytes as remote/untrusted data: use bounded deserialization and avoid dangerous polymorphic type activation. Record hit/miss, operation latency, timeout/error, payload-size, and eviction/capacity signals without logging keys that contain sensitive identifiers. Test cold start, connection restoration, failover, credential rotation, latency, and server eviction. The registration and `IDistributedCache` APIs themselves do not require reflection-based binding; application serializers and configuration binding may affect trimming/AOT.

### Operational signals

| Signal | Meaning/action | Privacy/cardinality rule |
| --- | --- | --- |
| Cache hit/miss ratio by cache region | Detects ineffective keys, expiry policy, or cold-cache events; correlate with source-of-truth load | Use a bounded logical region, never raw cache keys |
| Operation latency and timeout/error count | Reveals network, server, pool, or payload pressure | Record operation kind and sanitized endpoint/service name, not credentials or values |
| Redis connection/restoration events | Detects failover, DNS, TLS, authentication, or service availability changes | Redact connection strings and certificate material |
| Server memory, eviction, connection, CPU, and command latency | Distinguishes application behavior from Redis saturation | Collect from the Redis service; do not label metrics by tenant/key |

### Troubleshooting

| Symptom | Likely causes and diagnostics | Safe corrective action | Retry suitability |
| --- | --- | --- | --- |
| Persistent misses after deployment | Changed `InstanceName`, key schema, serialization version, or database selection; compare bounded key prefixes and deployment config | Restore the intended namespace/schema or warm/version keys deliberately | Retry does not fix namespace mismatch |
| Timeout/connection failures | Redis saturation, network/TLS/DNS failure, credential rotation, oversized payload, or reconnect activity; inspect client and server signals | Restore connectivity/capacity, reduce payload/concurrency, or rotate credentials through the approved path | Only within a bounded budget when the feature treats cache as optional |
| Deserialization failures | Mixed payload schemas, partial/corrupt values, or serializer mismatch; inspect schema/version metadata without logging payloads | Treat as miss, remove only the affected key, and roll forward/back to a compatible schema | Do not repeatedly retry the same corrupt value |
| Eviction spike and source overload | Memory pressure, unbounded TTL/value size, synchronized expiry, or stampede | Bound entries, add TTL jitter/fill coordination, and increase service capacity if justified | Blind retry amplifies the overload |

## Avoid

- Do not use distributed cache as the source of truth or a cross-process lock.
- Do not make callers assume `Get` always succeeds; timeouts and outages are normal distributed-system failures.
- Do not use unbounded keys, values, or expirations.

## Verification checklist

- [ ] The consuming project references the package without a version; central package management supplies `10.0.10`.
- [ ] Redis endpoint, TLS, credentials, rotation procedure, key prefix, TTLs, and payload limits are supplied by validated configuration and policy.
- [ ] Tests cover hit, miss, concurrent miss, expiry, invalidation after a successful write, corrupt payload, timeout, outage, and recovery.
- [ ] No sensitive payload, credential, or authorization result is cached contrary to policy; telemetry does not expose identifiers or values.

## Sources

- [NuGet: Microsoft.Extensions.Caching.StackExchangeRedis 10.0.10](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis/10.0.10) — Accessed 2026-07-27.
- [Microsoft Learn: Distributed caching in ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [Microsoft Learn API: `RedisCacheOptions`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.stackexchangeredis.rediscacheoptions?view=net-10.0-pp) — Accessed 2026-07-27.
- [.NET source: StackExchange Redis cache implementation](https://github.com/dotnet/aspnetcore/tree/main/src/Caching/StackExchangeRedis) — Accessed 2026-07-27.
