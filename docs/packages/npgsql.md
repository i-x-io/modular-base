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

- Reference the centrally pinned package without repeating its version:

```xml
<ItemGroup>
  <PackageReference Include="Npgsql" />
</ItemGroup>
```

Build one data source for each distinct connection configuration and keep it for the application lifetime:

```csharp
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<Status>("status");
await using var dataSource = dataSourceBuilder.Build();
```

Open short-lived connections only when state must span multiple commands, such as a transaction. Otherwise, execute directly through the data source. Always bind data values as parameters:

```csharp
await using var command = dataSource.CreateCommand(
    "SELECT id, display_name FROM customers WHERE email = $1");
command.Parameters.AddWithValue(email);

await using var reader = await command.ExecuteReaderAsync(cancellationToken);
while (await reader.ReadAsync(cancellationToken))
{
    var id = reader.GetGuid(0);
    var displayName = reader.GetString(1);
    // Map the row before the reader is disposed.
}
```

For an atomic workflow, open one connection, begin one transaction, create commands on that connection, and commit explicitly. Dispose commands, readers, transactions, and connections with `await using`; disposing a pooled connection promptly returns it to the pool.

## Enterprise implementation guidance

Apply database extensions and enum types through migrations where EF owns schema management. For driver-only code, provision them in a deployment/bootstrap step and reload type metadata after creating an extension that introduces types. Use positional placeholders (`$1`, `$2`) for PostgreSQL-native SQL and parameterize values; identifiers such as table or column names cannot be parameters and must come from an allowlist or trusted static SQL.

Connection strings are configuration, not source code. Obtain passwords from the deployment secret provider and require the deployment's approved TLS/certificate policy. Set command timeouts deliberately, pass cancellation tokens, and monitor pool saturation before changing pool sizes. Prepared statements or auto-prepare can improve repeated-query performance, but tune them from measurements rather than enabling broad preparation blindly.

## Integration with the catalog

Use [Npgsql.EntityFrameworkCore.PostgreSQL](npgsql.entityframeworkcore.postgresql.md) for EF Core, [Npgsql.OpenTelemetry](npgsql.opentelemetry.md) for traces, and [Pgvector](pgvector.md) for vector-type registration. If the same enum is used through EF Core, configure the provider-level mapping too; configuring only the driver layer is insufficient with an external data source.

## Security, performance, AOT, trimming, and operations

Keep parameter logging disabled in production. `EnableParameterLogging()` can expose personal data, credentials, or business data in logs. Treat data-source configuration as startup-only application composition, rather than creating a configured data source per request. `NpgsqlDataSource` is thread-safe and normally owns a connection pool; dispose it during application shutdown, not after each command. For trimmed or NativeAOT applications, evaluate `NpgsqlSlimDataSourceBuilder` and opt into only the required features, then publish-test the actual application.

## Avoid

- Do not use global type mapping for new applications; configure the data source instead.
- Do not enable parameter logging in production.
- Do not issue unparameterized SQL containing user data.
- Do not keep a connection or reader open while performing unrelated network or application work.
- Do not create a data source per request; that fragments pools and defeats pooling efficiency.

## Verification checklist

- [ ] The application builds one configured data source per intended connection configuration.
- [ ] Each PostgreSQL enum used by EF is mapped at both required layers.
- [ ] Production logging cannot record parameter values.
- [ ] Commands use parameters, timeouts, cancellation, and async disposal on failure paths.
- [ ] Pool saturation, connection-open failures, command duration, and PostgreSQL limits are observable.
- [ ] TLS, secret rotation, and certificate validation match the deployment policy.

## Sources

- [Npgsql basic usage, parameters, transactions, and pooling](https://www.npgsql.org/doc/basic-usage.html)
- [Npgsql data-source and enum mapping](https://www.npgsql.org/efcore/mapping/enum.html?tabs=with-connection-string%2Cwith-datasource)
- [Npgsql connection-string parameters](https://www.npgsql.org/doc/connection-string-parameters.html)
- [Npgsql logging and parameter-logging warning](https://www.npgsql.org/doc/diagnostics/logging.html)
- [Npgsql security and TLS guidance](https://www.npgsql.org/doc/security.html)
- [Npgsql package on NuGet](https://www.nuget.org/packages/Npgsql/10.0.3)

Accessed 2026-07-27.
