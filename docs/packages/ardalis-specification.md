# Ardalis.Specification

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Ardalis.Specification` | `9.3.1` | Provider-agnostic model for reusable query specifications | Cataloged; isolated EF Core 10 compile probe passed; project/PostgreSQL integration unverified |

## Decision and scope

Use this package to express bounded, reusable query intent. It does not establish a repository abstraction or make EF Core 10/PostgreSQL behavior supported without consuming-project tests.

## Recommended registration and use

Reference the centrally pinned package without a version:

```xml
<ItemGroup>
  <PackageReference Include="Ardalis.Specification" />
</ItemGroup>
```

Name small specifications after a business query and keep filters, ordering, projection, and paging explicit. For example, this specification describes a bounded read model without choosing an executor:

```csharp
using Ardalis.Specification;

public sealed record OpenOrderSummary(Guid Id, string Number, decimal Total);

public sealed class OpenOrdersForCustomerSpec
    : Specification<Order, OpenOrderSummary>
{
    public OpenOrdersForCustomerSpec(Guid customerId, int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        Query.Where(order => order.CustomerId == customerId && order.Status == OrderStatus.Open)
            .OrderByDescending(order => order.CreatedAt)
            .Take(Math.Min(take, 100))
            .Select(order => new(order.Id, order.Number, order.Total));
    }
}
```

Keep the specification provider-neutral where practical. The application creates it; EF infrastructure evaluates it with the companion adapter. Use `Specification<T, TResult>` for projections so callers receive a read model rather than persistence entities.

## Enterprise implementation guidance

Treat each specification as production query code:

1. Encode mandatory tenant/authorization predicates in a trusted specification or in an infrastructure policy that cannot be bypassed.
2. Add deterministic ordering before `Skip`/`Take`; cap page size at the boundary.
3. Prefer projections for read paths. Add includes only when the result really needs full related entities.
4. Unit-test the specification's intent, then integration-test translation and SQL through its real provider.

Do not attempt to merge arbitrary specifications. The upstream FAQ notes that includes, ordering, paging, projections, caching, and post-processing can conflict; create one explicit specification for the workflow instead. Cache tags are metadata for a cache-capable repository or decorator, not a cache implementation supplied by the core package.

## Integration with the catalog

Use [Ardalis.Specification.EntityFrameworkCore](ardalis-specification-entityframeworkcore.md) only in EF infrastructure. Align execution with [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md), the Npgsql provider catalog entry, and [MR.EntityFrameworkCore.KeysetPagination](mr-entityframeworkcore-keysetpagination.md) for seek paging.

## Security, performance, AOT, trimming, and operations

Keep authorization and tenant predicates in the composed server query; never accept arbitrary expressions, property names, include paths, or page sizes directly from a client. Log a stable specification name and timing rather than sensitive parameter values.

Projections reduce materialization and over-fetching, while bounded deterministic paging protects memory and latency. Inspect generated SQL and query plans for high-volume specifications and index their filter/order shape. Dynamic specification composition can prevent EF query precompilation; EF NativeAOT/query precompilation remains experimental, so AOT/trimming compatibility is unverified until the actual provider and published application exercise every important query path.

## Avoid

- Do not use a specification as an arbitrary client-filter transport.
- Do not expose unrestricted `IQueryable` or persistence entities from application boundaries.
- Do not assume its NuGet target-framework compatibility proves provider translation.
- Do not add an include to a projection unless provider behavior and generated SQL show that it is required.

## Verification checklist

- [ ] Compile the consuming `net10.0` project with the exact catalog pin.
- [ ] Unit-test criteria, ordering, projection, and page-size boundaries.
- [ ] Execute representative specifications against PostgreSQL and inspect generated SQL.
- [ ] Test authorization, tenant scope, projections, tracking, includes, and pagination.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [Ardalis.Specification 9.3.1 on NuGet](https://www.nuget.org/packages/Ardalis.Specification/9.3.1)
- [Ardalis Specification documentation](https://specification.ardalis.com/)
- [Creating specifications and projections](https://specification.ardalis.com/usage/create-specifications.html)
- [Ardalis Specification features](https://specification.ardalis.com/features/)
- [Ardalis Specification FAQ](https://specification.ardalis.com/getting-started/faq.html)
