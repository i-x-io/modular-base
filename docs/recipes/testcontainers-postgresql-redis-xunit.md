# PostgreSQL and Redis integration tests with Testcontainers and xUnit v3

## Problem and boundary

This recipe gives an xUnit v3 test class disposable PostgreSQL and Redis dependencies so it can verify real protocol, serialization, transaction, and cache behavior. Testcontainers owns Docker resources and dynamic ports, xUnit owns fixture lifecycle, Npgsql owns PostgreSQL access, and `Microsoft.Extensions.Caching.StackExchangeRedis` owns the production-shaped `IDistributedCache` registration. The test owns schema setup, unique data, assertions, and cleanup.

Docker-backed tests are integration tests. They complement unit tests; they do not replace production migration rehearsal, Redis topology/failover tests, performance tests, or security validation.

## Required packages

Use a dedicated integration test project with Microsoft Testing Platform:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <PackageReference Include="Npgsql" />
    <PackageReference Include="Testcontainers.PostgreSql" />
    <PackageReference Include="Testcontainers.Redis" />
    <PackageReference Include="xunit.v3" />
  </ItemGroup>
</Project>
```

`xunit.v3` supplies test discovery and `IAsyncLifetime`. Each Testcontainers module supplies a typed builder/container and post-start connection string. Npgsql and the distributed-cache package use the same client boundaries as the application; `Microsoft.Extensions.DependencyInjection` builds the test-owned cache service provider. Do not add VSTest SDK, adapter, or collector packages to this MTP project.

## Own both containers in one asynchronous fixture

Start independent containers concurrently, create clients only after readiness, and initialize only test-owned schema:

```csharp
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

public sealed class InfrastructureFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.1-alpine").Build();

    private readonly RedisContainer _redis =
        new RedisBuilder("redis:8.0.2-alpine").Build();

    private ServiceProvider? _services;

    public NpgsqlDataSource Database { get; private set; } = null!;
    public IDistributedCache Cache { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync());

        Database = new NpgsqlDataSourceBuilder(
            _postgres.GetConnectionString()).Build();

        var services = new ServiceCollection();
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = _redis.GetConnectionString());
        _services = services.BuildServiceProvider();
        Cache = _services.GetRequiredService<IDistributedCache>();

        await using var command = Database.CreateCommand(
            """
            CREATE TABLE IF NOT EXISTS test_orders (
                id uuid PRIMARY KEY,
                status text NOT NULL
            )
            """);
        await command.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Database is not null)
        {
            await Database.DisposeAsync();
        }

        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask());
    }
}
```

xUnit creates the class fixture before its dependent test class and awaits its async lifecycle. Testcontainers waits for each module's readiness and maps random host ports, so the connection strings are valid only after `StartAsync`. Starting the two independent resources concurrently reduces fixture latency without sharing mutable initialization state.

The image tags are explicit examples, not a promise that they match every application's production versions. Select reviewed images aligned with the supported production major/minor, prefer immutable digests where the delivery platform permits them, and update them deliberately. Docker daemon access is privileged; never mount source secrets, production data, or broad host paths into these containers.

The fixture shares state across its test class, so every test still needs unique data and cleanup. For a larger suite, apply application migrations rather than maintaining parallel hand-written schema. Keep migrations and reset behavior observable so a container that starts successfully cannot hide a schema failure.

## Verify one cross-resource contract with isolated data

Use a unique operation key, parameterized SQL, an explicit cache TTL, and cleanup in `finally`:

```csharp
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using Xunit;

