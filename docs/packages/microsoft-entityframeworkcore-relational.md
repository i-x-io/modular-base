# Microsoft.EntityFrameworkCore.Relational

> **Owner:** `IX` · **Last reviewed:** `2026-07-27` · **Review trigger:** EF relational API, Npgsql provider, target-framework, or migration-policy change.

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Microsoft.EntityFrameworkCore.Relational` | `10.0.10` | Shared relational EF Core APIs used by database providers and relational features | Cataloged; no provider integration compiled |

## Decision and scope

Use the relational layer as part of the EF Core/Npgsql stack. It is not itself a PostgreSQL provider and does not establish a schema, connection policy, or migration process.

## Recommended registration and use

Reference it only where shared relational APIs are used directly; an EF relational provider normally brings it transitively:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />
</ItemGroup>
```

Let Npgsql own PostgreSQL-specific translation and behavior. This package contributes shared APIs such as relational transactions, migrations, `ExecuteUpdateAsync`/`ExecuteDeleteAsync`, SQL query methods, and generated-SQL inspection. For a set-based update:

```csharp
var affected = await db.Orders
    .Where(order => order.Status == OrderStatus.Pending && order.ExpiresAt < clock.UtcNow)
    .ExecuteUpdateAsync(
        setters => setters.SetProperty(order => order.Status, OrderStatus.Expired),
        cancellationToken);
```

Set-based operations execute immediately and bypass change tracking; validate authorization predicates, check the affected-row count, and reconcile any already-tracked entities. Use interpolated/parameterized relational SQL APIs for values and never build SQL from untrusted text.

## Enterprise implementation guidance

Upgrade this package with the EF runtime, design package, Npgsql provider, conventions, and exception mapper. Test migrations, transactions, constraints, execution plans, and retry/error behavior against PostgreSQL.

For explicit transactions under a retrying execution strategy, run the whole transaction as the strategy's delegate:

```csharp
var strategy = db.Database.CreateExecutionStrategy();

await strategy.ExecuteAsync(async () =>
{
    await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
    await db.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
});
```

The delegate may be replayed, so keep it deterministic and coordinate non-database side effects using an idempotent/outbox workflow. Keep transactions short, define an isolation level only with a documented consistency requirement, and test serialization/deadlock handling with the actual provider.

### Upgrade and rollback

Upgrade `Relational` with the exact EF runtime/design line and a compatible Npgsql provider. Review breaking changes in SQL generation, migrations, transactions, batching, and set-based operations; generate a no-model-change migration and compare critical SQL/plans before promotion. Roll back the whole EF/provider application set. Any deployed relational migration or data rewrite needs its own reviewed down, forward-fix, or restore path—package rollback does not revert schema state.

## Integration with the catalog

The runtime is [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md); design tooling is [Microsoft.EntityFrameworkCore.Design](microsoft-entityframeworkcore-design.md). Naming and provider exception behavior are documented in [EFCore.NamingConventions](efcore-namingconventions.md) and [EntityFrameworkCore.Exceptions.PostgreSQL](entityframeworkcore-exceptions-postgresql.md). See [relational test fidelity](../package-guidance/package-selection.md#relational-test-fidelity), [PostgreSQL data-access selection](../package-guidance/package-selection.md#postgresql-data-access), the [EF Core/PostgreSQL recipe](../recipes/efcore-npgsql-exception-mapping.md), and the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-entityframeworkcore-relational).

## Security, performance, AOT, trimming, and operations

Use least-privilege database roles, parameterized APIs, projections, bounded loading, cancellation, statement/lock timeouts, and plan review. Set-based update/delete calls can affect many rows quickly, so require restrictive predicates, audit sensitive operations, and alert on unexpected affected-row counts.

Generated SQL is a diagnostic artifact and can contain schema or parameter detail; protect it according to log policy. Relational package metadata alone cannot prove trimming, NativeAOT, transaction semantics, migration safety, or provider correctness; publish and exercise the exact Npgsql application.

## Avoid

- Do not use the relational package as a replacement for Npgsql.
- Do not test PostgreSQL behavior only with the InMemory provider.
- Do not infer stable SQL or migration behavior without executing the provider integration.
- Do not assume tracked entities reflect `ExecuteUpdate`/`ExecuteDelete` results without reload or context-boundary handling.

## Verification checklist

- [ ] Restore and compile the exact 10.0.10 package with EF Core and Npgsql.
- [ ] Run PostgreSQL integration tests for relational translations, migrations, transactions, and constraints.
- [ ] Test set-based writes, affected-row guards, cancellation, retries, and tracked-entity reconciliation.
- [ ] Review SQL/plans and migration output for critical paths.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [Microsoft.EntityFrameworkCore.Relational 10.0.10 on NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Relational/10.0.10)
- [EF Core relational data documentation](https://learn.microsoft.com/ef/core/modeling/relationships)
- [EF Core SQL queries and parameterization](https://learn.microsoft.com/ef/core/querying/sql-queries)
- [EF Core transactions](https://learn.microsoft.com/ef/core/saving/transactions)
- [EF Core execute update and delete](https://learn.microsoft.com/ef/core/saving/execute-insert-update-delete)
- [EF Core migrations overview](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
