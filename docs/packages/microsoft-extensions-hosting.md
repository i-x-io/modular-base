# Microsoft.Extensions.Hosting

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Generic Host implementation, configuration, DI, lifetime, and hosted-service integration | Approved application host implementation |

## Decision and scope

Use the Generic Host as the application composition and lifecycle boundary for services, workers, and console applications. It implements the hosting contracts and owns the root DI provider, configuration pipeline, logging, and graceful shutdown coordination.

## Recommended registration and use

Create one host, register infrastructure/services before `Build`, and use `IHostedService`/`BackgroundService` for managed background work. Make start and stop cancellation-aware. Keep business workflows out of `Program` and expose feature registration through owned extension methods.

With Central Package Management, add the package without a version in the application project:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Hosting" />
</ItemGroup>
```

`Host.CreateApplicationBuilder(args)` supplies the standard host stack: configuration from `appsettings.json`, environment-specific JSON, Development user secrets, environment variables, and command-line arguments; console/debug/event-source logging; and the default DI container. Later configuration providers override earlier ones, with command-line arguments taking highest priority among these defaults.

This composition root registers a scoped processor and a singleton hosted worker. The worker creates a scope for every unit of work rather than capturing the scoped processor for the process lifetime:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<HostOptions>(options =>
    options.ShutdownTimeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped<IJobProcessor, JobProcessor>();
builder.Services.AddHostedService<QueueWorker>();

using IHost host = builder.Build();
await host.RunAsync();

public interface IJobProcessor
{
    Task ProcessNextAsync(CancellationToken cancellationToken);
}

public sealed class JobProcessor : IJobProcessor
{
    public Task ProcessNextAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class QueueWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<QueueWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        try
        {
            do
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IJobProcessor>();
                await processor.ProcessNextAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Queue worker received the host shutdown signal.");
        }
    }
}
```

Use `StartAsync` for short initialization, `ExecuteAsync` for long-running work, and `StopAsync` only for bounded drain/checkpoint logic. `RunAsync` keeps the process alive until shutdown; console lifetime translates `CTRL+C` and `SIGTERM` into the host stop sequence.

## Enterprise implementation guidance

Set the environment, configuration sources, logging, shutdown period, and host lifetime intentionally. Treat startup as a validation gate: options and required dependencies should fail clearly before accepting work. Coordinate background consumers so stop drains or checkpoints according to each data-delivery contract.

Choose a failure policy for each worker. An unhandled `BackgroundService` exception is an operational event, not a retry strategy; log enough correlation data, make work idempotent where delivery can repeat, and let the deployment supervisor restart the process when that is the declared policy. Align `HostOptions.ShutdownTimeout` with the orchestrator's termination grace period and stop accepting new work before draining in-flight work.

## Integration with the catalog

[Hosting.Abstractions](microsoft-extensions-hosting-abstractions.md) is appropriate for reusable hosted-service libraries. [DependencyInjection](microsoft-extensions-dependencyinjection.md), [Options](microsoft-extensions-options.md), and [Logging.Abstractions](microsoft-extensions-logging-abstractions.md) form the standard host stack.

## Security, performance, AOT, trimming, and operations

Do not execute unbounded blocking work in host startup. Enforce cancellation in background loops and do not swallow service failures; choose explicit restart/exit policy. Avoid reflection-driven discovery in startup paths for trimmed/AOT deployment. Test SIGTERM/CTRL+C shutdown, readiness transition, and process exit behavior in the deployment environment.

The host stack itself supports trimming-friendly static composition, but registrations, configuration binding, serializers, and plugin discovery added by the application may introduce warnings. Publish and smoke-test the actual RID-specific artifact. Emit startup, ready, stopping, and stopped telemetry; monitor worker throughput, failure count, queue depth, and shutdown duration without logging secrets or whole payloads.

## Avoid

- Do not build multiple root hosts/providers in one process without a defined ownership model.
- Do not start detached background tasks outside host lifecycle management.
- Do not assume startup validation proves remote dependencies remain healthy.
- Do not inject a scoped service directly into a hosted service, which is registered as a singleton.

## Verification checklist

- [ ] Startup validates required options and registrations before serving work.
- [ ] Hosted services start, stop, and honor cancellation deterministically.
- [ ] Deployment shutdown/probe behavior is integration-tested.
- [ ] The configured shutdown timeout fits inside the platform termination window.
- [ ] A published trimmed/single-file/AOT artifact is smoke-tested when those modes are supported.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Hosting) (Accessed 2026-07-27)
- [Generic Host in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host) (Accessed 2026-07-27)
- [Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) (Accessed 2026-07-27)
- [Worker services and `BackgroundService`](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers) (Accessed 2026-07-27)
- [Create a scoped service in a `BackgroundService`](https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service) (Accessed 2026-07-27)
