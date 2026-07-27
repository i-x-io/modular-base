# Microsoft.EntityFrameworkCore

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Microsoft.EntityFrameworkCore` | `10.0.10` | Core EF Core ORM/runtime surface | Cataloged; repository target framework is `net10.0`; no consuming project exists |

## Decision and scope

Use EF Core 10 as the relational persistence runtime with Npgsql as the PostgreSQL provider. This does not choose aggregate boundaries, repository shape, database schema, or migration deployment policy.

## Recommended registration and use

Reference the centrally pinned runtime; the Npgsql provider supplies `UseNpgsql`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
</ItemGroup>
```

Register one `DbContext` type in the application composition root and keep its options in DI:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppDatabase")));

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}
```

`AddDbContext` registers a scoped context by default, which fits one unit of work per request. A `DbContext` is not thread-safe: await every EF operation and never share an instance between concurrent tasks. For work outside a request scope or parallel units of work, inject `IDbContextFactory<AppDbContext>` and dispose each created context.

Keep filtering, ordering, and pagination in provider-translated LINQ until execution. Project and bound read paths, use no tracking when entities will not be updated, and pass cancellation:

```csharp
var orders = await db.Orders
    .AsNoTracking()
    .Where(order => order.CustomerId == customerId)
    .OrderByDescending(order => order.CreatedAt)
    .ThenByDescending(order => order.Id)
    .Take(100)
    .Select(order => new OrderSummary(order.Id, order.Number, order.Total))
    .ToListAsync(cancellationToken);
```

## Enterprise implementation guidance

Pin EF runtime, relational, design, Npgsql, conventions, and exception assets together. A common delivery workflow is:

1. Model mappings and constraints explicitly; create a reviewed migration and snapshot.
2. Run query, constraint, concurrency, transaction, and migration tests against disposable PostgreSQL.
3. Check `dotnet ef migrations has-pending-model-changes` in CI.
4. Produce a reviewed SQL script or migration bundle as a deployment artifact.
5. Apply it with a controlled, auditable deployment identity before rolling out dependent code.

Use optimistic concurrency tokens where conflicting updates matter. Keep transaction scopes short. When a retrying execution strategy and explicit transaction are both required, execute the entire transaction delegate through `Database.CreateExecutionStrategy()` so it can replay as one unit; ensure external side effects are idempotent or occur after commit.

## Integration with the catalog

Relational APIs are [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md); tooling is [Microsoft.EntityFrameworkCore.Design](microsoft-entityframeworkcore-design.md); test-only fake storage is [Microsoft.EntityFrameworkCore.InMemory](microsoft-entityframeworkcore-inmemory.md). Query abstractions belong in [Ardalis.Specification](ardalis-specification.md).

## Security, performance, AOT, trimming, and operations

Parameterize values through LINQ/EF APIs; use parameterized overloads for raw SQL, never string concatenation. Keep connection strings in secret configuration, redact provider details, separate runtime and DDL identities, and enforce authorization/tenant predicates before materialization.

Avoid lazy-loading-driven N+1 queries and unbounded loading. Measure query count, duration, rows, allocation, and plans; add indexes based on observed filter/order shapes. Use `AsSplitQuery` only after measuring cartesian explosion. Reuse contexts only through supported DI/pooling patterns and clear state between pooled requests.

EF's NativeAOT and query-precompilation support is documented as experimental and not suited to production. Dynamic queries and some value converters are unsupported, generated code can be large, and provider support is required. Treat every trimming/AOT warning as actionable and verify a published artifact with the actual Npgsql provider and workload.

## Avoid

- Do not expose DbContext/EF entities as public API contracts.
- Do not concatenate untrusted raw SQL.
- Do not run production migrations at startup with a broadly privileged application identity.
- Do not use one `DbContext` concurrently or continue using it after an unrecoverable `InvalidOperationException`.
- Do not return an `IQueryable` across an application or API boundary.

## Verification checklist

- [ ] Restore and compile the consuming `net10.0` project with 10.0.10.
- [ ] Verify context lifetime, disposal, cancellation, concurrency conflicts, and transaction rollback.
- [ ] Run PostgreSQL integration tests for queries, constraints, transactions, and migrations.
- [ ] Inspect SQL/plans for critical query paths and publish-test any AOT/trimming proposal.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [Microsoft.EntityFrameworkCore 10.0.10 on NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.10)
- [EF Core overview](https://learn.microsoft.com/ef/core/)
- [DbContext lifetime, configuration, and initialization](https://learn.microsoft.com/ef/core/dbcontext-configuration/)
- [Tracking and no-tracking queries](https://learn.microsoft.com/ef/core/querying/tracking)
- [Applying EF Core migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying)
- [EF Core connection resiliency](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency)
- [EF Core performance guidance](https://learn.microsoft.com/ef/core/performance/)
- [EF NativeAOT and precompiled queries](https://learn.microsoft.com/ef/core/performance/nativeaot-and-precompiled-queries)
