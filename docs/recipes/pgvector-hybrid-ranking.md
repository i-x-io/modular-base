# Pgvector similarity search with hybrid ranking

## Problem and boundary

This recipe retrieves a bounded semantic candidate set with pgvector, retrieves a bounded PostgreSQL full-text candidate set, and fuses their ranks with Reciprocal Rank Fusion (RRF). `Pgvector` owns the .NET vector type and Npgsql registration, Npgsql owns parameterized PostgreSQL access, PostgreSQL owns both candidate queries, and the application owns the fusion constant, candidate limits, eligibility rules, and final ordering. It does not treat approximate nearest-neighbor output or RRF as a complete product-ranking policy.

The example assumes that a controlled migration has already enabled the `vector` extension and created `documents(id, tenant_id, title, is_searchable, search_vector, embedding vector(1536))`, including a GIN index for `search_vector` and an HNSW index using `vector_cosine_ops`. Schema and index creation do not belong on the request path.

## Required packages

A PostgreSQL integration project can own this provider-specific implementation:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Npgsql" />
    <PackageReference Include="Pgvector" />
  </ItemGroup>
</Project>
```

`Npgsql` supplies the data source and command APIs. `Pgvector` supplies `Vector` and `UseVector()`. An embedding provider is intentionally outside this recipe: call it before the repository with a model-versioned, dimension-validated vector, and do not make a database retry regenerate or rebill an embedding.

## Register one vector-aware data source

Register vector mappings before the immutable data source is built:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pgvector;

var connectionString = Environment.GetEnvironmentVariable("SEARCH_DB_CONNECTION")
    ?? throw new InvalidOperationException("SEARCH_DB_CONNECTION is required.");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();

var services = new ServiceCollection();
services.AddSingleton(dataSourceBuilder.Build());
services.AddSingleton<HybridSearchRepository>();
```

`UseVector()` installs the Npgsql mappings needed to send a `Vector` parameter and read vector values. The built data source owns pooling and is intended to be shared for the host lifetime; the host must dispose its service provider on shutdown. Supply the connection string through an approved secret/configuration provider and never record it in logs or telemetry.

## Retrieve and fuse bounded candidate sets

Keep eligibility filters identical in both branches, assign 1-based ranks inside each branch, then aggregate RRF contributions:

```csharp
using Npgsql;
using Pgvector;

public sealed record SearchHit(long Id, string Title, double Score);

public sealed class HybridSearchRepository(NpgsqlDataSource dataSource)
{
    private const int CandidateLimit = 100;
    private const int ResultLimit = 20;
    private const int RrfK = 60;

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        Guid tenantId,
        string text,
        ReadOnlyMemory<float> embedding,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (embedding.Length != 1536)
        {
            throw new ArgumentException(
                "The embedding must contain exactly 1536 values.",
                nameof(embedding));
        }

        if (embedding.Span.ContainsAnyExceptInRange(float.MinValue, float.MaxValue))
        {
            throw new ArgumentException(
                "The embedding must contain only finite values.",
                nameof(embedding));
        }

        const string sql =
            """
            WITH semantic AS (
                SELECT id,
                       row_number() OVER (ORDER BY embedding <=> $1) AS rank
                FROM documents
                WHERE tenant_id = $2 AND is_searchable
                ORDER BY embedding <=> $1
                LIMIT $3
            ),
            lexical AS (
                SELECT id,
                       row_number() OVER (
                           ORDER BY ts_rank_cd(
                               search_vector,
                               websearch_to_tsquery('english', $4)) DESC
                       ) AS rank
                FROM documents
                WHERE tenant_id = $2
                  AND is_searchable
                  AND search_vector @@ websearch_to_tsquery('english', $4)
                ORDER BY ts_rank_cd(
                    search_vector,
                    websearch_to_tsquery('english', $4)) DESC
                LIMIT $3
            ),
            contributions AS (
                SELECT id, 1.0 / ($5 + rank) AS score FROM semantic
                UNION ALL
                SELECT id, 1.0 / ($5 + rank) AS score FROM lexical
            ),
            fused AS (
                SELECT id, sum(score) AS score
                FROM contributions
                GROUP BY id
            )
            SELECT document.id, document.title, fused.score
            FROM fused
            JOIN documents AS document ON document.id = fused.id
            ORDER BY fused.score DESC, document.id ASC
            LIMIT $6
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue(new Vector(embedding));
        command.Parameters.AddWithValue(tenantId);
        command.Parameters.AddWithValue(CandidateLimit);
        command.Parameters.AddWithValue(text);
        command.Parameters.AddWithValue(RrfK);
        command.Parameters.AddWithValue(ResultLimit);

        var hits = new List<SearchHit>(ResultLimit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hits.Add(new SearchHit(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetDouble(2)));
        }

        return hits;
    }
}
```

The semantic branch preserves the indexable `ORDER BY embedding <=> parameter LIMIT` shape; `<=>` is cosine distance and must match the HNSW `vector_cosine_ops` operator class. The lexical branch uses the same tenant and visibility predicate. RRF combines ranks rather than incomparable raw vector-distance and text-rank magnitudes, and `UNION ALL` preserves both contributions when a document appears in both lists. The unique `id` tie-breaker makes the returned page deterministic for one database snapshot.

