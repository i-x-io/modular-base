# Pgvector

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `Pgvector` |
| Pinned version | `0.3.2` |
| Status | Approved catalog dependency |
| Role | .NET mappings for pgvector types and Npgsql type registration |

## Decision and scope

Use this package for Npgsql-level pgvector type registration and `Vector` values. It does not install the PostgreSQL extension or define product ranking policy.

## Recommended registration and use

Register the .NET type handler before building the data source. Provision the PostgreSQL extension before using vector columns; after creating it through an existing connection, reload PostgreSQL type metadata.

```csharp
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();
await using var dataSource = dataSourceBuilder.Build();

await using var connection = await dataSource.OpenConnectionAsync();
await using var command = new NpgsqlCommand(
    "CREATE EXTENSION IF NOT EXISTS vector", connection);
await command.ExecuteNonQueryAsync();
connection.ReloadTypes();
```

## Enterprise implementation guidance

Use a `Vector` parameter with the same dimension as the column. `<->` is L2 distance; pgvector also supports inner-product and cosine operators. An approximate index must use the operator class matching the query distance.

```sql
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_items_embedding_hnsw
    ON items USING hnsw (embedding vector_cosine_ops);
```

Use `vector_l2_ops` for L2, `vector_ip_ops` for inner product, and `vector_cosine_ops` for cosine distance. HNSW offers a recall/latency trade-off; `hnsw.ef_search` raises recall at a speed cost and `SET LOCAL` confines a tuning override to one transaction. IVFFlat is another approximate-index option; configure it only after measuring dataset size, build time, recall, and query latency.

## Integration with the catalog

Use [Npgsql](npgsql.md) to build the registered data source. Use [Pgvector.EntityFrameworkCore](pgvector.entityframeworkcore.md) where EF owns extension migrations and index definitions.

## Security, performance, AOT, trimming, and operations

The database server must have pgvector installed and the database principal must be permitted to create/enable the extension. For EF-owned databases, prefer the migration declaration rather than issuing DDL from request code. Create large production indexes concurrently to avoid blocking writes, and measure recall and latency for each index/tuning combination.

## Avoid

- Do not use an operator class that differs from the query distance metric.
- Do not create extensions or indexes on the request path.
- Do not treat approximate nearest-neighbor output as an exact ranking guarantee.

## Verification checklist

- [ ] The server has the pgvector extension installed and migration/DDL permission is appropriate.
- [ ] Data-source registration occurs before opening connections.
- [ ] Column dimension, vector parameter dimension, query metric, and index operator class agree.
- [ ] HNSW/IVFFlat recall and latency are measured on representative data.

## Sources

- [pgvector-dotnet Npgsql setup](https://github.com/pgvector/pgvector-dotnet#npgsql-c)
- [pgvector indexes, operator classes, and tuning](https://github.com/pgvector/pgvector#indexing)
- [Pgvector package on NuGet](https://www.nuget.org/packages/Pgvector/0.3.2)

Accessed 2026-07-27.
