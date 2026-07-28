# Testcontainers.PostgreSql

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `Testcontainers.PostgreSql` |
| Pinned version | `4.13.0` |
| Status | Direct; approved only for test projects that require disposable PostgreSQL infrastructure |
| Role | Disposable PostgreSQL containers for integration tests |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | Testcontainers/module version, PostgreSQL image, Docker/runtime, target-framework, or CI infrastructure change |

## Decision and scope

Use when an integration test must exercise real PostgreSQL protocol, SQL, transactions, extensions, or provider behavior that a test double cannot represent. It provisions test infrastructure only; it is not a local-development database manager or a production provisioning mechanism.

## Recommended registration and use

Add versionless references only to a non-packable test project:

```xml
<PropertyGroup>
  <IsTestProject>true</IsTestProject>
  <IsPackable>false</IsPackable>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="xunit.v3" />
  <PackageReference Include="Testcontainers.PostgreSql" />
</ItemGroup>
```

Own the container through xUnit's asynchronous lifecycle, pin the image, and obtain the dynamically allocated connection string only after startup:

```csharp
using Testcontainers.PostgreSql;
using Xunit;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18.1-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() =>
        await _container.StartAsync();

    public async ValueTask DisposeAsync() =>
        await _container.DisposeAsync();
}
```

Apply application-owned migrations or a minimal schema after `StartAsync`, then open a fresh client connection per test/operation. The image above is an example version tag: select one supported by the application, validate it, and update it deliberately. Tags can be moved by registries; use an approved digest reference when immutable image identity is required.

## Enterprise implementation guidance

Choose isolation deliberately: one container per test is strongest but slowest; a class/collection fixture is efficient only when every test can restore a known database state. Prefer migrations plus a reset mechanism over production dumps. Do not bind a fixed host port; Testcontainers supplies a random mapped port to permit parallel CI jobs.

Put Docker-backed tests in a labeled integration job, bound job and startup timeouts, pre-pull approved images where appropriate, and attach redacted container logs only on failure. Separate image-pull failures from application assertions so infrastructure failures remain diagnosable.

Use environment overrides only where the CI runtime requires them:

| Setting | Purpose | Upstream default | CI guidance | Sensitivity and failure behavior |
| --- | --- | --- | --- | --- |
| `DOCKER_HOST` | Select the container runtime endpoint | Auto-discovered | Set only for the approved remote/socket topology | Endpoint and transport details may be sensitive; an invalid value prevents startup |
| `TESTCONTAINERS_HOST_OVERRIDE` | Override the host exposing mapped ports | Auto-discovered | Use only for nested/remote container runners | A wrong host causes connection failures after a successful container start |
| `TESTCONTAINERS_RYUK_DISABLED` | Disable resource cleanup | `false` | Keep `false`; ephemeral runners need an independently proven cleanup mechanism before an exception | Disabling it can leak containers, networks, and volumes |
| `TESTCONTAINERS_WAIT_STRATEGY_TIMEOUT` | Bound readiness waits | `01:00:00` | Set a finite job-appropriate deadline and retain startup logs | Too short creates infrastructure flakes; too long hides failed readiness |

Values are read when the Testcontainers client is created; change them between test processes, not during a running suite. Never place registry credentials or Docker authentication JSON in committed configuration.

### Upgrade and rollback

Upgrade the PostgreSQL and Redis modules together when both are used, because each resolves the same Testcontainers core version. Keep the package upgrade separate from the database image upgrade so regressions have one owner. Validate image pull, Ryuk cleanup, readiness, migrations, connection creation, reset, repeated/parallel execution, and redacted failure output in the Docker CI job. Roll back the central package pins and image pin independently according to the failing layer; remove any leaked resources through the approved CI cleanup path.

## Integration with the catalog

Use with [xunit.v3](xunit-v3.md) and the catalog's `Npgsql` or EF Core PostgreSQL packages when testing the real provider. Use [AwesomeAssertions](awesomeassertions.md) for result assertions. Redis lifecycle guidance is separate in [Testcontainers.Redis](testcontainers-redis.md). Central package management supplies the version; project files do not. See [relational test fidelity](../package-guidance/package-selection.md#relational-test-fidelity), the [PostgreSQL and Redis Testcontainers recipe](../recipes/testcontainers-postgresql-redis-xunit.md), and the [Testcontainers.PostgreSql supply-chain entry](../package-guidance/supply-chain.md#testcontainers-postgresql).

## Security, performance, AOT, trimming, and operations

Docker socket/daemon access is privileged. Restrict it to the integration job; do not mount host secrets, broad directories, or production data. Use ephemeral test credentials and redact connection strings and SQL values from logs. Pulling and starting PostgreSQL dominates test duration, so share only under an enforced reset contract. This package has no production trimming or NativeAOT role.

For `DockerApiException`, connection, pull, or timeout failures, record the Docker endpoint/context, daemon reachability, image reference/digest, container state and exit code, wait-strategy result, and bounded container/Ryuk logs. An image-pull failure is safe to retry only after registry/network health is restored; a readiness timeout requires inspecting PostgreSQL logs and resource pressure before retrying; a migrated-schema assertion failure is an application failure and must not be hidden by infrastructure retries. If resources remain after cancellation, verify Ryuk reachability and CI cleanup instead of disabling the reaper.

## Avoid

- Do not point tests at shared, staging, or production databases.
- Do not use `latest`, hard-coded host ports, or unreviewed images.
- Do not make stateful tests order-dependent or parallel without isolation.
- Do not enable Testcontainers resource reuse in CI without a separately reviewed cleanup and trust model.

## Verification checklist

- [ ] The test starts a pinned PostgreSQL image and reads its connection string only after startup.
- [ ] Migrations/schema initialization and state reset work from a clean Docker environment.
- [ ] Repeated and parallel runs cannot observe another test's state.
- [ ] The Docker-enabled CI job has bounded privileges, timeouts, cleanup, and redacted failure diagnostics.

## Sources

- [Testcontainers for .NET PostgreSQL module](https://dotnet.testcontainers.org/modules/postgres/)
- [Testcontainers for .NET xUnit guidance](https://dotnet.testcontainers.org/test_frameworks/xunit_net/)
- [Testcontainers CI guidance](https://dotnet.testcontainers.org/cicd/)
- [Testcontainers custom configuration](https://dotnet.testcontainers.org/custom_configuration/)
- [Testcontainers resource reaper](https://dotnet.testcontainers.org/api/resource_reaper/)
- [Testcontainers.PostgreSql 4.13.0 on NuGet](https://www.nuget.org/packages/Testcontainers.PostgreSql/4.13.0)

Accessed 2026-07-27. Context7 was consulted first.
