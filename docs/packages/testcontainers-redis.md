# Testcontainers.Redis

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `Testcontainers.Redis` |
| Pinned version | `4.13.0` |
| Status | Direct; approved only for test-role projects that require disposable Redis infrastructure |
| Role | Disposable Redis containers for integration tests |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | Testcontainers/module version, Redis image, Docker/runtime, target-framework, or CI infrastructure change |

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
using Xunit;

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

Use environment overrides only where the CI runtime requires them:

| Setting | Purpose | Upstream default | CI guidance | Sensitivity and failure behavior |
| --- | --- | --- | --- | --- |
| `DOCKER_HOST` | Select the container runtime endpoint | Auto-discovered | Set only for the approved remote/socket topology | Endpoint and transport details may be sensitive; an invalid value prevents startup |
| `TESTCONTAINERS_HOST_OVERRIDE` | Override the host exposing mapped ports | Auto-discovered | Use only for nested/remote container runners | A wrong host causes connection failures after successful startup |
| `TESTCONTAINERS_RYUK_DISABLED` | Disable resource cleanup | `false` | Keep `false`; require proven external cleanup before any exception | Disabling it can leak containers and networks |
| `TESTCONTAINERS_WAIT_STRATEGY_TIMEOUT` | Bound readiness waits | `01:00:00` | Set a finite job-appropriate deadline and retain startup logs | Too short flakes; too long obscures failed readiness |

Values are read when the Testcontainers client is created and are not intended for reload within a suite. Keep Docker and registry credentials out of source, test output, and failure attachments.

### Upgrade and rollback

Upgrade the Redis and PostgreSQL modules together when both are present so they continue to resolve one Testcontainers core version. Change the Redis image in a separate review from the package pin. Validate pull, readiness, post-start client creation, TTL/serialization behavior, isolation, cleanup, repeated/parallel execution, and redacted failure diagnostics. Roll back the core/module pins or image pin according to the failing layer; explicitly clean leaked resources through the approved CI mechanism.

## Integration with the catalog

Use with [xunit.v3](xunit-v3.md). Production client and cache behavior belong with [Microsoft.Extensions.Caching.StackExchangeRedis](microsoft-extensions-caching-stackexchangeredis.md), not this package. Use [AwesomeAssertions](awesomeassertions.md) for observable outcomes and [Testcontainers.PostgreSql](testcontainers-postgresql.md) for database tests. The `PackageReference` stays versionless. See the [PostgreSQL and Redis Testcontainers recipe](../recipes/testcontainers-postgresql-redis-xunit.md), [test-platform guidance](../package-guidance/package-selection.md#test-platform-runners-and-coverage), and the [Testcontainers.Redis supply-chain entry](../package-guidance/supply-chain.md#testcontainers-redis).

## Security, performance, AOT, trimming, and operations

Treat Docker daemon access, connection strings, keys, values, and container logs as sensitive. Do not mount host secrets or use production cache data. Network and startup cost make these integration tests slower than unit tests; control parallelism on constrained workers. The package has no production trimming or NativeAOT role.

For daemon, pull, connection, or readiness failures, record the Docker endpoint/context, daemon reachability, image reference/digest, container state and exit code, wait result, and bounded Redis/Ryuk logs. Retry pulls only after registry/network recovery. On readiness failure, inspect Redis startup output and worker resource pressure before retrying. A TTL, serialization, or atomicity assertion failure is an application/client defect and should not be converted into an infrastructure retry. Leaked resources indicate a Ryuk or CI-cleanup problem; do not solve that symptom by disabling cleanup.

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
- [Testcontainers custom configuration](https://dotnet.testcontainers.org/custom_configuration/)
- [Testcontainers resource reaper](https://dotnet.testcontainers.org/api/resource_reaper/)
- [Testcontainers.Redis 4.13.0 on NuGet](https://www.nuget.org/packages/Testcontainers.Redis/4.13.0)

Accessed 2026-07-27. Context7 was consulted first.
