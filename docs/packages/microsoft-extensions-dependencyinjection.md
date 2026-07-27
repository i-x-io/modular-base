# Microsoft.Extensions.DependencyInjection

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Default .NET DI container implementation and registration extensions | Approved composition-root implementation |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, container validation, lifetime, or disposal behavior change |

## Decision and scope

Use the default container to compose applications and infrastructure. This implementation consumes the contracts in `DependencyInjection.Abstractions`. Application services should use constructor injection and should not build nested service providers or resolve services ad hoc.

## Recommended registration and use

Register services once during host construction. Select lifetimes deliberately: singleton for stateless/thread-safe shared services, scoped for a unit of work/request, and transient for lightweight per-use services. Enable scope validation in development and create scopes explicitly only for background or factory scenarios.

With Central Package Management, keep the version in `Directory.Packages.props` and add only the reference to the consuming project:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
</ItemGroup>
```

The normal application workflow is:

1. Add registrations to the host's single `IServiceCollection` before `Build`.
2. Build the host once; it creates and owns the root provider.
3. Let constructor injection resolve the object graph.
4. Create and dispose an explicit scope for each background unit of work that needs scoped dependencies.
5. Dispose the host so the container disposes owned services in the correct order.

The following standalone provider is useful in a focused composition test. Production hosted applications should let `Microsoft.Extensions.Hosting` build and dispose this provider instead:

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<ISystemClock, SystemClock>();
services.AddScoped<IOrderRepository, OrderRepository>();
services.AddTransient<CreateOrderHandler>();

await using ServiceProvider provider = services.BuildServiceProvider(
    new ServiceProviderOptions
    {
        ValidateOnBuild = true,
        ValidateScopes = true,
    });

await using AsyncServiceScope scope = provider.CreateAsyncScope();
var handler = scope.ServiceProvider.GetRequiredService<CreateOrderHandler>();
await handler.HandleAsync(CancellationToken.None);

public interface ISystemClock { DateTimeOffset UtcNow { get; } }
public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public interface IOrderRepository
{
    Task SaveAsync(DateTimeOffset createdAt, CancellationToken cancellationToken);
}

public sealed class OrderRepository : IOrderRepository
{
    public Task SaveAsync(DateTimeOffset createdAt, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class CreateOrderHandler(
    IOrderRepository repository,
    ISystemClock clock)
{
    public Task HandleAsync(CancellationToken cancellationToken) =>
        repository.SaveAsync(clock.UtcNow, cancellationToken);
}
```

`ValidateOnBuild` checks that registered services can be constructed, while `ValidateScopes` detects scoped services resolved from the root provider or captured by singletons. Open generic registrations are not validated by `ValidateOnBuild`, so composition tests should also close and resolve important generic graphs.

## Enterprise implementation guidance

Keep registrations in feature-focused extension methods owned by the integrating module. Validate the composition root at startup; make optional integrations explicit rather than hiding failed resolution. Prefer factory abstractions over injecting `IServiceProvider` into domain/application services.

Use `TryAdd...` only when a library intentionally supplies an overridable default; use `Replace` only when replacement is part of the integration contract. Multiple registrations of the same service type are returned by `IEnumerable<T>`, while a direct `T` resolution returns the last registration. Document either behavior when it is part of a public extension method.

### Upgrade and rollback

Upgrade this implementation with `DependencyInjection.Abstractions`, Hosting, Options, Logging, HTTP, and HealthChecks packages that participate in the same composition root. Re-run build/scope validation, disposal tests, and closed-generic composition tests under the target framework. There is no data migration. Roll back the application and aligned package set together if resolution, lifetime validation, or disposal ordering changes.

## Integration with the catalog

[DependencyInjection.Abstractions](microsoft-extensions-dependencyinjection-abstractions.md) defines the public contracts. [Hosting](microsoft-extensions-hosting.md) creates and owns the root provider. HTTP, options, logging, and health checks are all registered through this container.

Use the [abstraction-versus-runtime selection guide](../package-guidance/package-selection.md#microsoft-abstractions-and-runtime-implementations) for direct-reference ownership. See the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-dependencyinjection).

## Security, performance, AOT, trimming, and operations

Incorrect lifetimes cause concurrency bugs, captured disposables, and memory retention. Do not resolve scoped services from singletons; create a scope for background work. The built-in container's normal registration model is static and suitable for trimming/AOT, but reflection-driven scanning and runtime type activation should be avoided or validated in the target publish mode.

The container disposes `IDisposable` and `IAsyncDisposable` instances that it creates. A caller-created instance registered with `AddSingleton(instance)` remains caller-owned. Avoid resolving disposable transients from the root provider because they are retained until the root provider is disposed. Treat every singleton implementation as concurrent and thread-safe.

## Avoid

- Do not call `BuildServiceProvider` inside registration code.
- Do not use `IServiceProvider` as a general service locator.
- Do not register disposable transient objects without understanding who disposes them.
- Do not capture request, tenant, credential, or unit-of-work state in a singleton.

## Verification checklist

- [ ] Scope validation runs in development/CI and no singleton captures a scoped dependency.
- [ ] A startup composition test resolves each hosted and feature service.
- [ ] Disposal and scope ownership are tested for background processing.
- [ ] Important open-generic registrations are resolved as closed types in composition tests.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) (Accessed 2026-07-27)
- [Dependency injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) (Accessed 2026-07-27)
- [Service lifetimes in .NET dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/service-lifetimes) (Accessed 2026-07-27)
- [Dependency injection guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines) (Accessed 2026-07-27)
- [`ServiceProviderOptions` API reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.serviceprovideroptions?view=net-10.0) (Accessed 2026-07-27)
