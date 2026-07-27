# Microsoft.EntityFrameworkCore.InMemory

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Microsoft.EntityFrameworkCore.InMemory` | `10.0.10` | Non-relational in-process EF Core provider | Cataloged; test-only by repository build policy |

## Decision and scope

Use only for intentionally narrow tests whose non-relational semantics are acceptable. It is not a PostgreSQL substitute and is not for production.

## Recommended registration and use

- Use it for isolated behavior where neither SQL translation nor relational semantics is under test.
- Use disposable PostgreSQL integration tests for provider behavior, migrations, constraints, transactions, raw SQL, and exception mapping.

## Enterprise implementation guidance

Keep package references in test projects; `Directory.Build.targets` enforces that policy. Label tests that use it so their limitations are visible, and retain a PostgreSQL integration-test layer for persistence contracts.

## Integration with the catalog

Production data access uses [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md), [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md), and Npgsql. Do not use InMemory to validate [exception mapping](entityframeworkcore-exceptions-postgresql.md) or [keyset pagination](mr-entityframeworkcore-keysetpagination.md).

## Security, performance, AOT, trimming, and operations

The provider is not designed for production robustness or performance. It cannot provide operational, query-plan, or PostgreSQL security evidence. AOT/trimming remains application-specific and unverified.

## Avoid

- Do not use it in non-test projects or production.
- Do not accept its success as proof of relational query translation or transactions.
- Do not test raw SQL, migrations, PostgreSQL constraints, or provider exceptions with it.

## Verification checklist

- [ ] Confirm every reference is in a project with `IXModularityProjectRole=Test` or `ArchitectureTest` and `IsTestProject=true`.
- [ ] Identify tests whose result depends on non-relational behavior.
- [ ] Duplicate relational/provider assertions against disposable PostgreSQL.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [Microsoft.EntityFrameworkCore.InMemory 10.0.10 on NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.InMemory/10.0.10)
- [EF Core InMemory provider guidance](https://learn.microsoft.com/ef/core/providers/in-memory/)
- [EF Core testing strategy](https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy)
