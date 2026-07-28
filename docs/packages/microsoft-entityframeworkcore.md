# Microsoft.EntityFrameworkCore

> **Owner:** `IX` · **Last reviewed:** `2026-07-27` · **Review trigger:** EF Core, target-framework, Npgsql provider, or migration/deployment-policy change.

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Microsoft.EntityFrameworkCore` | `10.0.10` | Core EF Core ORM/runtime surface | Direct; cataloged for the repository's `net10.0` target framework; no consuming project exists |

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

### Upgrade and rollback

Move `Microsoft.EntityFrameworkCore`, `Relational`, `Design`, `dotnet-ef`, the Npgsql provider, conventions, and exception processor as a validated set; do not mix EF patch/major lines casually. Review EF and provider breaking changes, regenerate a migration from an unchanged model to detect metadata drift, compile queries, and run PostgreSQL migrations, transactions, concurrency, and SQL-shape tests. Deploy compatible schema changes before code that requires them. Rollback means redeploying the prior application/package set and using the migration's rehearsed down, forward-fix, or restore path; restoring package pins alone cannot undo applied DDL or data transformations.

## Integration with the catalog

Relational APIs are [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md); tooling is [Microsoft.EntityFrameworkCore.Design](microsoft-entityframeworkcore-design.md); test-only fake storage is [Microsoft.EntityFrameworkCore.InMemory](microsoft-entityframeworkcore-inmemory.md). Query abstractions belong in [Ardalis.Specification](ardalis-specification.md). Use [relational test fidelity](../package-guidance/package-selection.md#relational-test-fidelity) and [PostgreSQL data-access selection](../package-guidance/package-selection.md#postgresql-data-access) to choose the real-provider boundary; see the [EF Core/PostgreSQL recipe](../recipes/efcore-npgsql-exception-mapping.md) and [supply-chain entry](../package-guidance/supply-chain.md#microsoft-entityframeworkcore).

## Security, performance, AOT, trimming, and operations

Parameterize values through LINQ/EF APIs; use parameterized overloads for raw SQL, never string concatenation. Keep connection strings in secret configuration, redact provider details, separate runtime and DDL identities, and enforce authorization/tenant predicates before materialization.

Avoid lazy-loading-driven N+1 queries and unbounded loading. Measure query count, duration, rows, allocation, and plans; add indexes based on observed filter/order shapes. Use `AsSplitQuery` only after measuring cartesian explosion. Reuse contexts only through supported DI/pooling patterns and clear state between pooled requests.

EF's NativeAOT and query-precompilation support is documented as experimental and not suited to production. Dynamic queries and some value converters are unsupported, generated code can be large, and provider support is required. Treat every trimming/AOT warning as actionable and verify a published artifact with the actual Npgsql provider and workload.

Operationally, use EF's `Microsoft.EntityFrameworkCore.*` log categories, `DiagnosticSource` events, interceptors, and Npgsql telemetry to observe query duration/count, save failures, optimistic-concurrency conflicts, retries, and transaction outcomes. Keep `EnableSensitiveDataLogging()` and detailed provider errors disabled in production unless an approved, time-bounded diagnostic procedure protects the output.

For repeated timeouts, correlate EF command logs/spans with Npgsql pool and server signals, inspect generated SQL/plans, and distinguish pool wait, lock wait, network failure, and slow execution before tuning. A context-concurrency error indicates overlapping operations on one `DbContext`; await the first operation or create independent contexts rather than retrying it.

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

- [Central package catalog](../../Directory.Packages.props) — Accessed 2026-07-27.
- [Microsoft.EntityFrameworkCore 10.0.10 on NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.10) — Accessed 2026-07-27.
- [EF Core overview](https://learn.microsoft.com/ef/core/) — Accessed 2026-07-27.
- [DbContext lifetime, configuration, and initialization](https://learn.microsoft.com/ef/core/dbcontext-configuration/) — Accessed 2026-07-27.
- [Tracking and no-tracking queries](https://learn.microsoft.com/ef/core/querying/tracking) — Accessed 2026-07-27.
- [Applying EF Core migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying) — Accessed 2026-07-27.
- [EF Core connection resiliency](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency) — Accessed 2026-07-27.
- [EF Core performance guidance](https://learn.microsoft.com/ef/core/performance/) — Accessed 2026-07-27.
- [EF Core logging, events, and diagnostics](https://learn.microsoft.com/ef/core/logging-events-diagnostics/) — Accessed 2026-07-27.
- [EF Core simple logging and sensitive-data warning](https://learn.microsoft.com/ef/core/logging-events-diagnostics/simple-logging) — Accessed 2026-07-27.
- [EF NativeAOT and precompiled queries](https://learn.microsoft.com/ef/core/performance/nativeaot-and-precompiled-queries) — Accessed 2026-07-27.
