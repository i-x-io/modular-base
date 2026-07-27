# FluentValidation.DependencyInjectionExtensions

## Catalog entry

`FluentValidation.DependencyInjectionExtensions` **12.1.1** — companion catalog package; registers FluentValidation validators with `Microsoft.Extensions.DependencyInjection`.

- **Owner:** IX
- **Last reviewed:** 2026-07-27
**Review trigger:** either FluentValidation companion version changes, target-framework changes, or DI scanning/lifetime behavior changes.

## Decision and scope

Use for controlled validator registration. It is a convenience scanner, not a universal composition mechanism.

## Recommended registration and use

The catalog supplies the version centrally, so the consuming project keeps the reference versionless:

```xml
<ItemGroup>
  <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
</ItemGroup>
```

Call `AddValidatorsFromAssemblyContaining<TMarker>` with a fixed, application-owned marker type. Transient is the FluentValidation documentation's simplest and safest recommendation when validators may depend on scoped or transient services:

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

public sealed class ValidationAssemblyMarker
{
}

public static class ValidationRegistration
{
    public static IServiceCollection AddRequestValidators(
        this IServiceCollection services) =>
        services.AddValidatorsFromAssemblyContaining<ValidationAssemblyMarker>(
            ServiceLifetime.Transient);
}
```

The scan registers public, non-abstract validators from the marker assembly. When only a subset is intended for production, pass the overload's filter and exclude types by `filter.ValidatorType`.

## Enterprise implementation guidance

The common startup workflow is: select the assembly marker, choose a lifetime, optionally filter, build the provider in a composition-root test, and resolve each required closed `IValidator<T>`. Keep scans to explicitly selected assemblies. Prefer explicit registrations or a source-generated approach for trim-sensitive applications. Never register validators as singleton when they depend on scoped/transient services; transient validators may safely resolve scoped dependencies only while created from an active scope.

### Configuration reference

| Setting | Purpose | Default behavior | Production guidance | Reload | Sensitive | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| Assembly marker | Selects validator assembly | Caller-supplied assembly | Use a fixed application marker type | Restart | No | Validators outside the assembly are not registered |
| `ServiceLifetime` | Controls validator lifetime | `Scoped` for scan helpers | Prefer transient/scoped; never singleton with shorter-lived dependencies | Restart | No | Captive dependencies or scope validation failure |
| Scan filter | Includes/excludes validators | All public validators in scope | Filter intentionally and test exclusions | Restart | No | Missing or unintended registrations |
| `includeInternalTypes` | Includes internal validators | `false` | Enable only for an explicit module boundary | Restart | No | Internal validators remain unresolved when disabled |

### Upgrade and rollback

Keep this package on the exact same version line as `FluentValidation`. Compare scanner visibility and lifetime defaults, build the provider with validation, and resolve representative validators with scoped dependencies. Roll back both pins together; do not leave mixed versions deployed.

## Integration with the catalog

This companion supports `fluentvalidation.md`; FastEndpoints endpoint validation should resolve the registered `IValidator<T>` through its supported integration. `scrutor.md` has the same fixed-marker, reflection-scanning constraint.

See the [validation/results recipe](../recipes/fastendpoints-validation-results.md) and [`FluentValidation.DependencyInjectionExtensions` supply-chain entry](../package-guidance/supply-chain.md#fluentvalidation-dependencyinjectionextensions).

## Security, performance, AOT, trimming, and operations

The automatic-registration methods scan assemblies with reflection. Startup work and trimming reachability therefore depend on the selected marker assembly. Do not let untrusted plugin assemblies enter the scan set. Validate the service graph at startup where the host supports it, and test registration discovery plus published trimmed/NativeAOT output when this package is enabled. Avoid logging validator type inventories if application structure is sensitive.

At startup/tests, record a count of discovered validator service descriptors and treat provider-validation failures as deployment failures. Do not log assembly-qualified types from untrusted plugins or validation input values.

### Troubleshooting

| Symptom | Likely cause | Diagnostic | Correction | Retry? |
| --- | --- | --- | --- | --- |
| `IValidator<T>` cannot resolve | Wrong assembly marker, internal type excluded, or scanner not called | Inspect service descriptors and marker assembly | Fix the bounded scan/filter or explicit registration | No |
| Provider validation reports captive dependency | Validator lifetime exceeds a dependency lifetime | Inspect descriptor lifetimes and constructor graph | Use transient/scoped validator or correct dependency lifetime | No |
| Validator runs twice | Duplicate scan or explicit plus scanned registration | Count matching descriptors in a provider test | Keep one registration owner or apply an intentional filter | No |

## Avoid

Do not scan all loaded assemblies, rely on an assembly name string, or make lifetime choices incompatible with validator dependencies.

## Verification checklist

- [ ] Build the service provider with scope validation and resolve every required `IValidator<T>`.
- [ ] Verify excluded and internal/non-public validators are not registered.
- [ ] Assert validator lifetimes are compatible with all injected dependencies.
- [ ] Run the application’s trimmed/NativeAOT publish smoke test if scanning remains enabled.

## Sources

- [NuGet Gallery: FluentValidation.DependencyInjectionExtensions 12.1.1](https://www.nuget.org/packages/FluentValidation.DependencyInjectionExtensions/12.1.1) (Accessed 2026-07-27)
- [FluentValidation: dependency injection and automatic registration](https://docs.fluentvalidation.net/en/latest/di.html) (Accessed 2026-07-27)
