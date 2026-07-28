# EF Core and Npgsql with stable PostgreSQL exception mapping

## Problem and boundary

This recipe configures one EF Core `DbContext` for PostgreSQL, applies snake-case identifiers before the first migration, and translates a known database constraint failure into a stable application outcome. EF Core owns change tracking and the unit of work, Npgsql owns PostgreSQL connectivity and SQL translation, `EFCore.NamingConventions` changes model identifiers, and `EntityFrameworkCore.Exceptions.PostgreSQL` classifies database-rejected writes. The database constraint remains authoritative; public errors never expose provider exception text or schema names.

## Required packages

The repository-oriented
`src/IX.Modularity.Orders.Adapters.PostgreSql/IX.Modularity.Orders.Adapters.PostgreSql.csproj`
project uses central package versions:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IXModularityProjectRole>Adapter</IXModularityProjectRole>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="EFCore.NamingConventions" />
    <PackageReference Include="EntityFrameworkCore.Exceptions.PostgreSQL" />
    <PackageReference Include="FluentResults" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
  </ItemGroup>
</Project>
```

The Npgsql EF provider supplies `UseNpgsql`; a separate direct `Npgsql` reference is unnecessary unless the project uses ADO.NET APIs directly. `FluentResults` carries the expected application conflict without turning a provider exception into a transport contract.

## Model and composition

Define the database constraint explicitly and give it a stable internal name:

```csharp
using Microsoft.EntityFrameworkCore;

public sealed class Order
{
    private Order() { }

    public Order(Guid id, string number)
    {
        Id = id;
        Number = number;
    }

    public Guid Id { get; private set; }
    public string Number { get; private set; } = null!;
}

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.HasKey(entity => entity.Id);
            order.Property(entity => entity.Number)
                .HasMaxLength(32)
                .IsRequired();
            order.HasIndex(entity => entity.Number)
                .IsUnique()
                .HasDatabaseName("uq_orders_order_number");
        });
    }
}
```

The unique index closes the race that an application-side existence check cannot. Its name is internal diagnostic metadata and must not become the public API code. Explicit length and required constraints keep the EF model aligned with PostgreSQL. The private parameterless constructor supports materialization while mutation remains controlled.

Configure all provider extensions on the same options chain:

```csharp
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var connectionString = Environment.GetEnvironmentVariable("ORDERS_DB_CONNECTION")
    ?? throw new InvalidOperationException("ORDERS_DB_CONNECTION is required.");

services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention()
        .UseExceptionProcessor());

services.AddScoped<IOrderWriter, EfOrderWriter>();
```

`UseNpgsql` selects the relational provider, `UseSnakeCaseNamingConvention` transforms generated identifiers, and `UseExceptionProcessor` adds provider-specific write-failure classification. Apply the naming convention before the first production migration. Enabling or changing it on an established schema can generate broad renames and requires a reviewed migration, dependency inventory, rollback/forward-fix plan, and production-like rehearsal. Supply the connection string from an approved secret/configuration provider; never embed or log it.

## Exception-to-result boundary

Translate only the known unique constraint into an application-owned typed error:

```csharp
using EntityFramework.Exceptions.Common;
using FluentResults;

public sealed record OrderCreated(Guid Id, string Number);

public sealed class DuplicateOrderNumberError()
    : Error("An order with this number already exists.")
{
    public const string Code = "order_number_already_exists";
}

public interface IOrderWriter
{
    Task<Result<OrderCreated>> CreateAsync(
        string number,
        CancellationToken cancellationToken);
}

public sealed class EfOrderWriter(OrdersDbContext dbContext) : IOrderWriter
{
    public async Task<Result<OrderCreated>> CreateAsync(
        string number,
        CancellationToken cancellationToken)
    {
        var order = new Order(Guid.NewGuid(), number);
        dbContext.Orders.Add(order);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok(new OrderCreated(order.Id, order.Number));
        }
        catch (UniqueConstraintException exception)
            when (exception.ConstraintName == "uq_orders_order_number")
        {
            return Result.Fail<OrderCreated>(new DuplicateOrderNumberError());
        }
    }
}
```

Classification happens only after PostgreSQL rejects `SaveChangesAsync`. The filter prevents an unrelated unique constraint from being mislabeled as an order-number conflict. Unknown constraint violations, connectivity/authentication failures, timeouts, concurrency failures, and cancellation continue as exceptions to the application host, which can apply its normal telemetry and safe generic response. Do not retry a uniqueness conflict. Retry a transient transaction only through a reviewed EF/Npgsql execution strategy and only when the complete unit of work is idempotent.

After a failed save, do not continue using the tracked failed entity as though it were persisted. This scoped context ends with the operation; a longer-lived workflow would need to clear/detach rejected state deliberately or start a fresh unit of work.

## Migration and deployment workflow

Generate and review migration SQL rather than relying on runtime schema creation:

```bash
dotnet ef migrations add InitialOrders \
  --project src/IX.Modularity.Orders.Adapters.PostgreSql \
  --startup-project ../Orders.Api

