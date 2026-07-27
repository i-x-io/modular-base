# MR.EntityFrameworkCore.KeysetPagination

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `MR.EntityFrameworkCore.KeysetPagination` | `1.6.0` | Keyset/seek pagination helpers for EF Core queries | Cataloged; no consuming query integration compiled |

## Decision and scope

Use keyset pagination for forward/backward traversal of large, changing, ordered result sets. It does not supply authorization, a public cursor format, snapshot semantics, or a substitute for an index.

## Recommended registration and use

- Define total ordering: business sort key(s) followed by an immutable unique tiebreaker, usually the primary key.
- Retain the same filters and ordering for every request; apply authorization and tenant scope before the seek predicate.

## Enterprise implementation guidance

Make cursors opaque and validate ordering values, direction, sort definition, scope, and bounded page size. Protect/sign cursors if tampering could cross a query scope. Create indexes aligned to filter/scope and ordered keys, then inspect PostgreSQL plans.

## Integration with the catalog

Compose pagination on [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md) queries or [Ardalis specifications](ardalis-specification.md) before execution. Test PostgreSQL translation with [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md); InMemory is not acceptable evidence.

## Security, performance, AOT, trimming, and operations

Cursors are not authorization grants and must not expose database internals. Keyset pagination avoids offset traversal instability as preceding rows shift, but it does not provide an immutable snapshot by itself. AOT/trimming compatibility is unverified.

## Avoid

- Do not use a non-unique sort key alone or change sort/filter scope between requests.
- Do not accept arbitrary cursor values without validation/protection.
- Do not substitute offset pagination for high-churn feeds that require deterministic traversal.

## Verification checklist

- [ ] Compile the exact 1.6.0 package with EF Core/Npgsql in a consuming project.
- [ ] Test duplicate/null keys, inserts/deletes between requests, next/previous navigation, malformed cursors, and tenant/auth scope.
- [ ] Inspect PostgreSQL SQL and plans with the supporting index.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [MR.EntityFrameworkCore.KeysetPagination 1.6.0 on NuGet](https://www.nuget.org/packages/MR.EntityFrameworkCore.KeysetPagination/1.6.0)
- [MR.EntityFrameworkCore.KeysetPagination repository](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination)
- [EF Core pagination guidance](https://learn.microsoft.com/ef/core/querying/pagination)
