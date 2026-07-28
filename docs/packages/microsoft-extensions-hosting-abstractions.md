# Microsoft.Extensions.Hosting.Abstractions

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Host and hosted-service contracts including `IHost`, `IHostApplicationLifetime`, and `IHostedService` | Direct; approved library-facing abstraction |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, hosting lifecycle contract, or background-service convention change |

## Decision and scope

Reference this package when a reusable component needs host lifecycle contracts, especially an `IHostedService`, without taking a dependency on the Generic Host implementation. It does not build a host or configure application services.

## Recommended registration and use

Implement `IHostedService` or derive from `BackgroundService` for host-managed work. Start quickly, run asynchronous work after startup, honor both start/stop cancellation, and make the operation idempotent enough for the host lifecycle. Register the implementation through the application's hosting/DI composition root.

With Central Package Management, a reusable worker library references only the host contracts it consumes:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
</ItemGroup>
```

Keep the worker focused on lifecycle coordination and delegate one unit of domain work to an injected abstraction:

```csharp
using Microsoft.Extensions.Hosting;

public interface IInboxPump
{
    Task PumpOnceAsync(CancellationToken cancellationToken);
}

public sealed class InboxWorker(IInboxPump pump) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        do
        {
            await pump.PumpOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
```

The application composition root registers this type with `AddHostedService<InboxWorker>()`. A hosted service is process-lived, so `IInboxPump` must also be singleton-safe in this direct-injection example. If each operation needs scoped state, inject `IServiceScopeFactory` and resolve the processor inside a fresh scope as shown in the [Hosting guide](microsoft-extensions-hosting.md).

## Enterprise implementation guidance

Define the work's delivery guarantee, shutdown drain/checkpoint limit, failure policy, and observability. Split independent workers instead of building one large supervisor service. Keep infrastructure lifecycle code separate from domain processing logic so it remains testable.

Use `IHostApplicationLifetime` to observe `ApplicationStarted`, `ApplicationStopping`, and `ApplicationStopped`, or to request an orderly stop with `StopApplication`; do not terminate the process directly from reusable infrastructure. Keep `StartAsync` bounded because hosted services start as part of the host startup sequence. Treat the cancellation token passed to `StopAsync` as the shutdown deadline, not merely a notification.

### Upgrade and rollback

Keep this contract package aligned with the concrete Hosting implementation. Recompile hosted-service libraries and test startup cancellation, `BackgroundService` failure behavior, application-lifetime callbacks, and graceful shutdown in the actual host. No data migration is required. Roll back library and host artifacts together when lifecycle contracts cannot be honored.

## Integration with the catalog

[Hosting](microsoft-extensions-hosting.md) provides the Generic Host and registration APIs. Use [DependencyInjection.Abstractions](microsoft-extensions-dependencyinjection-abstractions.md) for dependencies and [Logging.Abstractions](microsoft-extensions-logging-abstractions.md) for safe structured diagnostics.

Use the [abstraction-versus-runtime selection guide](../package-guidance/package-selection.md#microsoft-abstractions-and-runtime-implementations) before taking a concrete Hosting dependency. See the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-hosting-abstractions).

## Security, performance, AOT, trimming, and operations

Background services run with process authority; constrain credentials and tenant scope. Honor `StopAsync` cancellation and avoid unbounded queues/memory. These contracts are AOT/trimming-friendly; dynamic worker discovery requires separate publish-mode validation.

Bound concurrency and queue capacity, propagate cancellation into I/O, and make retry/backoff ownership explicit. `StopAsync` is not guaranteed after process failure, so correctness cannot depend on an in-memory finalizer or last checkpoint. Prefer statically registered worker types; assembly scanning for workers changes the trimming and deployment contract.

## Avoid

- Do not register a background service as a singleton and separately as a hosted service unless the identity/lifetime is intentional.
- Do not block `StartAsync` on endless work.
- Do not ignore stop signals or background exceptions.
- Do not call `Environment.Exit` when `StopApplication` can request a coordinated shutdown.

## Verification checklist

- [ ] Start, run, failure, and cancellation paths are covered.
- [ ] Shutdown drains/checkpoints according to the documented delivery guarantee.
- [ ] The worker has least-privilege credentials and useful logs/metrics.
- [ ] Directly injected dependencies are singleton-safe, or each iteration creates and disposes a scope.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Hosting.Abstractions) (Accessed 2026-07-27)
- [Hosted services in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers) (Accessed 2026-07-27)
- [`IHostedService` API reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice?view=net-10.0) (Accessed 2026-07-27)
- [`BackgroundService` API reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice?view=net-10.0) (Accessed 2026-07-27)
- [`IHostApplicationLifetime` API reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostapplicationlifetime?view=net-10.0) (Accessed 2026-07-27)
