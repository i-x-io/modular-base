# EFCore.NamingConventions

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `EFCore.NamingConventions` | `10.0.1` | Convention-based database identifier naming, including snake case | Cataloged; no provider/migration integration compiled |

## Decision and scope

Select one database naming convention before the first production migration. This package configures model naming; it does not automatically make an existing schema safe to rename.

## Recommended registration and use

- Configure the convention once while building the DbContext model/options.
- Generate and review a migration to verify table, column, key, foreign-key, index, and join-table names.

## Enterprise implementation guidance

Treat conversion of an established schema as a reviewed data-migration program: inventory dependent views/functions/extensions, schedule locks, test rename SQL and recovery, and use deployment DDL permissions rather than application credentials.

## Integration with the catalog

Use with [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md), [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md), [Microsoft.EntityFrameworkCore.Design](microsoft-entityframeworkcore-design.md), and the cataloged Npgsql provider. Specifications must not assume names outside generated SQL/migrations.

## Security, performance, AOT, trimming, and operations

Naming does not change authorization or parameterization requirements. A production rollout can lock or rename large objects; capture generated SQL, backup/recovery evidence, and provider behavior. AOT/trimming compatibility is unverified.

## Avoid

- Do not switch conventions as a formatting cleanup on a live schema.
- Do not assume every provider object or extension is transformed identically.
- Do not apply generated rename operations without production-like testing.

## Verification checklist

- [ ] Compile a consuming EF Core/Npgsql project with the exact 10.0.1 pin.
- [ ] Generate a throwaway migration and review every identifier/schema operation.
- [ ] Test upgrade, downgrade/recovery, views/functions, and PostgreSQL integration.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [EFCore.NamingConventions 10.0.1 on NuGet](https://www.nuget.org/packages/EFCore.NamingConventions/10.0.1)
- [EFCore.NamingConventions repository](https://github.com/efcore/EFCore.NamingConventions)
- [Npgsql naming guidance](https://www.npgsql.org/efcore/modeling/tables.html)
