# Microsoft.Extensions.DependencyInjection.Abstractions

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | DI contracts: `IServiceCollection`, `IServiceProvider`, `ServiceDescriptor`, and registration extensions | Direct; approved library-facing abstraction |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, public DI contract, or extension-registration convention change |

## Decision and scope

Reference this package when a reusable library needs to expose registration extensions or consume standard DI contracts without depending on the default container. It does not implement a container or host.

## Recommended registration and use

Expose an `IServiceCollection` extension method from integration libraries and register only the services owned by that library. Return `IServiceCollection` or the appropriate builder so callers can continue composition. Keep public services constructor-injected and avoid requiring a particular container implementation.

With Central Package Management, a reusable integration project references the abstraction without repeating the catalog version:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
</ItemGroup>
```

Keep the extension method small, deterministic, and explicit about its owned registrations:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Contoso.Payments;

public static class PaymentServiceCollectionExtensions
{
    public static IServiceCollection AddPayments(
        this IServiceCollection services,
        Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpoint);

        services.AddSingleton(new PaymentOptions(endpoint));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPaymentGateway, PaymentGateway>();
        return services;
    }
}

public sealed record PaymentOptions(Uri Endpoint);

public interface IPaymentGateway { DateTimeOffset CreatedAt { get; } }
internal sealed class PaymentGateway(TimeProvider timeProvider) : IPaymentGateway
{
    public DateTimeOffset CreatedAt => timeProvider.GetUtcNow();
}
```

This pattern lets the application compose the library, override deliberately documented defaults, and validate the complete graph when its host starts. If an extension uses the options pattern instead, the integration also needs the appropriate options package; do not assume this abstractions package supplies unrelated configuration behavior.

## Enterprise implementation guidance

Document every registration, lifetime, required configuration, and optional dependency. Make registration idempotence and replacement behavior deliberate. Libraries should not create a root provider, start hosted services, or mutate unrelated application registrations.

Use an options or feature-specific builder when configuration grows beyond a few parameters. Keep implementation types internal where possible and expose only the service contract. Test the extension against the supported concrete container, including repeated registration if idempotence is promised and consumer replacement if defaults are advertised as replaceable.

### Upgrade and rollback

Keep this contract package compatible with the concrete DI implementation selected by the consuming host. On upgrade, rebuild public registration extensions, verify their binary/API compatibility, and run composition tests with the concrete container. No state migration is required. Roll back the library and its consumers together when a newly exposed contract cannot be supported by the deployed host.

## Integration with the catalog

The default implementation is [DependencyInjection](microsoft-extensions-dependencyinjection.md). [Hosting](microsoft-extensions-hosting.md) owns the root provider. Use [Options](microsoft-extensions-options.md) for library configuration rather than injecting raw configuration into all services.

Use the [abstraction-versus-runtime selection guide](../package-guidance/package-selection.md#microsoft-abstractions-and-runtime-implementations) before adding a concrete implementation dependency. See the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-dependencyinjection-abstractions).

## Security, performance, AOT, trimming, and operations

The contracts are container-neutral; do not depend on undocumented behavior of the default container from a reusable library. Avoid reflection scanning in registration paths when AOT/trimming matters. Lifetimes remain a correctness and security boundary regardless of container.

Registration should not perform I/O, contact remote systems, read secrets, or enumerate assemblies. Those operations make startup ordering unpredictable and can leak process-level state into a library. Prefer compile-time registrations; if reflection-based discovery is unavoidable, publish and test a trimmed consumer application and act on every trim warning.

## Avoid

- Do not use the abstractions package as evidence that a concrete DI container exists.
- Do not resolve services during `Add...` registration.
- Do not publish registration extensions in the `Microsoft.Extensions.DependencyInjection` namespace unless you are an official Microsoft package.
- Do not silently replace application-owned registrations.

## Verification checklist

- [ ] The library composes against the supported host/container.
- [ ] Lifetimes and disposal ownership are documented and tested.
- [ ] Registration does not build a provider or resolve application services.
- [ ] Repeated registration and consumer override behavior match the documented contract.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) (Accessed 2026-07-27)
- [Dependency injection basics](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/basics) (Accessed 2026-07-27)
- [Dependency injection guidelines for library authors](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines) (Accessed 2026-07-27)
- [`IServiceCollection` API reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection?view=net-10.0) (Accessed 2026-07-27)
