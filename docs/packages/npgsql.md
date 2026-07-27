# Npgsql

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `Npgsql` |
| Pinned version | `10.0.3` |
| Status | Approved catalog dependency |
| Role | PostgreSQL ADO.NET driver and configured `NpgsqlDataSource` factory |

## Decision and scope

Use `NpgsqlDataSource` as the application-owned, long-lived PostgreSQL entry point. Configure mappings and plugins on its builder before building it; connections and commands then inherit that configuration. This package owns lower-level PostgreSQL type mappings, including enums and pgvector registration.

## Recommended registration and use

```csharp
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<Status>("status");
await using var dataSource = dataSourceBuilder.Build();
```

## Enterprise implementation guidance

Apply database extensions and enum types through migrations where EF owns schema management. For driver-only code, provision them explicitly and reload type metadata after creating an extension that introduces types. Use parameterized commands; do not interpolate user values into SQL.

## Integration with the catalog

Use [Npgsql.EntityFrameworkCore.PostgreSQL](npgsql.entityframeworkcore.postgresql.md) for EF Core, [Npgsql.OpenTelemetry](npgsql.opentelemetry.md) for traces, and [Pgvector](pgvector.md) for vector-type registration. If the same enum is used through EF Core, configure the provider-level mapping too; configuring only the driver layer is insufficient with an external data source.

## Security, performance, AOT, trimming, and operations

Keep parameter logging disabled in production. `EnableParameterLogging()` can expose personal data, credentials, or business data in logs. Treat data-source configuration as startup-only application composition, rather than creating a configured data source per request.

## Avoid

- Do not use global type mapping for new applications; configure the data source instead.
- Do not enable parameter logging in production.
- Do not issue unparameterized SQL containing user data.

## Verification checklist

- [ ] The application builds one configured data source per intended connection configuration.
- [ ] Each PostgreSQL enum used by EF is mapped at both required layers.
- [ ] Production logging cannot record parameter values.

## Sources

- [Npgsql data-source and enum mapping](https://www.npgsql.org/efcore/mapping/enum.html?tabs=with-connection-string%2Cwith-datasource)
- [Npgsql logging and parameter-logging warning](https://www.npgsql.org/doc/diagnostics/logging.html)
- [Npgsql package on NuGet](https://www.nuget.org/packages/Npgsql/10.0.3)

Accessed 2026-07-27.
