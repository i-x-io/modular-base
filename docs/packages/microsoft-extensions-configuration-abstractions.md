# Microsoft.Extensions.Configuration.Abstractions

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Configuration contracts such as `IConfiguration` and `IConfigurationSection` | Approved foundation abstraction |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, configuration-provider precedence, or platform configuration change |

## Decision and scope

Use these contracts at application boundaries to consume hierarchical configuration independent of its provider. This is an abstraction package, not a provider or object binder. Providers and host builders supply the configuration root; consumers should normally receive a narrowly scoped options type instead.

## Recommended registration and use

Build configuration in the host/composition root, then read only the required section. Prefer [Options](microsoft-extensions-options.md) for grouped settings and reserve direct `IConfiguration` injection for composition, dynamic sections, or infrastructure that cannot express a stable options contract.

Class libraries that expose or consume these contracts directly should reference the centrally pinned package without repeating its version:

```xml
<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" />
```

Use `GetRequiredSection` to fail when an entire section is absent, then parse and validate individual values deliberately. The indexer returns strings and does not perform typed binding:

```csharp
using Microsoft.Extensions.Configuration;

public sealed class PaymentEndpoint(IConfiguration configuration)
{
    private readonly IConfigurationSection _section =
        configuration.GetRequiredSection("Payments");

    public Uri BaseAddress => Uri.TryCreate(
        _section["BaseUrl"], UriKind.Absolute, out Uri? value)
            ? value
            : throw new InvalidOperationException(
                "Payments:BaseUrl must be an absolute URI.");
}
```

This package supplies contracts and section/key access only. It does not add JSON, environment-variable, secret-store, or other providers, and it does not bind sections to objects.

### Configuration contract reference

| Concern | Purpose | Default behavior | Production guidance | Reload | Sensitivity | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| Provider order | Resolves duplicate keys | Later providers normally win | Document the authoritative source and override order | Provider-specific | Can select secret-bearing values | Wrong order silently selects an unintended value |
| Required section/key | Defines presence requirements | Indexer returns `null` for missing keys | Use `GetRequiredSection`, then parse and validate leaves | New reads observe provider state | Key names can disclose topology | Missing required section throws; missing indexer key returns `null` |
| Reload support | Announces provider changes | Not guaranteed by the abstraction | Enable only for settings with safe application semantics | Provider/change-token-specific | Reload callbacks must not log values | Consumers may remain stale when the provider cannot reload |

## Enterprise implementation guidance

- Make provider precedence explicit: providers added later normally override values from earlier providers for the same key. Document which source is authoritative for each setting.
- Keep environment-specific values outside source control and obtain credentials from an approved secret provider. Configuration APIs transport secret values but are not a secret store.
- Use colon-delimited keys (`Feature:Limit`) as the portable hierarchy contract; environment-variable providers commonly map `__` to `:`.
- Give every operational setting an owner, safe default, validation rule, and reload expectation. Treat missing and empty values as different states when the domain does.

### Upgrade and rollback

Keep this abstraction aligned with the host and concrete configuration packages used by the application. On upgrade, compile reusable libraries against the new contracts and exercise provider precedence, missing-section, reload-token, and environment-override behavior. No data migration is required. Roll back the package set together if a contract or provider interaction regresses; configuration source contents remain unchanged.

## Integration with the catalog

[Configuration.Binder](microsoft-extensions-configuration-binder.md) turns sections into objects. [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md) binds sections into DI-managed options. [Hosting](microsoft-extensions-hosting.md) provides the standard application configuration pipeline.

Use the [abstraction-versus-runtime selection guide](../package-guidance/package-selection.md#microsoft-abstractions-and-runtime-implementations) to decide whether a direct reference belongs in a reusable library or only at the composition root. See the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-configuration-abstractions).

## Security, performance, AOT, trimming, and operations

Configuration is untrusted operational input: validate it before use and log key names or validation summaries, never secret values. A provider must both support reload and be configured for it before change tokens can fire; consuming `IConfiguration` does not itself guarantee reload. Choose `IOptionsMonitor<TOptions>` only when live changes are intended and safe, and remember that container and network file systems may need polling. The abstractions themselves have no reflection-based object binding requirement; binder-based APIs have trimming/AOT considerations.

## Avoid

- Do not pass `IConfiguration` deeply through business code as a service locator.
- Do not rely on source ordering accidentally or place secrets in checked-in JSON files.
- Do not treat an existing key as a valid value.

## Verification checklist

- [ ] A direct consuming class library references the package without a version; central package management supplies `10.0.10`.
- [ ] Provider order, key names, and environment overrides are intentional and documented.
- [ ] Required values are parsed and validated before use; credentials come from an approved secret provider and are redacted from telemetry.
- [ ] Reload behavior is tested end to end with the selected provider, or settings are explicitly static.

## Sources

- [NuGet: Microsoft.Extensions.Configuration.Abstractions 10.0.10](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Abstractions/10.0.10) — Accessed 2026-07-27.
- [Microsoft Learn: Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) — Accessed 2026-07-27.
- [Microsoft Learn API: `IConfiguration`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfiguration?view=net-10.0-pp) — Accessed 2026-07-27.
