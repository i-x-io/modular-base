# FluentValidation.DependencyInjectionExtensions

## Catalog entry

`FluentValidation.DependencyInjectionExtensions` **12.1.1** — companion catalog package; registers FluentValidation validators with `Microsoft.Extensions.DependencyInjection`.

## Decision and scope

Use for controlled validator registration. It is a convenience scanner, not a universal composition mechanism.

## Recommended registration and use

Call `AddValidatorsFromAssemblyContaining<TMarker>(ServiceLifetime.Transient)` with a fixed, application-owned marker type. Transient is the FluentValidation documentation's simplest and safest recommendation when validators may depend on scoped or transient services.

## Enterprise implementation guidance

Keep scans to explicitly selected assemblies and apply a filter when only a subset is intended. Prefer explicit registrations or a source-generated approach for trim-sensitive applications. Never register validators as singleton when they depend on scoped/transient services.

## Integration with the catalog

This companion supports `fluentvalidation.md`; FastEndpoints endpoint validation should resolve the registered `IValidator<T>` through its supported integration. `scrutor.md` has the same fixed-marker, reflection-scanning constraint.

## Security, performance, AOT, trimming, and operations

The automatic-registration methods scan assemblies with reflection. Startup work and trimming reachability therefore depend on the selected marker assembly. Test registration discovery and published trimmed output when this package is enabled.

## Avoid

Do not scan all loaded assemblies, rely on an assembly name string, or make lifetime choices incompatible with validator dependencies.

## Verification checklist

- Build the service provider and resolve every required `IValidator<T>`.
- Verify excluded validators are not registered.
- Run the application’s trimmed/NativeAOT publish smoke test if scanning remains enabled.

## Sources

- https://www.nuget.org/packages/FluentValidation.DependencyInjectionExtensions/12.1.1 (Accessed 2026-07-27)
- https://docs.fluentvalidation.net/en/latest/di.html (Accessed 2026-07-27)
