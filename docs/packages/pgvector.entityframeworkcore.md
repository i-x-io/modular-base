# Pgvector.EntityFrameworkCore

> **Owner:** `IX` · **Last reviewed:** `2026-07-27` · **Review trigger:** Package, Pgvector/Npgsql/EF Core, server-extension, target-framework, or ranking/index-policy change.

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `Pgvector.EntityFrameworkCore` |
| Pinned version | `0.3.0` |
| Status | Approved catalog dependency |
| Role | EF Core model, migration, index, and LINQ support for pgvector |

## Decision and scope

Use this package where EF Core owns pgvector extension declarations, migrations, and approximate-vector index metadata. It does not supply a built-in hybrid-search or reciprocal-rank-fusion API.

## Recommended registration and use

- Reference the centrally pinned EF integration. It transitively uses the base Pgvector mapping package, but the external data source and EF provider both still need vector registration:

```xml
<ItemGroup>
  <PackageReference Include="Pgvector.EntityFrameworkCore" />
</ItemGroup>
```

```csharp
using Pgvector;
using Pgvector.EntityFrameworkCore;

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();
var dataSource = dataSourceBuilder.Build();

services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource, npgsql => npgsql.UseVector()));
```

Declare the extension and vector dimension in the EF model so migrations include them, then configure the approximate index and matching operator class:

```csharp
modelBuilder.HasPostgresExtension("vector");

modelBuilder.Entity<Document>()
    .Property(document => document.Embedding)
    .HasColumnType("vector(1536)");

modelBuilder.Entity<Document>()
    .HasIndex(document => document.Embedding)
    .HasMethod("hnsw")
    .HasOperators("vector_cosine_ops")
    .HasStorageParameter("m", 16)
    .HasStorageParameter("ef_construction", 64);
```

Insert the same embedding dimension used by the model and order directly by the matching distance operation for nearest-neighbor retrieval:

```csharp
var queryEmbedding = new Vector(embeddingValues);

var matches = await dbContext.Documents
    .AsNoTracking()
    .Where(document => document.TenantId == tenantId && document.IsSearchable)
    .OrderBy(document => document.Embedding!.CosineDistance(queryEmbedding))
    .Select(document => new
    {
        document.Id,
        Distance = document.Embedding!.CosineDistance(queryEmbedding)
    })
    .Take(20)
    .ToListAsync(cancellationToken);
```

`CosineDistance` matches `vector_cosine_ops`; use `L2Distance` with `vector_l2_ops` or `MaxInnerProduct` with `vector_ip_ops`. Keep the SQL ordering as the bare distance expression with a bounded `Take` so PostgreSQL can use the approximate nearest-neighbor index; additional sort expressions can prevent that plan. Add a unique key only when reranking the bounded candidate set into the application's final order. Inspect generated SQL and the query plan rather than assuming the index is selected.

## Enterprise implementation guidance

The package supports HNSW and IVFFlat index metadata. Match `vector_l2_ops`, `vector_ip_ops`, or `vector_cosine_ops` to the distance operation used by the query; otherwise PostgreSQL cannot use that index for the ordered nearest-neighbor scan. Apply and review generated migrations in an environment where pgvector is installed.

Generate a migration after the extension, column dimension, and index are configured. Review the extension DDL and index operator class, then deploy it with a principal allowed to perform schema changes. Large index builds can block or consume substantial memory; if production requires `CREATE INDEX CONCURRENTLY`, use a reviewed migration strategy that accounts for PostgreSQL's prohibition on running it inside a transaction.

### Application guidance: hybrid search and RRF

Neither Pgvector nor Npgsql provides a built-in hybrid-search or reciprocal-rank-fusion API. pgvector recommends combining full-text search with vector search and suggests Reciprocal Rank Fusion (RRF); the following is application-layer policy, not package behavior:

1. Run a full-text candidate query (for example, `ts_rank_cd`) and a vector candidate query with independently measured limits.
2. Assign each document a 1-based rank within each candidate list.
3. Sum `1 / (k + rank)` over the lists in which a document occurs; choose and version `k` with relevance tests.
4. Apply business eligibility filters consistently to both candidate queries.
5. Order final output by fusion score descending, then by a unique document key ascending.

