# Microsoft.EntityFrameworkCore.Design

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.10` | Design-time APIs for EF tooling, scaffolding, and migrations | Cataloged; no migrations/design project exists |

## Decision and scope

Use this package in the project that runs EF tooling. It enables design-time operations; it is not a normal application runtime dependency or migration deployment strategy.

## Recommended registration and use

Keep the asset private so design-time dependencies do not flow to package consumers:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

Install `dotnet-ef` through a repository tool manifest, restore it in CI, and always specify the migration target and startup projects when they differ:

```bash
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.10
dotnet tool restore
dotnet ef migrations add AddOrders --project src/Infrastructure --startup-project src/Api
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Api
dotnet ef migrations script --idempotent --project src/Infrastructure --startup-project src/Api --output artifacts/migrations.sql
```

Keep migrations and the model snapshot in source control. For production, prefer reviewed SQL or a migration bundle produced in CI over application-startup migration.

## Enterprise implementation guidance

Use a design-time factory when startup composition cannot safely create the context or when tooling needs deterministic options:

```csharp
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("APP_MIGRATIONS_CONNECTION")
            ?? throw new InvalidOperationException("APP_MIGRATIONS_CONNECTION is required.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
```

Do not hard-code or print the connection string. Review generated operations for destructive changes and data backfills. Apply DDL with a narrowly privileged deployment identity; keep normal application identities free of DDL permission. Test forward migration and documented recovery against a production-like PostgreSQL copy before release.

## Integration with the catalog

This package supports [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md) and [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md). Coordinate naming changes with [EFCore.NamingConventions](efcore-namingconventions.md) and test generated PostgreSQL SQL through the Npgsql provider.

## Security, performance, AOT, trimming, and operations

`dotnet ef` creates a `DbContext` and can execute startup code. Keep that path deterministic and non-destructive: it must not migrate databases, seed production data, send messages, or contact unrelated services. Protect migration artifacts because generated SQL can disclose schema details; record review and deployment checksums.

Idempotent scripts consult migration history but still require review and controlled execution. Bundles make deployment self-contained but do not remove backup, locking, permission, or rollback planning. Design-time assets remain private; EF NativeAOT optimization is experimental and any generated compiled model/interceptors require application publish and runtime tests.

## Avoid

- Do not publish design-time assets as transitive runtime dependencies.
- Do not embed connection strings/secrets in migrations or design-time factories.
- Do not use tool execution as permission to mutate production state.
- Do not edit a previously deployed migration to represent a new schema change; add a new migration.

## Verification checklist

- [ ] Restore/compile with the exact 10.0.10 package and run `dotnet ef` against a disposable context.
- [ ] Run `dotnet ef migrations has-pending-model-changes` in CI with explicit project arguments.
- [ ] Generate, review, apply, and recover a throwaway migration against PostgreSQL.
- [ ] Verify CI specifies the intended target/startup project and redacts configuration values.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [Microsoft.EntityFrameworkCore.Design 10.0.10 on NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/10.0.10)
- [EF Core design-time tools architecture](https://learn.microsoft.com/ef/core/miscellaneous/internals/tools)
- [Design-time DbContext creation](https://learn.microsoft.com/ef/core/cli/dbcontext-creation)
- [EF Core migrations overview](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [Managing EF Core migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/managing)
- [Applying migrations, scripts, and bundles](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying)
