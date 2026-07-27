# MR.EntityFrameworkCore.KeysetPagination

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `MR.EntityFrameworkCore.KeysetPagination` | `1.6.0` | Keyset/seek pagination helpers for EF Core queries | Cataloged; no consuming query integration compiled |

## Decision and scope

Use keyset pagination for forward/backward traversal of large, changing, ordered result sets. It does not supply authorization, a public cursor format, snapshot semantics, or a substitute for an index.

## Recommended registration and use

- Reference the centrally pinned package without a version in the consuming project:

```xml
<ItemGroup>
  <PackageReference Include="MR.EntityFrameworkCore.KeysetPagination" />
</ItemGroup>
```

- Define total ordering: business sort key(s) followed by an immutable unique tiebreaker, usually the primary key.
- Retain the same filters and ordering for every request; apply authorization and tenant scope before the seek predicate.

Prebuild frequently used definitions so the package can reuse its internal caches. The example below serves the first, next, previous, and last page by changing `direction` and `reference`:

```csharp
using Microsoft.EntityFrameworkCore;
using MR.EntityFrameworkCore.KeysetPagination;

private static readonly KeysetQueryDefinition<Post> FeedOrder =
    KeysetQuery.Build<Post>(builder => builder
        .Descending(post => post.PublishedAt)
        .Descending(post => post.Id));

var keyset = dbContext.Posts
    .AsNoTracking()
    .Where(post => post.TenantId == tenantId && post.IsPublished)
    .KeysetPaginate(FeedOrder, direction, reference);

var posts = await keyset.Query
    .Take(Math.Clamp(pageSize, 1, 100))
    .ToListAsync(cancellationToken);

// Backward queries are produced in reverse traversal order.
keyset.EnsureCorrectOrder(posts);

var hasPrevious = await keyset.HasPreviousAsync(posts);
var hasNext = await keyset.HasNextAsync(posts);
```

Use the first item of the current page as the `Backward` reference and the last item as the `Forward` reference. Omit the reference for the first page; use `Backward` with no reference for the last page. A projection used as the reference must retain properties with names matching every configured keyset property.

## Enterprise implementation guidance

Make cursors opaque and validate ordering values, direction, sort definition, scope, and bounded page size. Protect/sign cursors if tampering could cross a query scope. The package accepts a reference object; serialization, signing, expiry, and versioning of a public cursor are application responsibilities.

Calculate a total count from the filtered base query before calling `KeysetPaginate`, because pagination adds ordering and seek predicates. Create a composite index aligned to the stable filters and keyset order, for example `(tenant_id, published_at DESC, id DESC)`, then inspect PostgreSQL plans with representative values. A prebuilt definition should be a long-lived immutable field, not rebuilt per request.

## Integration with the catalog

Compose pagination on [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md) queries or [Ardalis specifications](ardalis-specification.md) before execution. Test PostgreSQL translation with [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md); InMemory is not acceptable evidence.

## Security, performance, AOT, trimming, and operations

Cursors are not authorization grants and must not expose database internals. Reapply tenant, authorization, and soft-delete predicates before pagination even when those values are protected inside the cursor. Keyset pagination avoids offset traversal instability as preceding rows shift, but it does not provide an immutable snapshot by itself; changes to ordered values can still move rows between pages. AOT/trimming compatibility is unverified.

## Avoid

- Do not use a non-unique sort key alone or change sort/filter scope between requests.
- Do not accept arbitrary cursor values without validation/protection.
- Do not substitute offset pagination for high-churn feeds that require deterministic traversal.
- Do not forget `EnsureCorrectOrder` after a backward query.
- Do not apply a new `OrderBy` after `KeysetPaginate`; the keyset definition is the traversal contract.

## Verification checklist

- [ ] Compile the exact 1.6.0 package with EF Core/Npgsql in a consuming project.
- [ ] Test duplicate/null keys, inserts/deletes between requests, next/previous navigation, malformed cursors, and tenant/auth scope.
- [ ] Inspect PostgreSQL SQL and plans with the supporting index.
- [ ] Verify projected references retain every keyset property name and value.
- [ ] Verify the bounded page size and cursor version/signature before building the query.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [MR.EntityFrameworkCore.KeysetPagination 1.6.0 on NuGet](https://www.nuget.org/packages/MR.EntityFrameworkCore.KeysetPagination/1.6.0)
- [MR.EntityFrameworkCore.KeysetPagination repository](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination)
- [Version 1.6.0 source tag](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/tree/v1.6.0)
- [EF Core pagination guidance](https://learn.microsoft.com/ef/core/querying/pagination)
