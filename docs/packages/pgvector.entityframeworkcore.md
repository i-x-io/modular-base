# Pgvector.EntityFrameworkCore

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

Declare the extension in the EF model so migrations include it, then configure the approximate index and matching operator class:

```csharp
modelBuilder.HasPostgresExtension("vector");

modelBuilder.Entity<Document>()
    .HasIndex(document => document.Embedding)
    .HasMethod("hnsw")
    .HasOperators("vector_cosine_ops")
    .HasStorageParameter("m", 16)
    .HasStorageParameter("ef_construction", 64);
```

## Enterprise implementation guidance

The package supports HNSW and IVFFlat index metadata. Match `vector_l2_ops`, `vector_ip_ops`, or `vector_cosine_ops` to the distance operation used by the query; otherwise PostgreSQL cannot use that index for the ordered nearest-neighbor scan. Apply and review generated migrations in an environment where pgvector is installed.

### Application guidance: hybrid search and RRF

Neither Pgvector nor Npgsql provides a built-in hybrid-search or reciprocal-rank-fusion API. pgvector recommends combining full-text search with vector search and suggests Reciprocal Rank Fusion (RRF); the following is application-layer policy, not package behavior:

1. Run a full-text candidate query (for example, `ts_rank_cd`) and a vector candidate query with independently measured limits.
2. Assign each document a 1-based rank within each candidate list.
3. Sum `1 / (k + rank)` over the lists in which a document occurs; choose and version `k` with relevance tests.
4. Apply business eligibility filters consistently to both candidate queries.
5. Order final output by fusion score descending, then by a unique document key ascending.

Vector similarity is a candidate generator, not a complete business-ranking policy. RRF candidate limits and `k` are relevance/latency controls and should be evaluated with a labeled query set, not copied as universal constants.

## Integration with the catalog

Use [Pgvector](pgvector.md) for Npgsql type registration, [Npgsql.EntityFrameworkCore.PostgreSQL](npgsql.entityframeworkcore.postgresql.md) for full-text candidate queries, and `MR.EntityFrameworkCore.KeysetPagination` only after final ordering is complete.

Carry the exact final order—including the unique tie-breaker—into the keyset definition; do not paginate a floating vector, lexical, or fusion score alone.

## Security, performance, AOT, trimming, and operations

Apply migrations under a principal allowed to create the extension and index. Assess HNSW/IVFFlat build time, memory, recall, and latency on production-like data. EF translation is provider-dependent, so inspect generated migration SQL and query plans after upgrades.

## Avoid

- Do not present RRF or hybrid search as a capability provided by this package.
- Do not use a ranking score alone as the cursor order.
- Do not deploy an approximate index without measuring recall and query latency.

## Verification checklist

- [ ] The migration declares `vector` and generates the intended HNSW or IVFFlat operator class.
- [ ] Vector, lexical, and final fusion candidate limits are measured separately.
- [ ] Final ranking has a unique tie-breaker before keyset pagination.
- [ ] Relevance tests document the selected RRF `k` and candidate limits.

## Sources

- [pgvector-dotnet EF Core setup and index examples](https://github.com/pgvector/pgvector-dotnet#entity-framework-core)
- [pgvector hybrid-search guidance](https://github.com/pgvector/pgvector#hybrid-search)
- [pgvector HNSW operator classes and tuning](https://github.com/pgvector/pgvector#indexing)
- [Pgvector.EntityFrameworkCore package on NuGet](https://www.nuget.org/packages/Pgvector.EntityFrameworkCore/0.3.0)

Accessed 2026-07-27.
