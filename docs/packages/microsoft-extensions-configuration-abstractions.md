# Microsoft.Extensions.Configuration.Abstractions

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Configuration contracts such as `IConfiguration` and `IConfigurationSection` | Approved foundation abstraction |

## Decision and scope

Use these contracts at application boundaries to consume hierarchical configuration independent of its provider. This is an abstraction package, not a provider or object binder. Providers and host builders supply the configuration root; consumers should normally receive a narrowly scoped options type instead.

## Recommended registration and use

Build configuration in the host/composition root, then read only the required section. Prefer [Options](microsoft-extensions-options.md) for grouped settings and reserve direct `IConfiguration` injection for composition, dynamic sections, or infrastructure that cannot express a stable options contract.

## Enterprise implementation guidance

Make provider precedence explicit and document which source is authoritative for each setting. Keep environment-specific values outside source control, use secret stores for credentials, and give every operational setting an owner, safe default, and reload expectation.

## Integration with the catalog

[Configuration.Binder](microsoft-extensions-configuration-binder.md) turns sections into objects. [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md) binds sections into DI-managed options. [Hosting](microsoft-extensions-hosting.md) provides the standard application configuration pipeline.

## Security, performance, AOT, trimming, and operations

Configuration is untrusted operational input: validate it before use and do not log secret values. Configuration reload can change behavior while the process is running; choose `IOptionsMonitor<TOptions>` only when reload is intended and safe. The abstractions themselves have no reflection binding requirement; binder-based APIs have trimming/AOT considerations.

## Avoid

- Do not pass `IConfiguration` deeply through business code as a service locator.
- Do not rely on source ordering accidentally or place secrets in checked-in JSON files.
- Do not treat an existing key as a valid value.

## Verification checklist

- Provider order and environment overrides are intentional.
- Required values and secret handling are validated at startup.
- Reload behavior is tested or explicitly disabled for each affected setting.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Abstractions) (Accessed 2026-07-27)
- [Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) (Accessed 2026-07-27)
