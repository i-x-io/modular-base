# Ardalis.Specification.EntityFrameworkCore

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Ardalis.Specification.EntityFrameworkCore` | `9.3.1` | EF Core evaluators and repository support for specifications | Cataloged; isolated EF Core 10 compile/evaluator probe passed; PostgreSQL integration unverified |

## Decision and scope

Use this adapter at the EF infrastructure boundary to execute Ardalis specifications. Its NuGet dependency groups target EF Core 8/9; the catalog's EF Core 10 combination has isolated compile evidence but not a project or PostgreSQL support guarantee.

## Recommended registration and use

- Compose a specification on `IQueryable` and execute it before materialization.
- Keep predicates, ordering, projections, and paging server-translatable; inspect SQL for important paths.

## Enterprise implementation guidance

Keep adapter/repository references out of domain code. Pin this package with [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md) and [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md), then update and test them as a set.

## Integration with the catalog

The provider-neutral query model is [Ardalis.Specification](ardalis-specification.md). Use Npgsql for PostgreSQL execution, [EFCore.NamingConventions](efcore-namingconventions.md) before migrations, and [MR.EntityFrameworkCore.KeysetPagination](mr-entityframeworkcore-keysetpagination.md) only with stable ordering.

## Security, performance, AOT, trimming, and operations

Apply authorization/tenant filters before evaluation and retain parameterized LINQ. Avoid client materialization (`AsEnumerable`, `ToList`) before full composition. AOT/trimming safety is unverified.

## Avoid

- Do not treat the isolated probe as proof of production PostgreSQL compatibility.
- Do not hide expensive includes, tracking, or provider-only expressions inside opaque specifications.
- Do not return provider exceptions or EF entities as API contracts.

## Verification checklist

- [ ] Restore and compile with exact 9.3.1/10.0.10 pins; the isolated probe built with zero warnings/errors and returned one evaluated row.
- [ ] Run representative filter/include/projection/paging specifications against PostgreSQL.
- [ ] Confirm translation, SQL shape, authorization scope, and error handling in the consuming application.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [Ardalis.Specification.EntityFrameworkCore 9.3.1 on NuGet](https://www.nuget.org/packages/Ardalis.Specification.EntityFrameworkCore/9.3.1)
- [Ardalis Specification documentation](https://specification.ardalis.com/)
- [EF Core query translation guidance](https://learn.microsoft.com/ef/core/querying/client-eval)
