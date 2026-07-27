# EntityFrameworkCore.Exceptions.PostgreSQL

## Catalog entry

| Package | Exact version | Role | Status |
| --- | ---: | --- | --- |
| `EntityFrameworkCore.Exceptions.PostgreSQL` | `10.0.1` | PostgreSQL-aware classification of EF Core database update errors | Cataloged; no persistence error boundary compiled |

## Decision and scope

Use this package only in data-access infrastructure to classify database constraint failures. It does not define domain errors or the public HTTP/API error contract.

## Recommended registration and use

- Map known uniqueness, null, length, numeric-overflow, reference, and constraint failures to stable application error codes at the infrastructure/application boundary.
- Preserve original exceptions for protected diagnostics while returning generic conflict or validation information externally.

## Enterprise implementation guidance

Keep constraints authoritative: a preflight uniqueness query may improve a message but cannot prevent races. Configure exception processing in the DbContext composition root and test actual PostgreSQL failures.

## Integration with the catalog

Use with [Microsoft.EntityFrameworkCore](microsoft-entityframeworkcore.md), [Microsoft.EntityFrameworkCore.Relational](microsoft-entityframeworkcore-relational.md), and the cataloged Npgsql provider. Keep resulting application errors independent of [Ardalis specifications](ardalis-specification.md).

## Security, performance, AOT, trimming, and operations

Never disclose SQL, constraint/schema names, connection strings, or provider stack traces to clients. Log protected correlation data and retain least-privilege database identities. AOT/trimming compatibility is unverified.

## Avoid

- Do not catch classified exceptions in domain entities or expose them from APIs.
- Do not replace database constraints with application-side prechecks.
- Do not map every database failure to a client validation response.

## Verification checklist

- [ ] Compile exact 10.0.1 assets with EF Core/Npgsql in a consuming project.
- [ ] Trigger unique, foreign-key/reference, null, length, check, and numeric failures against PostgreSQL.
- [ ] Assert stable public errors and absence of provider details in responses/log exports.

## Sources

Accessed 2026-07-27.

- [Central package catalog](../../Directory.Packages.props)
- [EntityFrameworkCore.Exceptions.PostgreSQL 10.0.1 on NuGet](https://www.nuget.org/packages/EntityFrameworkCore.Exceptions.PostgreSQL/10.0.1)
- [EntityFrameworkCore.Exceptions repository](https://github.com/Giorgi/EntityFramework.Exceptions)
- [EF Core error handling and retry guidance](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency)
