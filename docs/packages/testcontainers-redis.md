# Testcontainers.Redis

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `Testcontainers.Redis` |
| Pinned version | `4.13.0` |
| Status | Approved test-only dependency |
| Role | Disposable Redis containers for integration tests |

## Decision and scope

Use when integration tests require the actual Redis protocol, expiry, serialization, atomic operations, or connection behavior. It supplies disposable test infrastructure; it does not configure the production cache client or replace performance and resilience testing.

## Recommended registration and use

Add versionless references only to a `Test` or `ArchitectureTest` project with `IsTestProject=true`:

```xml
<PackageReference Include="xunit.v3" />
<PackageReference Include="Testcontainers.Redis" />
```

Use xUnit's asynchronous lifecycle, a reviewed image pin, and the post-start connection string:

```csharp
using Testcontainers.Redis;

public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container =
        new RedisBuilder("redis:8.0.2-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() =>
        await _container.StartAsync();

    public async ValueTask DisposeAsync() =>
        await _container.DisposeAsync();
}
```

The image is an example deterministic pin; align it with the supported production major/minor and validate upgrades. Construct the test-owned Redis client after startup, use a unique key prefix per test, assert value and TTL behavior, and delete the prefix during cleanup.

## Enterprise implementation guidance

Share a fixture only when the suite has a reliable reset contract. Unique prefixes reduce collisions but do not replace cleanup when testing scans, key counts, eviction, or pub/sub. Avoid timing tests that sleep until expiry; use bounded polling with a deadline and enough tolerance for CI scheduling, while keeping the Redis TTL itself explicit.

Run container tests in a labeled Docker-capable CI job with pull/start timeouts and resource limits. Capture bounded, redacted logs on infrastructure failure and keep unit tests independent of Docker availability.

## Integration with the catalog

Use with [xunit.v3](xunit-v3.md). Production client and cache behavior belong with `Microsoft.Extensions.Caching.StackExchangeRedis`, not this package. Use [AwesomeAssertions](awesomeassertions.md) for observable outcomes and [Testcontainers.PostgreSql](testcontainers-postgresql.md) for database tests. The project reference stays versionless.

## Security, performance, AOT, trimming, and operations

Treat Docker daemon access, connection strings, keys, values, and container logs as sensitive. Do not mount host secrets or use production cache data. Network and startup cost make these integration tests slower than unit tests; control parallelism on constrained workers. The package has no production trimming or NativeAOT role.

## Avoid

- Do not connect tests to shared, staging, or production Redis instances.
- Do not use `latest`, static host ports, global key names, or unbounded sleeps.
- Do not share a fixture across tests that cannot prove cleanup and isolation.
- Do not expose Docker credentials, connection strings, or cached payloads in failures.

## Verification checklist

- [ ] The test starts a pinned image and creates its client from the post-start connection string.
- [ ] A focused test writes, reads, expires, and cleans up a uniquely namespaced key.
- [ ] Repeated and parallel runs cannot share keys or observe stale data.
- [ ] CI separates Docker tests, bounds startup/runtime, cleans resources, and redacts diagnostics.

## Sources

- [Testcontainers for .NET Redis module](https://dotnet.testcontainers.org/modules/redis/)
- [Testcontainers for .NET xUnit guidance](https://dotnet.testcontainers.org/test_frameworks/xunit_net/)
- [Testcontainers CI guidance](https://dotnet.testcontainers.org/cicd/)
- [Testcontainers.Redis 4.13.0 on NuGet](https://www.nuget.org/packages/Testcontainers.Redis/4.13.0)

Accessed 2026-07-27. Context7 was consulted first.
