# Microsoft.EntityFrameworkCore.InMemory

> **Owner:** `IX` · **Last reviewed:** `2026-07-27` · **Review trigger:** Package, EF Core test guidance, target-framework, or PostgreSQL test-strategy change.

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Microsoft.EntityFrameworkCore.InMemory` | `10.0.10` | Non-relational in-process EF Core provider | Companion; cataloged for test-only use |

## Decision and scope

Use only for intentionally narrow tests whose non-relational semantics are acceptable. It is not a PostgreSQL substitute and is not for production.

## Recommended registration and use

Reference it only from a non-packable test project:

```xml
<PropertyGroup>
  <IsTestProject>true</IsTestProject>
  <IsPackable>false</IsPackable>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
</ItemGroup>
```

Use a unique database name per test and dispose the context:

```csharp
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase($"{nameof(OrderServiceTests)}-{Guid.NewGuid():N}")
    .Options;

await using var db = new AppDbContext(options);
var service = new OrderService(db);

await service.CreateAsync(new CreateOrder("A-100"), cancellationToken);

Assert.Single(await db.Orders.ToListAsync(cancellationToken));
```

This can support an isolated application-service test where storage is merely a test collaborator. It cannot establish that the query translates, a relational constraint fires, a transaction rolls back, or PostgreSQL behaves correctly. Use disposable PostgreSQL integration tests for provider behavior, migrations, constraints, concurrency, transactions, raw SQL, collations, and exception mapping.

## Enterprise implementation guidance

Keep package references in projects explicitly marked with `IsTestProject=true` and `IsPackable=false`. Label tests that use it so their limitations are visible, and retain a PostgreSQL integration-test layer for persistence contracts.

Avoid sharing one named InMemory store across tests because state leaks make tests order-dependent. If sharing is deliberate, control the same internal service provider and database root explicitly and reset state between tests. Prefer fakes/stubs at a narrower application port when the test does not need EF behavior at all.

### Upgrade and rollback

Upgrade InMemory with the EF Core runtime used by the test project. Re-run only tests that intentionally exercise EF state-management behavior and compare them with the PostgreSQL integration suite; changed InMemory behavior must not redefine the production contract. The provider has no durable schema or production data. Roll back by restoring the prior EF/InMemory pins and test artifact; discard the process-local test database.

## Integration with the catalog

Production data access uses [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md), [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md), and Npgsql. Do not use InMemory to validate [exception mapping](entityframeworkcore-exceptions-postgresql.md) or [keyset pagination](mr-entityframeworkcore-keysetpagination.md). The [relational test-fidelity decision](../package-guidance/package-selection.md#relational-test-fidelity) defines when PostgreSQL Testcontainers are required; see the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-entityframeworkcore-inmemory).

## Security, performance, AOT, trimming, and operations

The provider is not designed for production robustness or performance. It cannot provide SQL-injection, row-level security, collation, locking, query-plan, timeout, or PostgreSQL permission evidence. Do not use its timings for capacity decisions. AOT/trimming remains application-specific and unverified, and there is no production reason to publish this test-only package.

## Avoid

- Do not use it in non-test projects or production.
- Do not accept its success as proof of relational query translation or transactions.
- Do not test raw SQL, migrations, PostgreSQL constraints, or provider exceptions with it.
- Do not make production queries less relational merely to satisfy an InMemory test.

## Verification checklist

- [ ] Confirm every reference is in a project with `IsTestProject=true` and `IsPackable=false`.
- [ ] Identify tests whose result depends on non-relational behavior.
- [ ] Ensure test database names/state are isolated and tests remain order-independent.
- [ ] Duplicate relational/provider assertions against disposable PostgreSQL.

## Sources

- [Central package catalog](../../Directory.Packages.props) — Accessed 2026-07-27.
- [Microsoft.EntityFrameworkCore.InMemory 10.0.10 on NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.InMemory/10.0.10) — Accessed 2026-07-27.
- [EF Core InMemory provider guidance](https://learn.microsoft.com/ef/core/providers/in-memory/) — Accessed 2026-07-27.
- [EF Core testing strategy](https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy) — Accessed 2026-07-27.
- [Testing EF Core applications](https://learn.microsoft.com/ef/core/testing/) — Accessed 2026-07-27.
