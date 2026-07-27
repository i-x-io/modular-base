# FluentValidation.DependencyInjectionExtensions

## Catalog entry

`FluentValidation.DependencyInjectionExtensions` **12.1.1** — companion catalog package; registers FluentValidation validators with `Microsoft.Extensions.DependencyInjection`.

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

## Integration with the catalog

This companion supports `fluentvalidation.md`; FastEndpoints endpoint validation should resolve the registered `IValidator<T>` through its supported integration. `scrutor.md` has the same fixed-marker, reflection-scanning constraint.

## Security, performance, AOT, trimming, and operations

The automatic-registration methods scan assemblies with reflection. Startup work and trimming reachability therefore depend on the selected marker assembly. Do not let untrusted plugin assemblies enter the scan set. Validate the service graph at startup where the host supports it, and test registration discovery plus published trimmed/NativeAOT output when this package is enabled. Avoid logging validator type inventories if application structure is sensitive.

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
