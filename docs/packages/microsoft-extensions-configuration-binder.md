# Microsoft.Extensions.Configuration.Binder

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Binds `IConfiguration` sections to typed objects | Approved at composition boundaries |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, binder/source-generator, or trimming/AOT behavior change |

## Decision and scope

Use the binder to map hierarchical configuration into a typed configuration or options object. It complements `Configuration.Abstractions`; it neither validates values nor secures configuration sources. Bind at the composition boundary, then expose an options interface or a validated immutable value to application code.

## Recommended registration and use

For DI-managed settings, bind with [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md) and validate on start. Use `Get<T>` or `Bind` only for short-lived composition work where the object does not need options lifecycle services. Keep options types small, public/settable as required by the binder, and free of behavior.

Reference the centrally pinned binder package without a version:

```xml
<PackageReference Include="Microsoft.Extensions.Configuration.Binder" />
```

For one-time composition, bind a required section, reject unmapped or unconvertible input, and then apply semantic validation. `Get<T>` creates a new instance; `Bind(instance)` populates an existing instance.

```csharp
using Microsoft.Extensions.Configuration;

WorkerSettings settings = configuration
    .GetRequiredSection("Worker")
    .Get<WorkerSettings>(options =>
        options.ErrorOnUnknownConfiguration = true)
    ?? throw new InvalidOperationException("Worker settings are missing.");

if (settings.BatchSize is < 1 or > 1_000)
    throw new InvalidOperationException("Worker:BatchSize must be 1..1000.");

public sealed class WorkerSettings
{
    public int BatchSize { get; set; }
    public TimeSpan PollInterval { get; set; }
}
```

`ErrorOnUnknownConfiguration` is useful where configuration drift must fail fast, but it does not replace required-field, range, or cross-field validation.

### Binding reference

| Setting/API | Purpose | Default behavior | Production guidance | Reload | Sensitivity | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| `Get<T>()` | Creates and binds a new object | Returns `null` when no value can be created | Use for one-time composition; validate the result | None by itself | Bound object can contain secrets | Conversion errors can throw |
| `Bind(instance)` | Populates an existing object | Binds matching keys | Keep off hot paths and validate afterward | None by itself | Same as source configuration | Partial objects are possible without validation |
| `ErrorOnUnknownConfiguration` | Rejects unknown keys and strict conversion failures | `false` | Enable where configuration drift must fail deployment | Applies at each bind | Error messages should avoid values | Unknown or unconvertible input throws `InvalidOperationException` |
| Binding source generator | Replaces supported reflection binding at compile time | Disabled unless enabled by project property | Enable for trim/AOT-sensitive applications and test the published artifact | Build-time | No change | Unsupported call shapes may still require reflection or fail publish/runtime checks |

## Enterprise implementation guidance

- Name configuration sections independently from CLR types so section names remain stable during refactoring.
- Bind once at the composition boundary. Do not repeatedly materialize the same graph on request or message hot paths.
- Validate required values, ranges, cross-field constraints, nested objects, and collection cardinality after binding.
- Decide whether unknown keys should be forward-compatible or rejected. When strict binding is enabled, malformed conversions and unknown properties can throw `InvalidOperationException`; exercise that failure path in deployment tests.

### Upgrade and rollback

Upgrade with `Configuration.Abstractions` and the options/configuration integration packages. Re-run strict-binding tests and publish the exact trimmed or Native AOT artifact because generated and reflection binding behavior can change independently of source compilation. No persistent-data migration is required. Roll back the package set and deployment artifact together; restore the previous configuration shape if the new deployment introduced keys older binaries reject.

## Integration with the catalog

Consumes [Configuration.Abstractions](microsoft-extensions-configuration-abstractions.md) and commonly feeds [Options](microsoft-extensions-options.md). [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md) supplies the DI integration.

The [options validation, reload, and health recipe](../recipes/options-validation-reload-health.md) shows the complete composition. See the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-configuration-binder).

## Security, performance, AOT, trimming, and operations

The reflection binder APIs are annotated for dynamic-code and trimming risk because member discovery occurs at runtime. In AOT- or trim-sensitive projects, enable `<EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>` so supported binder calls are intercepted by generated code. Keep bound types explicit and publish/run the exact trimmed or Native AOT artifact in CI; generator coverage is not a substitute for that test. Binding is not validation, does not decrypt values, and does not make a configuration source suitable for secrets.

## Avoid

- Do not bind arbitrary user-controlled configuration into security-sensitive types.
- Do not call the binder repeatedly on hot paths.
- Do not equate successful binding with a complete or valid configuration.

## Verification checklist

- [ ] The consuming project references the package without a version; central package management supplies `10.0.10`.
- [ ] Tests cover an absent section, conversion failure, unknown keys according to policy, nested values, and collection cardinality.
- [ ] Semantic validation rejects missing, malformed, range-invalid, and cross-field-invalid values before service use.
- [ ] The exact trimmed or Native AOT artifact is published and run; supported calls use generated binding where enabled.

## Sources

- [NuGet: Microsoft.Extensions.Configuration.Binder 10.0.10](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Binder/10.0.10) — Accessed 2026-07-27.
- [Microsoft Learn: Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) — Accessed 2026-07-27.
- [Microsoft Learn: Compile-time configuration source generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-generator) — Accessed 2026-07-27.
- [Microsoft Learn: strict binder conversion behavior](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/8.0/configurationbinder-exceptions) — Accessed 2026-07-27.
