# Ardalis.Specification

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Ardalis.Specification` | `9.3.1` | Provider-agnostic model for reusable query specifications | Cataloged; isolated EF Core 10 compile probe passed; project/PostgreSQL integration unverified |

## Decision and scope

Use this package to express bounded, reusable query intent. It does not establish a repository abstraction or make EF Core 10/PostgreSQL behavior supported without consuming-project tests.

## Recommended registration and use

- Name small specifications after a business query and keep filters, ordering, includes, projection, and paging explicit.
- Keep specifications provider-neutral where practical; put EF execution in infrastructure.

## Enterprise implementation guidance

Review each specification as query code. Prefer read-model projections and no-tracking reads when mutation is not intended; limit includes and page sizes.

## Integration with the catalog

Use [Ardalis.Specification.EntityFrameworkCore](ardalis-specification-entityframeworkcore.md) only in EF infrastructure. Align execution with [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md), the Npgsql provider catalog entry, and [MR.EntityFrameworkCore.KeysetPagination](mr-entityframeworkcore-keysetpagination.md) for seek paging.

## Security, performance, AOT, trimming, and operations

Keep authorization/tenant predicates in the composed server query. Inspect SQL and plans for high-volume specifications. AOT/trimming compatibility is unverified until a real published application exercises its query paths.

## Avoid

- Do not use a specification as an arbitrary client-filter transport.
- Do not expose unrestricted `IQueryable` or persistence entities from application boundaries.
- Do not assume its NuGet target-framework compatibility proves provider translation.

## Verification checklist

- [ ] Compile the consuming `net10.0` project with the exact catalog pin.
- [ ] Execute representative specifications against PostgreSQL and inspect generated SQL.
- [ ] Test authorization, tenant scope, projections, tracking, includes, and pagination.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [Ardalis.Specification 9.3.1 on NuGet](https://www.nuget.org/packages/Ardalis.Specification/9.3.1)
- [Ardalis Specification documentation](https://specification.ardalis.com/)
