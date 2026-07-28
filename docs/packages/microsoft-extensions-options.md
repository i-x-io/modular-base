# Microsoft.Extensions.Options

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Options contracts, factories, caching, validation, and `IOptions*` accessors | Direct; approved typed-configuration foundation |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, options lifecycle, validation, or source-generator change |

## Decision and scope

Use options for strongly typed, scenario-specific application settings. This package owns the options lifecycle and validation contracts; binding configuration sections requires [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md).

## Recommended registration and use

Use `IOptions<T>` for stable singleton-readable values, `IOptionsSnapshot<T>` for scoped values, and `IOptionsMonitor<T>` for singleton consumers that intentionally support reload. Use named options only when multiple instances of the same settings shape are genuinely needed. Validate every required options type with `Validate`, `IValidateOptions<T>`, or generated/data-annotation validation and call `ValidateOnStart` for startup-critical configuration.

Reference the centrally pinned lifecycle/validation package without a version:

```xml
<PackageReference Include="Microsoft.Extensions.Options" />
```

This package can configure and validate options without configuration binding. The following workflow is appropriate for defaults or settings supplied programmatically:

```csharp
using Microsoft.Extensions.Options;

builder.Services
    .AddOptions<RetryOptions>()
    .Configure(options =>
    {
        options.MaxAttempts = 3;
        options.Delay = TimeSpan.FromMilliseconds(250);
    })
    .Validate(options => options.MaxAttempts is >= 1 and <= 10,
        "MaxAttempts must be between 1 and 10.")
    .Validate(options => options.Delay > TimeSpan.Zero,
        "Delay must be positive.")
    .ValidateOnStart();

public sealed class RetryOptions
{
    public int MaxAttempts { get; set; }
    public TimeSpan Delay { get; set; }
}
```

Consumers select a lifecycle explicitly: read `IOptions<T>.Value` for static settings, `IOptionsSnapshot<T>.Value` once per scope, or `IOptionsMonitor<T>.CurrentValue` for reloadable settings. An `IOptionsMonitor<T>.OnChange` subscription returns `IDisposable`; a long-lived subscriber should retain and dispose it.

### Options lifecycle reference

| API | Purpose | Default behavior | Production guidance | Reload | Sensitivity | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| `IOptions<T>` | Singleton-readable options | Creates/caches the default instance on first access | Use for intentionally static values | Does not observe reload | May contain secrets; never log the object | Validation failure appears on access unless startup validation runs |
| `IOptionsSnapshot<T>` | Per-scope options | Recomputes/caches per scope and name | Use only in scoped/transient consumers | Observes changes in a later scope | Same as source configuration | Invalid value fails when accessed in the scope |
| `IOptionsMonitor<T>` | Current value and callbacks | Singleton service with cached named values | Keep callbacks fast, thread-safe, and disposable | Observes configured change tokens | Callback must not expose values | Invalid updated values can fail access/callback processing |
| `ValidateOnStart` | Forces startup-critical validation | Validation is otherwise lazy | Enable for settings required to start safely | Revalidates startup instance; reload semantics remain consumer-specific | Validation messages must omit secrets | Host startup fails with `OptionsValidationException` |

## Enterprise implementation guidance

- Create one small options class per capability, not a global configuration object. Keep its public shape separate from credentials where different ownership or disclosure rules apply.
- Put basic shape/range validation next to registration and cross-field rules in `IValidateOptions<T>`. Do not perform slow or unreliable network calls inside validators.
- Validation is lazy unless startup validation is requested. Use `ValidateOnStart` for configuration required to start safely.
- All options instances are named internally; names are case-sensitive. Validate and test each configured name rather than assuming default-name validation covers it.
- Define reload semantics: which settings may change, whether consumers read a fresh value per operation, how callbacks update dependent state atomically, and what happens when a newly supplied value fails validation.

### Upgrade and rollback

Upgrade with `Options.ConfigurationExtensions`, configuration packages, and the hosting/DI stack. Re-run validation timing, every named option, snapshot scope, monitor reload, invalid-update, and callback-disposal tests. No persistent-data migration is required, but configuration-schema changes need staged deployment. Roll back the package set and configuration shape together; keep the last-known-good external configuration available.

## Integration with the catalog

[Configuration.Abstractions](microsoft-extensions-configuration-abstractions.md) supplies settings, [Configuration.Binder](microsoft-extensions-configuration-binder.md) maps them, and [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md) wires them into DI. [DependencyInjection](microsoft-extensions-dependencyinjection.md) registers the services.

The [options validation, reload, and health recipe](../recipes/options-validation-reload-health.md) shows a complete lifecycle. See the [abstraction-versus-runtime selection guide](../package-guidance/package-selection.md#microsoft-abstractions-and-runtime-implementations) and [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-options).

## Security, performance, AOT, trimming, and operations

Options are configuration, not secret storage. They can carry a secret obtained from a secure provider, but never log complete option objects or include secret values in validation errors. `IOptionsSnapshot<T>` is scoped, recomputes at most once per scope/name when accessed, and must not enter singleton dependencies. `IOptionsMonitor<T>` is singleton-capable; callbacks can run concurrently or repeatedly, so keep them fast, thread-safe, and disposable. Use the options validation source generator for annotation-based, AOT-friendly validators where applicable, and still test the final deployment artifact.

## Avoid

- Do not inject `IOptionsSnapshot<T>` into singleton services.
- Do not omit `ValidateOnStart` for configuration required to safely start.
- Do not use named options as a substitute for an explicit client/service abstraction.

## Verification checklist

- [ ] The consuming project references the package without a version; central package management supplies `10.0.10`.
- [ ] Each startup-critical options type rejects missing, malformed, range-invalid, and cross-field-invalid values during host startup.
- [ ] Every configured option name is resolved and validated; service lifetimes do not inject snapshots into singletons.
- [ ] Monitor callbacks are thread-safe and disposed, and reload/invalid-update behavior is tested, or settings are intentionally static.

## Sources

- [NuGet: Microsoft.Extensions.Options 10.0.10](https://www.nuget.org/packages/Microsoft.Extensions.Options/10.0.10) — Accessed 2026-07-27.
- [Microsoft Learn: Options pattern in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) — Accessed 2026-07-27.
- [Microsoft Learn: Options pattern guidance for library authors](https://learn.microsoft.com/en-us/dotnet/core/extensions/options-library-authors) — Accessed 2026-07-27.
- [Microsoft Learn: Compile-time options validation](https://learn.microsoft.com/en-us/dotnet/core/extensions/options-validation-generator) — Accessed 2026-07-27.
