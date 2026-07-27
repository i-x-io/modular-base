# Microsoft.Extensions.Options.ConfigurationExtensions

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | DI extensions that bind `IConfiguration` sections into options | Approved composition-boundary integration |

## Decision and scope

Use this package to connect a configuration section to the options system through `OptionsBuilder<TOptions>.Bind` and related registration APIs. It complements, but does not replace, options validation or configuration providers.

## Recommended registration and use

At the composition root, bind one explicit section to one options type, then attach validation and `ValidateOnStart`. Prefer `AddOptions<TOptions>().Bind(section)` over scattering `Get<T>` calls. Preserve section names as external configuration contracts rather than deriving them from type names when long-term compatibility matters.

## Enterprise implementation guidance

Centralize registration per feature so section name, options type, validator, and consuming services are visible together. For named options, make the configuration section/name mapping explicit and test every registered name. Document which options support reload and avoid mutable options objects in application code.

## Integration with the catalog

Uses [Configuration.Abstractions](microsoft-extensions-configuration-abstractions.md) and [Configuration.Binder](microsoft-extensions-configuration-binder.md) to configure [Options](microsoft-extensions-options.md). Registration belongs in [DependencyInjection](microsoft-extensions-dependencyinjection.md) / [Hosting](microsoft-extensions-hosting.md) composition.

## Security, performance, AOT, trimming, and operations

Binding still relies on configuration input and may use reflection; validate settings and assess source-generated binding for trimming/AOT-sensitive services. Do not log bound option instances indiscriminately. Monitor configuration reload failures and validate before a changed setting affects an outbound client, storage connection, or security boundary.

## Avoid

- Do not bind a single catch-all application options object.
- Do not assume `Bind` validates required fields or semantic constraints.
- Do not let named option configuration silently fall back to the default name.

## Verification checklist

- Each options section has a stable name, validator, startup policy, and owner.
- Missing, invalid, and reload-time values have tested outcomes.
- Trim/AOT publish validation covers reflection-bound settings, or generated binding is used.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions) (Accessed 2026-07-27)
- [Options pattern in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) (Accessed 2026-07-27)
- [Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) (Accessed 2026-07-27)
