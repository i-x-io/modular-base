# Microsoft.Extensions.DependencyInjection.Abstractions

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | DI contracts: `IServiceCollection`, `IServiceProvider`, `ServiceDescriptor`, and registration extensions | Approved library-facing abstraction |

## Decision and scope

Reference this package when a reusable library needs to expose registration extensions or consume standard DI contracts without depending on the default container. It does not implement a container or host.

## Recommended registration and use

Expose an `IServiceCollection` extension method from integration libraries and register only the services owned by that library. Return `IServiceCollection` or the appropriate builder so callers can continue composition. Keep public services constructor-injected and avoid requiring a particular container implementation.

## Enterprise implementation guidance

Document every registration, lifetime, required configuration, and optional dependency. Make registration idempotence and replacement behavior deliberate. Libraries should not create a root provider, start hosted services, or mutate unrelated application registrations.

## Integration with the catalog

The default implementation is [DependencyInjection](microsoft-extensions-dependencyinjection.md). [Hosting](microsoft-extensions-hosting.md) owns the root provider. Use [Options](microsoft-extensions-options.md) for library configuration rather than injecting raw configuration into all services.

## Security, performance, AOT, trimming, and operations

The contracts are container-neutral; do not depend on undocumented behavior of the default container from a reusable library. Avoid reflection scanning in registration paths when AOT/trimming matters. Lifetimes remain a correctness and security boundary regardless of container.

## Avoid

- Do not use the abstractions package as evidence that a concrete DI container exists.
- Do not resolve services during `Add...` registration.
- Do not publish registration extensions in the `Microsoft.Extensions.DependencyInjection` namespace unless you are an official Microsoft package.

## Verification checklist

- The library composes against the supported host/container.
- Lifetimes and disposal ownership are documented and tested.
- Registration does not build a provider or resolve application services.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) (Accessed 2026-07-27)
- [Dependency injection basics](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/basics) (Accessed 2026-07-27)
