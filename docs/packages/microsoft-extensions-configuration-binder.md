# Microsoft.Extensions.Configuration.Binder

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Binds `IConfiguration` sections to typed objects | Approved at composition boundaries |

## Decision and scope

Use the binder to map hierarchical configuration into a typed configuration or options object. It complements `Configuration.Abstractions`; it neither validates values nor secures configuration sources. Bind at the composition boundary, then expose an options interface or a validated immutable value to application code.

## Recommended registration and use

For DI-managed settings, bind with [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md) and validate on start. Use `Get<T>` or `Bind` only for short-lived composition work where the object does not need options lifecycle services. Keep options types small, public/settable as required by the binder, and free of behavior.

## Enterprise implementation guidance

Name configuration sections independently from CLR types so section names remain stable during refactoring. Validate required values, ranges, cross-field constraints, and nested objects. Treat unknown/unused settings as a governance concern: audit them rather than silently accepting configuration drift.

## Integration with the catalog

Consumes [Configuration.Abstractions](microsoft-extensions-configuration-abstractions.md) and commonly feeds [Options](microsoft-extensions-options.md). [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md) supplies the DI integration.

## Security, performance, AOT, trimming, and operations

The reflection binder can generate trimming/AOT warnings because member discovery occurs at runtime. Prefer the .NET configuration binding source generator where its constraints fit, keep bound types explicit, and publish/test the trimmed or native deployment mode that will run. Binding is not validation; fail startup before a malformed endpoint, credential, or limit reaches a client.

## Avoid

- Do not bind arbitrary user-controlled configuration into security-sensitive types.
- Do not call the binder repeatedly on hot paths.
- Do not equate successful binding with a complete or valid configuration.

## Verification checklist

- The section name, required values, and nested object rules are covered by tests.
- Validation rejects malformed and cross-field-invalid values before service use.
- AOT/trimming publish validation covers every reflection-bound type, or source generation is enabled.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Binder) (Accessed 2026-07-27)
- [Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) (Accessed 2026-07-27)
- [Configuration binding source generator](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-generator) (Accessed 2026-07-27)