All values are positional parameters; user text, tenant identity, limits, and the vector are never concatenated into SQL. `CandidateLimit`, `ResultLimit`, and `RrfK` are application policy, not pgvector defaults. Version them with the embedding model and evaluate them on labeled relevance data. If user-controlled text length or query complexity is material, validate it before this boundary and apply a database statement budget at the composition root.

The finite-value guard uses the C# 14 span search API. If the consuming codebase targets an earlier language/runtime, replace it with an explicit finite-value loop; never allow `NaN` or infinities to become stored ranking data.

## Migration and index contract

A reviewed migration should establish compatible full-text and vector indexes:

```sql
CREATE EXTENSION IF NOT EXISTS vector;

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_documents_search_vector
    ON documents USING gin (search_vector);

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_documents_embedding_hnsw
    ON documents USING hnsw (embedding vector_cosine_ops);
```

Run extension and index DDL through a migration identity, not the runtime identity. `CREATE INDEX CONCURRENTLY` cannot run inside a normal PostgreSQL transaction and therefore needs an explicit migration strategy. A filtered approximate search may return too few eligible rows or choose a sequential scan; inspect the plan and measure recall rather than assuming that an index definition guarantees its use.

## Failure modes and operations

| Symptom | Likely boundary | Observation and safe response |
| --- | --- | --- |
| `vector` type or handler error | Extension/type registration | Confirm the extension exists and `UseVector()` ran before `Build()`. After enabling an extension on an already-open connection, reload types or reopen through the configured data source. |
| Dimension or non-finite-value failure | Embedding contract | Reject the request before SQL and compare the model/version with `vector(1536)`. Retrying cannot repair the contract. |
| Sequential scan or high latency | Query/index mismatch | Inspect `EXPLAIN (ANALYZE, BUFFERS)`, operator class, `ORDER BY ... LIMIT`, filters, statistics, and index readiness. Do not hide the issue with broad retries. |
| Good semantic or lexical results disappear after fusion | Ranking policy | Capture branch ranks and candidate membership in a safe offline evaluation dataset, then tune limits and `RrfK` against labeled judgments. |
| Too few tenant-eligible approximate results | Filtering/ANN recall | Measure exact-versus-approximate recall and evaluate iterative scans, partitioning, partial indexes, or a larger measured candidate limit for the deployed pgvector version. |

Observe branch latency, candidate count, fused-result count, query-plan/index selection, exact-versus-approximate recall, and bounded model/ranking-policy versions. Do not attach raw query text, embeddings, document bodies, tenant identifiers, connection strings, or unbounded document IDs to metrics or traces. Database connectivity and serialization faults may be transient, but retry only inside the caller's remaining budget and only when the complete read operation is safe; ranking/configuration faults are not retryable.

## Verification checklist

Authoring evidence:

- [x] The registration and repository sample compiled in a temporary `net10.0` SDK project with the catalog's pinned Npgsql and Pgvector packages.
- [x] No PostgreSQL instance was contacted; SQL translation, index selection, recall, and ranking relevance were not integration-tested during authoring.

Consuming-application checks:

- [ ] Apply reviewed extension/table/index migrations in a disposable PostgreSQL environment.
- [ ] Verify the embedding dimension, finite-value validation, model version, and `vector_cosine_ops` contract.
- [ ] Inspect `EXPLAIN (ANALYZE, BUFFERS)` for both branches with representative tenant filters and data volumes.
- [ ] Compare approximate results with an exact-search baseline and record recall/latency targets.
- [ ] Evaluate semantic-only, lexical-only, and fused results on labeled queries before selecting candidate limits and `RrfK`.
- [ ] Exercise cancellation, statement timeout, unavailable database, malformed text, empty-result, and schema-drift paths without leaking sensitive data.

## Related guides

- [Pgvector](../packages/pgvector.md)
- [Pgvector.EntityFrameworkCore](../packages/pgvector.entityframeworkcore.md)
- [Npgsql](../packages/npgsql.md)
- [PostgreSQL data-access selection](../package-guidance/package-selection.md#postgresql-data-access)

## Primary sources

Accessed 2026-07-27.

- [pgvector-dotnet 0.3.2 Npgsql setup and nearest-neighbor query](https://github.com/pgvector/pgvector-dotnet/blob/v0.3.2/README.md#npgsql-c)
- [pgvector indexing, operator classes, filtering, and tuning](https://github.com/pgvector/pgvector#indexing)
- [pgvector hybrid-search guidance](https://github.com/pgvector/pgvector#hybrid-search)
- [PostgreSQL full-text search controls](https://www.postgresql.org/docs/current/textsearch-controls.html)
- [Npgsql basic usage and data sources](https://www.npgsql.org/doc/basic-usage.html)
- [Pgvector 0.3.2 on NuGet](https://www.nuget.org/packages/Pgvector/0.3.2)
- [Npgsql 10.0.3 on NuGet](https://www.nuget.org/packages/Npgsql/10.0.3)
