# Microsoft.EntityFrameworkCore

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `Microsoft.EntityFrameworkCore` | `10.0.10` | Core EF Core ORM/runtime surface | Cataloged; repository target framework is `net10.0`; no consuming project exists |

## Decision and scope

Use EF Core 10 as the relational persistence runtime with Npgsql as the PostgreSQL provider. This does not choose aggregate boundaries, repository shape, database schema, or migration deployment policy.

## Recommended registration and use

- Use explicit projections, bounded queries, no-tracking reads where appropriate, cancellation tokens, and generated-SQL review.
- Keep filtering, ordering, and pagination in provider-translated LINQ until execution.

## Enterprise implementation guidance

Pin EF runtime, relational, design, Npgsql, conventions, and exception assets together. Review migrations, apply them via controlled deployment identities, and test provider translations against PostgreSQL rather than a fake provider.

## Integration with the catalog

Relational APIs are [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md); tooling is [Microsoft.EntityFrameworkCore.Design](microsoft-entityframeworkcore-design.md); test-only fake storage is [Microsoft.EntityFrameworkCore.InMemory](microsoft-entityframeworkcore-inmemory.md). Query abstractions belong in [Ardalis.Specification](ardalis-specification.md).

## Security, performance, AOT, trimming, and operations

Parameterize values through LINQ/EF APIs, redact connection/provider details, retain least privilege, and avoid N+1/unbounded loading. EF NativeAOT/precompiled-query support is experimental and provider support must be proven by publishing and exercising the application.

## Avoid

- Do not expose DbContext/EF entities as public API contracts.
- Do not concatenate untrusted raw SQL.
- Do not run production migrations at startup with a broadly privileged application identity.

## Verification checklist

- [ ] Restore and compile the consuming `net10.0` project with 10.0.10.
- [ ] Run PostgreSQL integration tests for queries, constraints, transactions, and migrations.
- [ ] Inspect SQL/plans for critical query paths and publish-test any AOT/trimming proposal.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [Microsoft.EntityFrameworkCore 10.0.10 on NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.10)
- [EF Core overview](https://learn.microsoft.com/ef/core/)
- [EF Core performance guidance](https://learn.microsoft.com/ef/core/performance/)
