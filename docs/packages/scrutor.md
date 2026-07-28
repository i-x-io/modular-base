# Scrutor

## Catalog entry

`Scrutor` **7.0.0** — direct catalog package; assembly scanning and service-decoration extensions for `Microsoft.Extensions.DependencyInjection`.

- **Adoption:** Direct
- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** `Scrutor` version changes, target-framework changes, or Microsoft DI/scanning behavior changes.

## Decision and scope

Use for narrowly bounded convention-based registration or well-defined decorators. It is not a replacement for an explicit composition root: assembly boundaries, filters, service shapes, lifetimes, and decorator order remain application decisions.

## Recommended registration and use

With central package management, add a versionless `PackageReference`:

```xml
<ItemGroup>
  <PackageReference Include="Scrutor" />
</ItemGroup>
```

Anchor scanning to a marker in an application-owned assembly, filter to the intended contract and namespace, and assign the lifetime explicitly:

```csharp
using Microsoft.Extensions.DependencyInjection;

public interface IApplicationAssemblyMarker { }
public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<IApplicationAssemblyMarker>()
            .AddClasses(classes => classes
                .InNamespaces("Orders.Application.Commands")
                .AssignableTo(typeof(ICommandHandler<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
```

`AddClasses` starts from public, non-abstract types. `AsImplementedInterfaces` registers every matching interface, so narrow the candidates first when a class implements infrastructure interfaces that should not be exposed.

Decoration wraps an existing registration. The following contextual fragment assumes the consuming application declares the shown service and decorator types. Later decoration calls become outer wrappers, so keep the order visible and test it:

```csharp
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.Decorate<IOrderService, MetricsOrderService>();
builder.Services.Decorate<IOrderService, AuthorizationOrderService>();

// Resolution: AuthorizationOrderService -> MetricsOrderService -> OrderService
```

## Enterprise implementation guidance

Create one marker per intended assembly boundary and keep scan rules next to the explicit registrations they complement. Prefer contract-, namespace-, or attribute-based selection over class-name suffixes. Review the registration diff whenever assemblies or marker locations change; a newly matching class changes the service graph without editing the composition root.

Common workflow:

1. Define the marker and registration convention in the owning module.
2. Scan exactly that assembly and filter the smallest useful candidate set.
3. Choose `AsSelf`, one specific interface, or implemented interfaces deliberately.
4. Set scoped, transient, or singleton lifetime explicitly and validate captive dependencies.
5. Apply decorators after registration in documented inner-to-outer order.
6. Build the provider with validation and resolve representative closed generic services.

Decorators should preserve cancellation, exception, result, async, and disposal semantics. If a decorator adds resilience, ensure it is the single retry owner and that its lifetime matches the stateful pipeline it uses. Prefer the HTTP resilience integration for `HttpClient` instead of a Scrutor retry decorator.

### Upgrade and rollback

Compare supported frameworks and scan/decoration semantics, then snapshot service descriptors, lifetimes, duplicate-registration behavior, and decorator order. Publish-test trimmed deployments. Roll back the central pin and redeploy; mixed application versions can otherwise construct different service graphs.

## Integration with the catalog

The fixed-marker policy aligns with [FluentValidation.DependencyInjectionExtensions](fluentvalidation-dependencyinjectionextensions.md). Decorators may invoke a DI-managed [Polly.Extensions](polly-extensions.md) pipeline for non-HTTP work, but [Microsoft.Extensions.Http.Resilience](microsoft-extensions-http-resilience.md) remains the preferred HTTP integration. Keep a single retry layer when these packages meet.

See the [`Scrutor` supply-chain entry](../package-guidance/supply-chain.md#scrutor).

## Security, performance, AOT, trimming, and operations

Scanning uses reflection and increases startup work. Broad scans can expose unintended implementations or produce duplicate registrations. Assembly scanning is also a trimming/NativeAOT risk unless matching types remain reachable; preserve required types explicitly or replace the scan with explicit/generated registrations for constrained deployments.

Validate the complete service graph during startup or tests, and smoke-test resolution from the published artifact. Decorators handling authorization, transactions, or telemetry must not leak arguments, swallow failures, or convert caller cancellation into an ordinary error.

## Avoid

Do not scan every loaded or dependency assembly, rely on implicit default lifetimes, register all implemented interfaces without reviewing them, include decorators in the implementation scan, depend on accidental registration order, or decorate services with incompatible lifetimes. Do not use scanning where explicit registrations are required for trim safety or auditability.

## Verification checklist

- [ ] Confirm the centrally managed dependency resolves `Scrutor` `7.0.0`.
- [ ] Assert the exact implementations, service types, and lifetimes produced by each scan.
- [ ] Add a nonmatching type and verify the filter excludes it.
- [ ] Build the provider with scope/build validation and resolve representative closed generic handlers.
- [ ] Test decorator order plus cancellation, exception, result, and disposal propagation.
- [ ] Publish with the intended trimming/NativeAOT settings and run startup/resolution smoke tests if scanning remains.

## Sources

- [NuGet: Scrutor 7.0.0](https://www.nuget.org/packages/Scrutor/7.0.0) (Accessed 2026-07-27)
- [Scrutor upstream README and examples](https://github.com/khellang/Scrutor) (Accessed 2026-07-27)
- [Microsoft dependency injection guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines) (Accessed 2026-07-27)
