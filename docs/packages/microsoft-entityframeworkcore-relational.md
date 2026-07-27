# Microsoft.EntityFrameworkCore.Relational

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Microsoft.EntityFrameworkCore.Relational` | `10.0.10` | Shared relational EF Core APIs used by database providers and relational features | Cataloged; no provider integration compiled |

## Decision and scope

Use the relational layer as part of the EF Core/Npgsql stack. It is not itself a PostgreSQL provider and does not establish a schema, connection policy, or migration process.

## Recommended registration and use

- Reference it only where its relational APIs are required; let the provider own PostgreSQL-specific behavior.
- Keep relational queries server-translated and use generated SQL/migrations as review artifacts.

## Enterprise implementation guidance

Upgrade this package with the EF runtime, design package, Npgsql provider, conventions, and exception mapper. Test migrations, transactions, constraints, execution plans, and retry/error behavior against PostgreSQL.

## Integration with the catalog

The runtime is [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md); design tooling is [Microsoft.EntityFrameworkCore.Design](microsoft-entityframeworkcore-design.md). Naming and provider exception behavior are documented in [EFCore.NamingConventions](efcore-namingconventions.md) and [EntityFrameworkCore.Exceptions.PostgreSQL](entityframeworkcore-exceptions-postgresql.md).

## Security, performance, AOT, trimming, and operations

Use least-privilege database roles, parameterized APIs, projections, bounded loading, cancellation, and plan review. Relational package metadata alone cannot prove AOT/trimming or provider correctness.

## Avoid

- Do not use the relational package as a replacement for Npgsql.
- Do not test PostgreSQL behavior only with the InMemory provider.
- Do not infer stable SQL or migration behavior without executing the provider integration.

## Verification checklist

- [ ] Restore and compile the exact 10.0.10 package with EF Core and Npgsql.
- [ ] Run PostgreSQL integration tests for relational translations, migrations, transactions, and constraints.
- [ ] Review SQL/plans and migration output for critical paths.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [Microsoft.EntityFrameworkCore.Relational 10.0.10 on NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Relational/10.0.10)
- [EF Core relational data documentation](https://learn.microsoft.com/ef/core/modeling/relationships)
- [EF Core migrations overview](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
