# EntityFrameworkCore.Exceptions.PostgreSQL

> **Owner:** `IX` · **Last reviewed:** `2026-07-27` · **Review trigger:** Package, EF Core, Npgsql provider, PostgreSQL error-mapping, or target-framework change.

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `EntityFrameworkCore.Exceptions.PostgreSQL` | `10.0.1` | PostgreSQL-aware classification of EF Core database update errors | Companion; no persistence error boundary compiled |

## Decision and scope

Use this package only in data-access infrastructure to classify database constraint failures. It does not define domain errors or the public HTTP/API error contract.

## Recommended registration and use

Reference the provider-specific package and add its processor to the existing Npgsql options chain:

```xml
<ItemGroup>
  <PackageReference Include="EntityFrameworkCore.Exceptions.PostgreSQL" />
</ItemGroup>
```

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppDatabase"))
        .UseExceptionProcessor());
```

For `AddDbContextPool`, configure `UseExceptionProcessor()` on the pooled options builder, as shown upstream. Catch only the classified cases the use case can translate:

```csharp
using EntityFramework.Exceptions.Common;

try
{
    await db.SaveChangesAsync(cancellationToken);
}
catch (UniqueConstraintException exception)
{
    logger.LogInformation(exception, "Order number conflict");
    throw new ApplicationConflictException("order_number_already_exists", exception);
}
catch (ReferenceConstraintException exception)
{
    throw new ApplicationConflictException("referenced_record_is_in_use", exception);
}
```

The library also classifies missing required values, excessive length, numeric overflow, and deadlocks. Preserve the original exception for protected diagnostics while returning a stable, generic application error externally.

## Enterprise implementation guidance

Keep constraints authoritative: a preflight uniqueness query may improve a message but cannot prevent races. Configure exception processing in the `DbContext` composition root and test actual PostgreSQL failures.

Define unique/foreign-key constraints in the EF model when you need `ConstraintName` and `ConstraintProperties`; upstream notes those fields are not populated for indexes that exist only in the database or were created using `MigrationBuilder.Sql`. Map only explicitly recognized constraints to public application errors. Allow connectivity, authentication, syntax, timeout, serialization, and unknown failures to follow the normal infrastructure-failure path. If a deadlock or transient failure is retried, make the full unit of work idempotent and follow the Npgsql/EF execution-strategy policy.

### Upgrade and rollback

Upgrade this provider-specific processor with its compatible EF Core/Npgsql major line. Before promotion, trigger every application-mapped PostgreSQL constraint and deadlock case and verify the concrete exception type, metadata, public translation, and retry decision. The package owns no schema, but constraint renames in accompanying migrations can change diagnostic metadata. Roll back the package and application translation together; database migrations require their own forward-fix or restore procedure.

## Integration with the catalog

Use with [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md), [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md), and the cataloged Npgsql provider. Keep resulting application errors independent of [Ardalis specifications](ardalis-specification.md). See [PostgreSQL data-access selection](../package-guidance/package-selection.md#postgresql-data-access), the [EF Core/PostgreSQL recipe](../recipes/efcore-npgsql-exception-mapping.md), and the [supply-chain entry](../package-guidance/supply-chain.md#entityframeworkcore-exceptions-postgresql).

## Security, performance, AOT, trimming, and operations

Never disclose SQL, constraint/schema names, entity values, connection strings, or provider stack traces to clients. Log protected correlation data and stable error categories; apply log retention and access controls because exception objects may carry sensitive provider detail.

Classification happens after the database rejects a write; it is not input validation and does not remove the cost of failed transactions. Monitor failure rates by category and alert on unexpected spikes. AOT/trimming compatibility is unverified until the exact Npgsql/EF/package graph is published and real failure paths are exercised.

## Avoid

- Do not catch classified exceptions in domain entities or expose them from APIs.
- Do not replace database constraints with application-side prechecks.
- Do not map every database failure to a client validation response.
- Do not depend on a database constraint name as a public, long-lived API code.

## Verification checklist

- [ ] Compile exact 10.0.1 assets with EF Core/Npgsql in a consuming project.
- [ ] Trigger unique, foreign-key/reference, null, length, check, and numeric failures against PostgreSQL.
- [ ] Verify model-defined constraints populate metadata and unknown constraints take the generic failure path.
- [ ] Assert stable public errors and absence of provider details in responses/log exports.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [EntityFrameworkCore.Exceptions.PostgreSQL 10.0.1 on NuGet](https://www.nuget.org/packages/EntityFrameworkCore.Exceptions.PostgreSQL/10.0.1)
- [EntityFrameworkCore.Exceptions repository](https://github.com/Giorgi/EntityFramework.Exceptions)
- [EntityFrameworkCore.Exceptions setup and exception types](https://github.com/Giorgi/EntityFramework.Exceptions#how-do-i-get-started)
- [EF Core error handling and retry guidance](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency)
