# Microsoft.Extensions.Options

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Options contracts, factories, caching, validation, and `IOptions*` accessors | Approved typed-configuration foundation |

## Decision and scope

Use options for strongly typed, scenario-specific application settings. This package owns the options lifecycle and validation contracts; binding configuration sections requires [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md).

## Recommended registration and use

Use `IOptions<T>` for stable singleton-readable values, `IOptionsSnapshot<T>` for scoped values, and `IOptionsMonitor<T>` for singleton consumers that intentionally support reload. Use named options only when multiple instances of the same settings shape are genuinely needed. Validate every required options type with `Validate`, `IValidateOptions<T>`, or generated/data-annotation validation and call `ValidateOnStart` for startup-critical configuration.

## Enterprise implementation guidance

Create one small options class per capability, not a global configuration object. Put basic shape/range validation next to the type and cross-field/external-rule validation in `IValidateOptions<T>`. Validation must cover each name where named options are used. Define reload semantics: which settings may change, how consumers observe changes, and how invalid reload values are handled.

## Integration with the catalog

[Configuration.Abstractions](microsoft-extensions-configuration-abstractions.md) supplies settings, [Configuration.Binder](microsoft-extensions-configuration-binder.md) maps them, and [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md) wires them into DI. [DependencyInjection](microsoft-extensions-dependencyinjection.md) registers the services.

## Security, performance, AOT, trimming, and operations

Options are configuration, not secret storage. Never log complete option objects. `IOptionsSnapshot<T>` is scoped and should not enter singleton dependencies; `IOptionsMonitor<T>` callbacks must be thread-safe and resilient to repeated updates. Binder/data-annotation reflection can affect trimming/AOT; prefer source generators where applicable and test the deployment artifact.

## Avoid

- Do not inject `IOptionsSnapshot<T>` into singleton services.
- Do not omit `ValidateOnStart` for configuration required to safely start.
- Do not use named options as a substitute for an explicit client/service abstraction.

## Verification checklist

- Each startup-critical options type validates missing, malformed, and cross-field-invalid values at startup.
- Named options validate every configured name.
- Reload behavior and invalid-update handling are tested, or settings are intentionally static.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Options) (Accessed 2026-07-27)
- [Options pattern in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) (Accessed 2026-07-27)
- [Options validation source generator](https://learn.microsoft.com/en-us/dotnet/core/extensions/options-validation-generator) (Accessed 2026-07-27)