dotnet ef migrations script --idempotent \
  --project src/IX.Modularity.Orders.Adapters.PostgreSql \
  --startup-project ../Orders.Api \
  --output artifacts/orders-migration.sql
```

The design-time package/tooling belongs in the migration workflow, not the runtime example above. Here `../Orders.Api` represents the consuming standalone application's startup project; it is not a repository project-role example. Review the generated table, column, key, index, and sequence names—especially `uq_orders_order_number`—and deploy through the repository's controlled migration identity and sequencing. An idempotent script does not make a risky rename or data transformation safe; rehearse locking, duration, recovery, and dependent SQL against production-like PostgreSQL.

## Failure modes and operations

| Symptom | Likely boundary | Observation and safe response |
| --- | --- | --- |
| Duplicate writes surface as generic `DbUpdateException` | Exception processor/configuration | Confirm `UseExceptionProcessor()` is on the active Npgsql options chain and reproduce against real PostgreSQL, not EF InMemory. |
| A known duplicate still escapes | Schema/model drift | Compare the deployed index/constraint name with the migration and classified exception metadata. Correct schema drift; do not broaden the catch to all unique violations. |
| A new migration renames most objects | Naming convention change | Stop deployment, compare model snapshots and convention versions, inventory dependent SQL, and produce a reviewed migration/recovery plan. |
| Repeated timeouts or pool waits | Database/connectivity | Correlate EF command logs and Npgsql telemetry with PostgreSQL locks, plans, connections, and network signals. Diagnose before adding retries. |
| Public errors contain SQL or constraint names | Transport/logging boundary | Return only the stable application code/message and keep provider detail in access-controlled diagnostics with appropriate retention. |

Observe save duration and outcome, stable classified-error categories, optimistic concurrency failures, execution-strategy retries, Npgsql pool pressure, PostgreSQL lock/statement latency, and migration duration. Keep EF sensitive-data logging and detailed provider errors disabled in production except under a time-bounded approved diagnostic procedure. Never record connection strings, parameters containing personal data, raw SQL with values, or provider exception payloads in client-visible telemetry.

## Verification checklist

Authoring evidence:

- [x] The model, configuration, and exception-mapping sample compiled in a temporary `net10.0` SDK project with the pinned package graph.
- [ ] No PostgreSQL instance was contacted; classification and migration behavior were not integration-tested during authoring.

Consuming-application checks:

- [ ] Generate and review a migration; confirm all snake-case identifiers and the expected unique index name.
- [ ] Against real PostgreSQL, create an order, then trigger the same number concurrently and assert the stable conflict result.
- [ ] Trigger an unrelated unique constraint and confirm it does not map to `order_number_already_exists`.
- [ ] Test connectivity, authentication, timeout, cancellation, concurrency, and unknown update failures through the generic infrastructure-failure path.
- [ ] Confirm responses and exported logs contain no SQL, schema names, values, connection strings, or provider stack traces.
- [ ] Rehearse migration deployment and recovery against a production-like database before changing an established naming convention.

## Related guides

- [Microsoft.EntityFrameworkCore](../packages/microsoft-entityframeworkcore.md)
- [Npgsql.EntityFrameworkCore.PostgreSQL](../packages/npgsql.entityframeworkcore.postgresql.md)
- [EFCore.NamingConventions](../packages/efcore-namingconventions.md)
- [EntityFrameworkCore.Exceptions.PostgreSQL](../packages/entityframeworkcore-exceptions-postgresql.md)
- [FluentResults](../packages/fluentresults.md)
- [PostgreSQL data-access selection](../package-guidance/package-selection.md#postgresql-data-access)

## Primary sources

Accessed 2026-07-27.

- [EF Core DbContext configuration](https://learn.microsoft.com/ef/core/dbcontext-configuration/)
- [EF Core migrations management](https://learn.microsoft.com/ef/core/managing-schemas/migrations/managing)
- [EF Core connection resiliency](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency)
- [Npgsql Entity Framework Core provider](https://www.npgsql.org/efcore/)
- [Npgsql table and naming guidance](https://www.npgsql.org/efcore/modeling/tables.html)
- [EFCore.NamingConventions upstream usage and migration warning](https://github.com/efcore/EFCore.NamingConventions#usage)
- [EntityFrameworkCore.Exceptions setup and classified exception types](https://github.com/Giorgi/EntityFramework.Exceptions#how-do-i-get-started)
- [Microsoft.EntityFrameworkCore 10.0.10 on NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.10)
- [Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 on NuGet](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3)
- [EFCore.NamingConventions 10.0.1 on NuGet](https://www.nuget.org/packages/EFCore.NamingConventions/10.0.1)
- [EntityFrameworkCore.Exceptions.PostgreSQL 10.0.1 on NuGet](https://www.nuget.org/packages/EntityFrameworkCore.Exceptions.PostgreSQL/10.0.1)
- [FluentResults 4.0.0 on NuGet](https://www.nuget.org/packages/FluentResults/4.0.0)