Vector similarity is a candidate generator, not a complete business-ranking policy. RRF candidate limits and `k` are relevance/latency controls and should be evaluated with a labeled query set, not copied as universal constants.

### Upgrade and rollback

Upgrade `Pgvector.EntityFrameworkCore` with compatible Pgvector, Npgsql provider/driver, and EF Core versions. Generate and inspect a no-model-change migration, compile distance expressions, and run PostgreSQL extension, dimension, index-DDL, plan, recall, and hybrid-ranking regressions. Treat embedding-model/dimension changes as a versioned data migration rather than a package update. Roll back the full application package set; reverse or forward-fix extension, column, generated migration, and index changes through a rehearsed database procedure.

## Integration with the catalog

Use [Pgvector](pgvector.md) for Npgsql type registration, [Npgsql.EntityFrameworkCore.PostgreSQL](npgsql.entityframeworkcore.postgresql.md) for full-text candidate queries, and `MR.EntityFrameworkCore.KeysetPagination` only after final ordering is complete.

Carry the exact final order—including the unique tie-breaker—into the keyset definition; do not paginate a floating vector, lexical, or fusion score alone.

See [PostgreSQL data-access selection](../package-guidance/package-selection.md#postgresql-data-access), the [pgvector hybrid-ranking recipe](../recipes/pgvector-hybrid-ranking.md), and the [supply-chain entry](../package-guidance/supply-chain.md#pgvector-entityframeworkcore).

## Security, performance, AOT, trimming, and operations

Apply migrations under a principal allowed to create the extension and index; keep those privileges away from the application runtime identity. Assess HNSW/IVFFlat build time, memory, recall, and latency on production-like data. EF translation is provider-dependent, so inspect generated migration SQL and query plans after upgrades. Validate embedding dimensions and finite values at the boundary, classify embeddings under the data-handling policy, and avoid logging them. AOT/trimming compatibility is unverified and requires publish testing with the actual EF model.

Track query latency, candidate count, exact-versus-approximate recall, model/index version, migration/index build duration, and expected index-plan use with bounded tags. If EF cannot translate a distance method, verify `UseVector()` on both provider and external data source and the pinned compatibility set; do not silently materialize an unbounded table. If the expected index is absent, inspect generated SQL, operator class, `Take`, filters, statistics, and index readiness. These are deterministic configuration/plan faults, not broadly retryable failures.

## Avoid

- Do not present RRF or hybrid search as a capability provided by this package.
- Do not use a ranking score alone as the cursor order.
- Do not deploy an approximate index without measuring recall and query latency.
- Do not register vectors only on the EF provider when an external `NpgsqlDataSource` is used; configure both layers.
- Do not expect a secondary `ThenBy` to make an approximate floating-distance cursor stable across changing data.

## Verification checklist

- [ ] The migration declares `vector` and generates the intended HNSW or IVFFlat operator class.
- [ ] Vector, lexical, and final fusion candidate limits are measured separately.
- [ ] Final ranking has a unique tie-breaker before keyset pagination.
- [ ] Relevance tests document the selected RRF `k` and candidate limits.
- [ ] External data-source and EF provider registrations both call `UseVector()`.
- [ ] Generated SQL retains the intended distance operator, bounded limit, tenant filter, and expected index plan.
- [ ] Migration privileges, index build impact, and rollback/forward-fix steps are reviewed.

## Sources

- [pgvector-dotnet 0.3.0 EF Core setup, queries, and index examples](https://github.com/pgvector/pgvector-dotnet/blob/efcore-v0.3.0/README.md#entity-framework-core)
- [pgvector hybrid-search guidance](https://github.com/pgvector/pgvector#hybrid-search)
- [pgvector HNSW operator classes and tuning](https://github.com/pgvector/pgvector#indexing)
- [Pgvector.EntityFrameworkCore package on NuGet](https://www.nuget.org/packages/Pgvector.EntityFrameworkCore/0.3.0)

Accessed 2026-07-27.