[Trait("category", "integration")]
public sealed class StorageContractTests(
    InfrastructureFixture infrastructure)
    : IClassFixture<InfrastructureFixture>
{
    [Fact]
    public async Task Order_status_round_trips_through_postgres_and_redis()
    {
        var orderId = Guid.NewGuid();
        var cacheKey = $"test:order-status:{orderId:N}";
        var cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            await using (var insert = infrastructure.Database.CreateCommand(
                "INSERT INTO test_orders (id, status) VALUES ($1, $2)"))
            {
                insert.Parameters.AddWithValue(orderId);
                insert.Parameters.AddWithValue("accepted");
                Assert.Equal(
                    1,
                    await insert.ExecuteNonQueryAsync(cancellationToken));
            }

            await infrastructure.Cache.SetStringAsync(
                cacheKey,
                "accepted",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                },
                cancellationToken);

            await using var select = infrastructure.Database.CreateCommand(
                "SELECT status FROM test_orders WHERE id = $1");
            select.Parameters.AddWithValue(orderId);

            Assert.Equal(
                "accepted",
                await select.ExecuteScalarAsync(cancellationToken));
            Assert.Equal(
                "accepted",
                await infrastructure.Cache.GetStringAsync(
                    cacheKey,
                    cancellationToken));
        }
        finally
        {
            await infrastructure.Cache.RemoveAsync(cacheKey, cancellationToken);

            await using var delete = infrastructure.Database.CreateCommand(
                "DELETE FROM test_orders WHERE id = $1");
            delete.Parameters.AddWithValue(orderId);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
```

The test asserts observable behavior at both client boundaries rather than Testcontainers internals. A GUID primary key and cache namespace prevent collisions during parallel execution; parameterized SQL prevents test values from changing command structure. The explicit TTL limits residue if cleanup is interrupted, while `finally` provides prompt cleanup on assertion failure. Do not use sleep-based expiry assertions: if TTL behavior is under test, poll with a bounded deadline and tolerate CI scheduling jitter.

This is not an atomic dual-write. If the application requires consistency across PostgreSQL and Redis, test the actual outbox/cache-invalidation or reconciliation design, including failure between the two writes. Never infer a distributed transaction from a successful happy-path test.

## CI gate and failure classification

Run the project only in a labeled Docker-capable integration job:

```bash
dotnet test \
  -- --filter-query /[category=integration]
```

Keep container tests independently selectable from unit tests and verify the expected discovery count. Bound image-pull, fixture-start, test, and job deadlines at CI level. Distinguish daemon/socket, registry, pull, disk, port, and readiness failures from application assertions; attach only bounded and redacted diagnostics. A missing Docker daemon should fail the integration job clearly, not silently skip the suite.

## Failure modes and operations

| Symptom | Likely boundary | Observation and safe response |
| --- | --- | --- |
| Cannot connect to Docker | CI/daemon authorization | Verify the labeled worker, socket/remote endpoint, least-privilege access, and Testcontainers diagnostics. Do not fall back to shared infrastructure. |
| Image pull fails or is slow | Registry/network/cache | Record image/tag and registry error, use approved pre-pull/cache policy, and keep a bounded startup deadline. Do not switch to `latest`. |
| Container starts but client fails | Readiness/configuration | Use only the post-start connection string, inspect bounded container logs, and verify TLS/auth/client options. Avoid hard-coded host ports. |
| Tests pass alone but fail together | Shared state | Audit fixture scope, database rows, transactions, Redis prefixes, cleanup, and xUnit parallelism. Restore deterministic isolation rather than ordering tests. |
| Teardown leaves resources | Cancellation/worker termination | Ensure async disposal is awaited and let the Testcontainers resource reaper clean abnormal exits; monitor worker disk/container growth. |
| TTL test flakes | Wall-clock scheduling | Replace fixed sleep with deadline-bounded polling and assert a time window appropriate to Redis semantics and CI variance. |

Observe fixture startup/teardown duration, image-pull duration, container readiness, test duration, expected/discovered test counts, and cleanup failures. Redact database/cache connection strings, passwords, keys, values, SQL parameters, container environment, registry credentials, and Docker endpoint credentials from logs and artifacts. Keep Docker-backed jobs isolated from untrusted code where daemon access could escape the build workspace.

## Verification checklist

Authoring evidence:

- [x] The fixture and test sample compiled as a temporary `net10.0` xUnit v3/MTP test project with the catalog's pinned packages.
- [x] Docker was intentionally not required during authoring; containers, client round trips, readiness, cleanup, and the test itself were not run.

Consuming-application checks:

- [ ] Run from a clean Docker-capable worker and confirm both reviewed image pins/digests, dynamic ports, readiness, and async cleanup.
- [ ] Apply the application's real migrations and reset strategy, then repeat and parallelize the suite to prove isolation.
- [ ] Verify PostgreSQL transactions/provider behavior and Redis serialization/TTL/atomic behavior relevant to the application.
- [ ] Exercise daemon unavailable, image pull denied, readiness timeout, PostgreSQL unavailable, Redis unavailable, cancellation, and teardown failure paths.
- [ ] Enforce bounded job/resource limits, expected test discovery counts, redacted diagnostics, and no access to production secrets or data.
- [ ] Keep unit and Docker integration suites independently runnable without silently skipping either gate.

## Related guides

- [Testcontainers.PostgreSql](../packages/testcontainers-postgresql.md)
- [Testcontainers.Redis](../packages/testcontainers-redis.md)
- [xunit.v3](../packages/xunit-v3.md)
- [Npgsql](../packages/npgsql.md)
- [Microsoft.Extensions.Caching.StackExchangeRedis](../packages/microsoft-extensions-caching-stackexchangeredis.md)
- [Microsoft.Extensions.DependencyInjection](../packages/microsoft-extensions-dependencyinjection.md)
- [Relational test fidelity](../package-guidance/package-selection.md#relational-test-fidelity)

## Primary sources

Accessed 2026-07-27.

- [Testcontainers for .NET PostgreSQL module](https://dotnet.testcontainers.org/modules/postgres/)
- [Testcontainers for .NET Redis module](https://dotnet.testcontainers.org/modules/redis/)
- [Testcontainers for .NET xUnit guidance](https://dotnet.testcontainers.org/test_frameworks/xunit_net/)
- [Testcontainers for .NET CI guidance](https://dotnet.testcontainers.org/cicd/)
- [xUnit v3 shared-context guidance](https://xunit.net/docs/shared-context)
- [xUnit v3 Microsoft Testing Platform guidance](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [Testcontainers.PostgreSql 4.13.0 on NuGet](https://www.nuget.org/packages/Testcontainers.PostgreSql/4.13.0)
- [Testcontainers.Redis 4.13.0 on NuGet](https://www.nuget.org/packages/Testcontainers.Redis/4.13.0)
- [xunit.v3 3.2.2 on NuGet](https://www.nuget.org/packages/xunit.v3/3.2.2)
