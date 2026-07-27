# Microsoft.Extensions.Options.ConfigurationExtensions

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | DI extensions that bind `IConfiguration` sections into options | Approved composition-boundary integration |

## Decision and scope

Use this package to connect a configuration section to the options system through `OptionsBuilder<TOptions>.Bind` and related registration APIs. It complements, but does not replace, options validation or configuration providers.

## Recommended registration and use

At the composition root, bind one explicit section to one options type, then attach validation and `ValidateOnStart`. Prefer `AddOptions<TOptions>().Bind(section)` over scattering `Get<T>` calls. Preserve section names as external configuration contracts rather than deriving them from type names when long-term compatibility matters.

Reference the centrally pinned integration package without a version:

```xml
<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
```

Bind, validate, and choose startup behavior in one visible registration chain:

```csharp
using Microsoft.Extensions.Options;

builder.Services
    .AddOptions<OutboundApiOptions>()
    .Bind(builder.Configuration.GetRequiredSection("OutboundApi"),
        binder => binder.ErrorOnUnknownConfiguration = true)
    .Validate(options => Uri.TryCreate(
            options.BaseUrl, UriKind.Absolute, out _),
        "OutboundApi:BaseUrl must be an absolute URI.")
    .Validate(options => options.Timeout is > 0 and <= 30,
        "OutboundApi:Timeout must be 1..30 seconds.")
    .ValidateOnStart();

public sealed class OutboundApiOptions
{
    public string BaseUrl { get; set; } = "";
    public int Timeout { get; set; }
}
```

`Bind` also registers a configuration change-token source. Reload only occurs when the underlying provider supports and enables reload; consumers must use `IOptionsSnapshot<T>` or `IOptionsMonitor<T>` to observe updated instances.

## Enterprise implementation guidance

- Centralize registration per feature so section name, options type, binder strictness, validators, startup policy, and consuming services are visible together.
- Prefer `GetRequiredSection` for required configuration. Remember that it checks section existence, not whether every property is populated or semantically valid.
- For named options, use `AddOptions<T>(name).Bind(section)` so the name-to-section mapping is explicit; names are case-sensitive and each one needs validation coverage.
- Treat each resolved options instance as read-only application input. Do not mutate it after resolution or retain stale nested references across reloads.

## Integration with the catalog

Uses [Configuration.Abstractions](microsoft-extensions-configuration-abstractions.md) and [Configuration.Binder](microsoft-extensions-configuration-binder.md) to configure [Options](microsoft-extensions-options.md). Registration belongs in [DependencyInjection](microsoft-extensions-dependencyinjection.md) / [Hosting](microsoft-extensions-hosting.md) composition.

## Security, performance, AOT, trimming, and operations

Binding still relies on untrusted configuration input and may use reflection. Enable the configuration binding source generator for trimming/AOT-sensitive services, use generated options validators where appropriate, and publish/run the deployment artifact. This integration does not store or protect secrets: source them from an approved provider and redact bound objects and validation messages. Reload callbacks can cause partial operational transitions; rebuild dependent clients atomically, retain the last known-good resource when policy allows, and emit a sanitized failure signal when an updated value cannot be applied.

## Avoid

- Do not bind a single catch-all application options object.
- Do not assume `Bind` validates required fields or semantic constraints.
- Do not let named option configuration silently fall back to the default name.

## Verification checklist

- [ ] The consuming project references the package without a version; central package management supplies `10.0.10`.
- [ ] Each section has a stable name, owner, strictness decision, validator, and startup policy; every named instance is tested.
- [ ] Missing sections, unknown keys according to policy, invalid values, valid reloads, and rejected reloads have observed outcomes.
- [ ] The exact trimmed or Native AOT artifact is published and run; generated binding/validation is enabled where required.

## Sources

- [NuGet: Microsoft.Extensions.Options.ConfigurationExtensions 10.0.10](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions/10.0.10) — Accessed 2026-07-27.
- [Microsoft Learn: Options pattern in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) — Accessed 2026-07-27.
- [Microsoft Learn: Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) — Accessed 2026-07-27.
- [Microsoft Learn: Compile-time configuration source generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-generator) — Accessed 2026-07-27.
