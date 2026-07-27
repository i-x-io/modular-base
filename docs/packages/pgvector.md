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

- Reference the centrally pinned package without a version:

```xml
<ItemGroup>
  <PackageReference Include="Pgvector" />
</ItemGroup>
```

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

The DDL above belongs in a controlled bootstrap step when EF migrations do not own the schema. Normal request code should assume the extension and table already exist. Insert and query vectors as parameters; the column and query vector must have the same dimension:

```csharp
var queryEmbedding = new Vector(new float[] { 0.12f, -0.34f, 0.56f });

await using var command = dataSource.CreateCommand(
    """
    SELECT id, embedding <=> $1 AS distance
    FROM items
    WHERE tenant_id = $2
    ORDER BY embedding <=> $1
    LIMIT 20
    """);
command.Parameters.AddWithValue(queryEmbedding);
command.Parameters.AddWithValue(tenantId);

await using var reader = await command.ExecuteReaderAsync(cancellationToken);
while (await reader.ReadAsync(cancellationToken))
{
    var id = reader.GetInt64(0);
    var cosineDistance = reader.GetDouble(1);
}
```

The `<=>` expression is cosine distance, so smaller values are nearer. Convert it to cosine similarity only when a product contract needs similarity (`1 - distance`) and document the score definition.

## Enterprise implementation guidance

Use a `Vector` parameter with the same dimension as the column. `<->` is L2 distance, `<#>` is negative inner product, and `<=>` is cosine distance. An approximate index must use the operator class matching the query distance.

```sql
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_items_embedding_hnsw
    ON items USING hnsw (embedding vector_cosine_ops);
```

Use `vector_l2_ops` for L2, `vector_ip_ops` for inner product, and `vector_cosine_ops` for cosine distance. The nearest-neighbor shape must be `ORDER BY` a distance operator plus `LIMIT` for an approximate index scan; wrapping the indexed expression or sorting by a differently defined score can prevent index use.

HNSW offers a recall/latency trade-off; `hnsw.ef_search` raises recall at a speed cost and `SET LOCAL` confines a tuning override to one transaction. IVFFlat is another approximate-index option; build it only after the table has representative data and tune its lists/probes from measured dataset size, recall, build time, and query latency. Keep eligibility filters consistent and inspect plans, because filtered approximate queries may need iterative scans or different indexing strategies on the deployed pgvector version.

## Integration with the catalog

Use [Npgsql](npgsql.md) to build the registered data source. Use [Pgvector.EntityFrameworkCore](pgvector.entityframeworkcore.md) where EF owns extension migrations and index definitions.

## Security, performance, AOT, trimming, and operations

The database server must have pgvector installed and the database principal must be permitted to create/enable the extension. For EF-owned databases, prefer the migration declaration rather than issuing DDL from request code. Use a separate migration/deployment identity so the runtime principal does not need extension or index DDL privileges. Create large production indexes concurrently where the migration workflow supports non-transactional concurrent DDL, and measure recall and latency for each index/tuning combination. Reject non-finite values and dimension mismatches before persistence; do not log embeddings if they are sensitive derived data.

## Avoid

- Do not use an operator class that differs from the query distance metric.
- Do not create extensions or indexes on the request path.
- Do not treat approximate nearest-neighbor output as an exact ranking guarantee.
- Do not concatenate vector literals or tenant identifiers into SQL.
- Do not compare latency alone; track recall against an exact-search baseline.

## Verification checklist

- [ ] The server has the pgvector extension installed and migration/DDL permission is appropriate.
- [ ] Data-source registration occurs before opening connections.
- [ ] Column dimension, vector parameter dimension, query metric, and index operator class agree.
- [ ] HNSW/IVFFlat recall and latency are measured on representative data.
- [ ] Parameterized SQL preserves `ORDER BY distance LIMIT` and the expected index appears in `EXPLAIN (ANALYZE, BUFFERS)`.
- [ ] Runtime credentials cannot create extensions or indexes, and embedding logging follows data policy.

## Sources

- [pgvector-dotnet 0.3.2 Npgsql setup](https://github.com/pgvector/pgvector-dotnet/blob/v0.3.2/README.md#npgsql-c)
- [pgvector indexes, operator classes, and tuning](https://github.com/pgvector/pgvector#indexing)
- [pgvector query operators and nearest-neighbor examples](https://github.com/pgvector/pgvector#querying)
- [Pgvector package on NuGet](https://www.nuget.org/packages/Pgvector/0.3.2)

Accessed 2026-07-27.
