# Microsoft.Extensions.DependencyInjection

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Default .NET DI container implementation and registration extensions | Approved composition-root implementation |

## Decision and scope

Use the default container to compose applications and infrastructure. This implementation consumes the contracts in `DependencyInjection.Abstractions`. Application services should use constructor injection and should not build nested service providers or resolve services ad hoc.

## Recommended registration and use

Register services once during host construction. Select lifetimes deliberately: singleton for stateless/thread-safe shared services, scoped for a unit of work/request, and transient for lightweight per-use services. Enable scope validation in development and create scopes explicitly only for background or factory scenarios.

## Enterprise implementation guidance

Keep registrations in feature-focused extension methods owned by the integrating module. Validate the composition root at startup; make optional integrations explicit rather than hiding failed resolution. Prefer factory abstractions over injecting `IServiceProvider` into domain/application services.

## Integration with the catalog

[DependencyInjection.Abstractions](microsoft-extensions-dependencyinjection-abstractions.md) defines the public contracts. [Hosting](microsoft-extensions-hosting.md) creates and owns the root provider. HTTP, options, logging, and health checks are all registered through this container.

## Security, performance, AOT, trimming, and operations

Incorrect lifetimes cause concurrency bugs, captured disposables, and memory retention. Do not resolve scoped services from singletons; create a scope for background work. The built-in container's normal registration model is static and suitable for trimming/AOT, but reflection-driven scanning and runtime type activation should be avoided or validated in the target publish mode.

## Avoid

- Do not call `BuildServiceProvider` inside registration code.
- Do not use `IServiceProvider` as a general service locator.
- Do not register disposable transient objects without understanding who disposes them.

## Verification checklist

- Scope validation runs in development/CI and no singleton captures a scoped dependency.
- A startup composition test resolves each hosted and feature service.
- Disposal and scope ownership are tested for background processing.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) (Accessed 2026-07-27)
- [Dependency injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) (Accessed 2026-07-27)
