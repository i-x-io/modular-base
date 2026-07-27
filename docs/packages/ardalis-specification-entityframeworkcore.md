# Ardalis.Specification.EntityFrameworkCore

> **Owner:** `IX` · **Last reviewed:** `2026-07-27` · **Review trigger:** This package, Ardalis.Specification, EF Core, Npgsql, or target-framework change.

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Ardalis.Specification.EntityFrameworkCore` | `9.3.1` | EF Core evaluators and repository support for specifications | Cataloged; isolated EF Core 10 compile/evaluator probe passed; PostgreSQL integration unverified |

## Decision and scope

Use this adapter at the EF infrastructure boundary to execute Ardalis specifications. Its NuGet dependency groups target EF Core 8/9; the catalog's EF Core 10 combination has isolated compile evidence but not a project or PostgreSQL support guarantee.

## Recommended registration and use

Reference the EF adapter only from infrastructure:

```xml
<ItemGroup>
  <PackageReference Include="Ardalis.Specification.EntityFrameworkCore" />
</ItemGroup>
```

The application owns the specification; infrastructure applies it to the `DbSet` and materializes exactly once:

```csharp
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public sealed class OrderQueries(AppDbContext db)
{
    public Task<List<OpenOrderSummary>> ListAsync(
        ISpecification<Order, OpenOrderSummary> specification,
        CancellationToken cancellationToken)
    {
        IQueryable<OpenOrderSummary> query = SpecificationEvaluator.Default
            .GetQuery(db.Set<Order>().AsQueryable(), specification);

        return query.ToListAsync(cancellationToken);
    }
}
```

Alternatively, derive an infrastructure repository from the package's `RepositoryBase<T>` and expose only bounded operations needed by the application. Keep predicates, ordering, projections, and paging server-translatable; call `ToQueryString()` on a non-sensitive diagnostic path when reviewing important SQL.

## Enterprise implementation guidance

Keep adapter/repository references out of domain code. Pin this package with [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md) and [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md), then update and test them as a set.

A common read workflow is:

1. Validate and normalize the caller's filter/page request.
2. Construct one named, bounded specification.
3. Evaluate it against the EF query root.
4. Materialize asynchronously with the request cancellation token.
5. Measure the query and inspect SQL/plans for hot paths.

Use `AsNoTracking()`/the specification tracking feature for read-only entity results; projections are usually preferable. Use `AsSplitQuery()` only after measuring cartesian expansion from multiple collection includes, because it adds database round trips and consistency tradeoffs. Apply `IgnoreQueryFilters()` only in a tightly authorized administrative workflow.

### Upgrade and rollback

Treat `Ardalis.Specification.EntityFrameworkCore` `9.3.1`, the core specification package, EF Core, and Npgsql as one tested compatibility set. The adapter's declared EF dependency range does not by itself certify this catalog's EF Core 10 combination, so compile custom evaluators/repositories and run PostgreSQL translation tests before promotion. There is no adapter-owned schema change. Roll back the application and both Ardalis pins together; if an EF/provider upgrade also emitted migrations, follow that migration's separately rehearsed database recovery plan.

## Integration with the catalog

The provider-neutral query model is [Ardalis.Specification](ardalis-specification.md). Use Npgsql for PostgreSQL execution, [EFCore.NamingConventions](efcore-namingconventions.md) before migrations, and [MR.EntityFrameworkCore.KeysetPagination](mr-entityframeworkcore-keysetpagination.md) only with stable ordering. See [PostgreSQL data-access selection](../package-guidance/package-selection.md#postgresql-data-access), the [EF Core/PostgreSQL recipe](../recipes/efcore-npgsql-exception-mapping.md), and the [supply-chain entry](../package-guidance/supply-chain.md#ardalis-specification-entityframeworkcore).

## Security, performance, AOT, trimming, and operations

Apply authorization/tenant filters before evaluation and retain parameterized LINQ. Avoid client materialization (`AsEnumerable`, `ToList`) before full composition. Never expose an unrestricted specification or arbitrary expression builder to an untrusted client.

Track query count, latency, returned rows, and timeouts by stable operation name. Cap includes and result size; verify indexes match the final predicate and ordering. Dynamic specifications may not be eligible for EF precompiled-query interception, and provider support is required; AOT/trimming safety remains unverified until publish and runtime tests pass.

## Avoid

- Do not treat the isolated probe as proof of production PostgreSQL compatibility.
- Do not hide expensive includes, tracking, or provider-only expressions inside opaque specifications.
- Do not return provider exceptions or EF entities as API contracts.
- Do not evaluate the same paged specification with `CountAsync` without confirming that paging is ignored or using a separate count specification.

## Verification checklist

- [ ] Restore and compile with exact 9.3.1/10.0.10 pins; the isolated probe built with zero warnings/errors and returned one evaluated row.
- [ ] Exercise direct `SpecificationEvaluator` and any repository wrapper with cancellation.
- [ ] Run representative filter/include/projection/paging specifications against PostgreSQL.
- [ ] Confirm translation, SQL shape, authorization scope, and error handling in the consuming application.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [Ardalis.Specification.EntityFrameworkCore 9.3.1 on NuGet](https://www.nuget.org/packages/Ardalis.Specification.EntityFrameworkCore/9.3.1)
- [Ardalis Specification documentation](https://specification.ardalis.com/)
- [Specification repository-pattern integration](https://specification.ardalis.com/usage/use-specification-repository-pattern.html)
- [Specification Include feature](https://specification.ardalis.com/features/include.html)
- [Specification tracking feature](https://specification.ardalis.com/features/astracking.html)
- [EF Core query translation guidance](https://learn.microsoft.com/ef/core/querying/client-eval)
