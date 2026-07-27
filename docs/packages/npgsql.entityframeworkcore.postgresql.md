# Npgsql.EntityFrameworkCore.PostgreSQL

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Pinned version | `10.0.3` |
| Status | Approved catalog dependency |
| Role | EF Core provider for PostgreSQL types, migrations, SQL translation, JSON, enums, and full-text search |

## Decision and scope

Use this provider for EF Core access to PostgreSQL-specific capabilities. It owns database migrations and provider SQL translation; it does not make arbitrary CLR methods server-evaluable. Keep PostgreSQL-only expressions in the EF adapter layer when composing Ardalis specifications.

## Recommended registration and use

- Reference the centrally pinned provider without a version:

```xml
<ItemGroup>
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
</ItemGroup>
```

Configure driver and EF mappings separately when injecting an `NpgsqlDataSource`:

```csharp
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<Status>();
var dataSource = dataSourceBuilder.Build();

services.AddDbContextPool<AppDbContext>(options =>
    options.UseNpgsql(dataSource, npgsql =>
        npgsql.MapEnum<Status>("status")));
```

With a connection string directly in `UseNpgsql`, `MapEnum<Status>("status")` configures both layers and makes migrations create the PostgreSQL enum.

A common read workflow applies stable authorization filters, keeps evaluation on the server, avoids tracking for read-only results, and passes cancellation through materialization:

```csharp
var customers = await dbContext.Customers
    .AsNoTracking()
    .Where(customer => customer.TenantId == tenantId && customer.Status == Status.Active)
    .OrderBy(customer => customer.Name)
    .ThenBy(customer => customer.Id)
    .Select(customer => new CustomerSummary(customer.Id, customer.Name))
    .Take(100)
    .ToListAsync(cancellationToken);
```

## Enterprise implementation guidance

### JSON mapping choices

PostgreSQL `jsonb` is normally preferable to `json` for query efficiency. For a stable document schema on EF Core 10, model JSON as a complex type and call `ToJson()`:

```csharp
modelBuilder.Entity<Customer>()
    .ComplexProperty(customer => customer.Details, details => details.ToJson());
```

This is the recommended POCO approach: EF understands the document structure and can translate richer queries and updates. Use `JsonDocument`/`JsonElement` where the schema is genuinely variable; `JsonDocument` is disposable and entities that own it must dispose it. A `string` mapped as `jsonb` delegates parsing and validation to the application. Avoid legacy provider-specific POCO JSON mapping for new code.

### Full-text search

Map `tsvector` to `NpgsqlTsVector`, then use a stored generated column and GIN index:

```csharp
modelBuilder.Entity<Product>()
    .HasGeneratedTsVectorColumn(
        product => product.SearchVector,
        "english",
        product => new { product.Name, product.Description })
    .HasIndex(product => product.SearchVector)
    .HasMethod("GIN");
```

```csharp
var query = EF.Functions.WebSearchToTsQuery("english", userInput);
var results = context.Products
    .Where(product => product.SearchVector.Matches(query))
    .OrderByDescending(product => product.SearchVector.RankCoverDensity(query));
```

`ToTsQuery` accepts tsquery syntax; use `PlainToTsQuery`, `PhraseToTsQuery`, or `WebSearchToTsQuery` for free text according to the desired PostgreSQL parsing behavior. For JSON documents, generated-vector support uses `json_to_tsvector`/`jsonb_to_tsvector`; use computed SQL when its default `all` filter is not the required policy.

### Migration workflow

Create migrations from the startup project that supplies the design-time context configuration, inspect generated SQL, and produce a reviewed deployment artifact:

```bash
dotnet ef migrations add AddCustomerSearch --project src/Infrastructure --startup-project src/Api
dotnet ef migrations script --idempotent --project src/Infrastructure --startup-project src/Api --output artifacts/postgresql.sql
```

Apply migrations under a deployment identity, not concurrently from every application replica. Back up and rehearse destructive or long-running changes, verify PostgreSQL-version compatibility, and define rollback or forward-fix steps before production execution.

## Integration with the catalog

Use [Npgsql](npgsql.md) for the external data source, [Pgvector.EntityFrameworkCore](pgvector.entityframeworkcore.md) for vector migrations and indexes, `Ardalis.Specification.EntityFrameworkCore` for query composition, and `MR.EntityFrameworkCore.KeysetPagination` after defining a complete stable order.

Npgsql translates many ordinary .NET/EF constructs—including regex, strings, JSON, arrays, ranges, network types, and full-text functions—to PostgreSQL. Inspect generated SQL for important queries and keep predicates/orderings translatable.

## Security, performance, AOT, trimming, and operations

Full-text search requires an index for efficient querying. For ordered results, add a unique final tie-breaker (normally the primary key); a ranking score alone is not a deterministic cursor key. Use keyset pagination only after carrying that complete ordering into the keyset definition. Validate generated migration SQL and translated query plans against the deployed PostgreSQL version. Keep a pooled `DbContext` free of request-specific mutable state, and remember that `DbContext` is not thread-safe. AOT/trimming behavior depends on EF Core, the provider, mappings, and application model; verify with the real publish configuration.

## Avoid

- Do not map new stable-schema JSON documents with deprecated legacy POCO mapping.
- Do not feed untrusted free text directly to `ToTsQuery`.
- Do not assume a CLR method is translated without inspecting its provider support and generated SQL.
- Do not run production migrations automatically from every service instance.
- Do not reuse one `DbContext` concurrently or hold it across unrelated workflows.

## Verification checklist

- [ ] External data sources map each EF enum on both driver and provider layers.
- [ ] JSON representation matches schema stability and disposable DOM ownership is explicit.
- [ ] FTS migrations create the intended generated vector and GIN/GiST index.
- [ ] Search and pagination order ends with a unique key.
- [ ] Important queries remain server-translated and have plans checked on production-like data.
- [ ] The reviewed migration script, execution identity, backup, and rollback/forward-fix procedure are ready.

## Sources

- [EF provider setup](https://www.npgsql.org/efcore/)
- [JSON mapping](https://www.npgsql.org/efcore/mapping/json.html?tabs=data-annotations%2Ccomplex-types%2Cjsondocument)
- [Enum mapping](https://www.npgsql.org/efcore/mapping/enum.html?tabs=with-connection-string%2Cwith-datasource)
- [Full-text search mapping and translations](https://www.npgsql.org/efcore/mapping/full-text-search.html)
- [Provider translation table](https://www.npgsql.org/efcore/mapping/translations.html)
- [EF Core migrations overview](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [EF Core efficient querying guidance](https://learn.microsoft.com/ef/core/performance/efficient-querying)
- [Package on NuGet](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3)

Accessed 2026-07-27.
