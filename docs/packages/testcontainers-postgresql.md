# Testcontainers.PostgreSql

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `Testcontainers.PostgreSql` |
| Pinned version | `4.13.0` |
| Status | Approved test-only dependency |
| Role | Disposable PostgreSQL containers for integration tests |

## Decision and scope

Use when an integration test must exercise real PostgreSQL protocol, SQL, transactions, extensions, or provider behavior that a test double cannot represent. It provisions test infrastructure only; it is not a local-development database manager or a production provisioning mechanism.

## Recommended registration and use

Add versionless references only to a `Test` or `ArchitectureTest` project with `IsTestProject=true`:

```xml
<PackageReference Include="xunit.v3" />
<PackageReference Include="Testcontainers.PostgreSql" />
```

Own the container through xUnit's asynchronous lifecycle, pin the image, and obtain the dynamically allocated connection string only after startup:

```csharp
using Testcontainers.PostgreSql;

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

Apply application-owned migrations or a minimal schema after `StartAsync`, then open a fresh client connection per test/operation. The image above is an example pin: select one supported by the application, validate it, and update it deliberately.

## Enterprise implementation guidance

Choose isolation deliberately: one container per test is strongest but slowest; a class/collection fixture is efficient only when every test can restore a known database state. Prefer migrations plus a reset mechanism over production dumps. Do not bind a fixed host port; Testcontainers supplies a random mapped port to permit parallel CI jobs.

Put Docker-backed tests in a labeled integration job, bound job and startup timeouts, pre-pull approved images where appropriate, and attach redacted container logs only on failure. Separate image-pull failures from application assertions so infrastructure failures remain diagnosable.

## Integration with the catalog

Use with [xunit.v3](xunit-v3.md) and the catalog's `Npgsql` or EF Core PostgreSQL packages when testing the real provider. Use [AwesomeAssertions](awesomeassertions.md) for result assertions. Redis lifecycle guidance is separate in [Testcontainers.Redis](testcontainers-redis.md). Central package management supplies the version; project files do not.

## Security, performance, AOT, trimming, and operations

Docker socket/daemon access is privileged. Restrict it to the integration job; do not mount host secrets, broad directories, or production data. Use ephemeral test credentials and redact connection strings and SQL values from logs. Pulling and starting PostgreSQL dominates test duration, so share only under an enforced reset contract. This package has no production trimming or NativeAOT role.

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
- [Testcontainers.PostgreSql 4.13.0 on NuGet](https://www.nuget.org/packages/Testcontainers.PostgreSql/4.13.0)

Accessed 2026-07-27. Context7 was consulted first.
