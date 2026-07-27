# EFCore.NamingConventions

> **Owner:** `IX` · **Last reviewed:** `2026-07-27` · **Review trigger:** Package, EF Core/Npgsql provider, target-framework, or production schema naming-policy change.

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `EFCore.NamingConventions` | `10.0.1` | Convention-based database identifier naming, including snake case | Cataloged; no provider/migration integration compiled |

## Decision and scope

Select one database naming convention before the first production migration. This package configures model naming; it does not automatically make an existing schema safe to rename.

## Recommended registration and use

Reference the centrally pinned package and configure the convention on the same options builder as Npgsql:

```xml
<ItemGroup>
  <PackageReference Include="EFCore.NamingConventions" />
</ItemGroup>
```

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppDatabase"))
        .UseSnakeCaseNamingConvention());
```

The package also exposes lower-case, upper-case, camel-case, and upper-snake-case conventions. Choose exactly one project-wide convention before the first migration; explicit Fluent API names remain appropriate where a legacy or externally managed schema requires them.

After configuring it, generate and inspect a migration:

```bash
dotnet ef migrations add ApplySnakeCase --project src/Infrastructure --startup-project src/Api
dotnet ef migrations script 0 ApplySnakeCase --project src/Infrastructure --startup-project src/Api
```

Review table, column, primary/foreign key, index, sequence, and join-table identifiers in both the migration and generated SQL.

## Enterprise implementation guidance

Treat conversion of an established schema as a reviewed data-migration program. The upstream project warns that enabling a convention on an existing database can generate renames for every object and may drop/recreate primary keys.

Inventory dependent views, functions, triggers, policies, quoted SQL, reporting jobs, and external consumers. Test rename SQL and rollback/recovery from a production-like copy, estimate locks, and deploy with a DDL identity rather than the application identity. For a new database, capture the convention in the initial model snapshot so every environment starts consistently.

### Upgrade and rollback

Upgrade `EFCore.NamingConventions` with the corresponding EF Core major line and regenerate a throwaway migration before accepting the change. Compare the model snapshot and every identifier operation; a package or convention change can appear as broad schema renames even when entity classes are unchanged. For a new schema, rollback is an application/package revert plus regeneration of undeployed migrations. For an already migrated schema, use a reviewed down/forward-fix migration or restore plan—never assume reverting the package reverses database identifiers.

## Integration with the catalog

Use with [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md), [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md), [Microsoft.EntityFrameworkCore.Design](microsoft-entityframeworkcore-design.md), and the cataloged Npgsql provider. Specifications must not assume names outside generated SQL/migrations. See [PostgreSQL data-access selection](../package-guidance/package-selection.md#postgresql-data-access), the [EF Core/PostgreSQL recipe](../recipes/efcore-npgsql-exception-mapping.md), and the [supply-chain entry](../package-guidance/supply-chain.md#efcore-namingconventions).

## Security, performance, AOT, trimming, and operations

Naming does not change authorization, row-level security, or parameterization requirements. Identifier renames can break security policies and operational SQL even when EF-generated queries work. A production rollout can lock or rebuild objects; capture reviewed SQL, dependency inventory, duration, backup/restore evidence, and schema-drift checks.

The convention participates in model construction, so compiled models and migration snapshots must be regenerated after a change. AOT/trimming compatibility remains unverified until the exact provider/convention combination is published and exercised.

## Avoid

- Do not switch conventions as a formatting cleanup on a live schema.
- Do not assume every provider object or extension is transformed identically.
- Do not apply generated rename operations without production-like testing.

## Verification checklist

- [ ] Compile a consuming EF Core/Npgsql project with the exact 10.0.1 pin.
- [ ] Generate a throwaway migration and review every identifier/schema operation.
- [ ] Search application SQL, views, functions, policies, dashboards, and jobs for old quoted identifiers.
- [ ] Test upgrade, downgrade/recovery, views/functions, and PostgreSQL integration.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [EFCore.NamingConventions 10.0.1 on NuGet](https://www.nuget.org/packages/EFCore.NamingConventions/10.0.1)
- [EFCore.NamingConventions repository](https://github.com/efcore/EFCore.NamingConventions)
- [EFCore.NamingConventions setup and existing-database warning](https://github.com/efcore/EFCore.NamingConventions#usage)
- [Npgsql naming guidance](https://www.npgsql.org/efcore/modeling/tables.html)
- [EF Core migration management](https://learn.microsoft.com/ef/core/managing-schemas/migrations/managing)
