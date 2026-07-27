# Microsoft.EntityFrameworkCore.Design

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.10` | Design-time APIs for EF tooling, scaffolding, and migrations | Cataloged; no migrations/design project exists |

## Decision and scope

Use this package in the project that runs EF tooling. It enables design-time operations; it is not a normal application runtime dependency or migration deployment strategy.

## Recommended registration and use

- Keep the asset private (`PrivateAssets="all"` when referenced) so tooling dependencies do not flow to package consumers.
- Keep migration source and snapshots in source control, and make startup/target project selection deterministic in CI.

## Enterprise implementation guidance

Use a design-time factory when startup composition cannot safely create the context. Read secure configuration without logging secrets. Review generated migrations and apply production DDL with a narrowly privileged deployment identity; keep normal application identities free of DDL permission.

## Integration with the catalog

This package supports [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md) and [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md). Coordinate naming changes with [EFCore.NamingConventions](efcore-namingconventions.md) and test generated PostgreSQL SQL through the Npgsql provider.

## Security, performance, AOT, trimming, and operations

`dotnet ef` creates a DbContext and can execute startup code. Keep that path deterministic and non-destructive: it must not migrate databases, seed production data, send messages, or contact unrelated services. Design-time assets should remain private; AOT/trimming claims require an application publish test.

## Avoid

- Do not publish design-time assets as transitive runtime dependencies.
- Do not embed connection strings/secrets in migrations or design-time factories.
- Do not use tool execution as permission to mutate production state.

## Verification checklist

- [ ] Restore/compile with the exact 10.0.10 package and run `dotnet ef` against a disposable context.
- [ ] Generate, review, apply, and recover a throwaway migration against PostgreSQL.
- [ ] Verify CI specifies the intended target/startup project and redacts configuration values.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [Microsoft.EntityFrameworkCore.Design 10.0.10 on NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/10.0.10)
- [EF Core design-time tools architecture](https://learn.microsoft.com/ef/core/miscellaneous/internals/tools)
- [EF Core migrations overview](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
