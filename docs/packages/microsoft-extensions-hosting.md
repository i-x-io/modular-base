# Microsoft.Extensions.Hosting

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Generic Host implementation, configuration, DI, lifetime, and hosted-service integration | Direct; approved application host implementation |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, Generic Host defaults, lifecycle, or shutdown behavior change |

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
            throw;
        }
    }
}
```

Use `StartAsync` for short initialization, `ExecuteAsync` for long-running work, and `StopAsync` only for bounded drain/checkpoint logic. `RunAsync` keeps the process alive until shutdown; console lifetime translates `CTRL+C` and `SIGTERM` into the host stop sequence.

### Host configuration reference

| Setting | Purpose | Default behavior | Production guidance | Reload | Sensitivity | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| Environment name | Selects environment-specific behavior/configuration | Platform/host default | Set explicitly in deployment and validate allowed names | Restart | Can reveal deployment topology | Wrong value loads unintended configuration |
| Content root | Resolves content files | Host builder chooses a default | Use an explicit deployment-owned path when files are required | Restart | Path may reveal filesystem layout | Missing files fail at their point of use |
| `HostOptions.ShutdownTimeout` | Bounds graceful stop | Framework default | Keep below orchestrator termination grace while allowing safe cleanup | Options-based but operationally restart-controlled | Not sensitive | Remaining work is abandoned when the process is terminated after the limit |
| `HostOptions.BackgroundServiceExceptionBehavior` | Defines host response to unhandled background exceptions | Stops the host | Keep stop-host behavior unless an explicit supervisor owns restart | Restart-controlled | Not sensitive | Unhandled exception stops the host or is ignored according to policy |

## Enterprise implementation guidance

Set the environment, configuration sources, logging, shutdown period, and host lifetime intentionally. Treat startup as a validation gate: options and required dependencies should fail clearly before accepting work. Coordinate background consumers so stop drains or checkpoints according to each data-delivery contract.

Choose a failure policy for each worker. An unhandled `BackgroundService` exception is an operational event, not a retry strategy; log enough correlation data, make work idempotent where delivery can repeat, and let the deployment supervisor restart the process when that is the declared policy. Align `HostOptions.ShutdownTimeout` with the orchestrator's termination grace period and stop accepting new work before draining in-flight work.

### Upgrade and rollback

Upgrade Hosting with its abstractions and the Microsoft.Extensions packages composed by the host. Re-run configuration precedence, environment selection, DI validation, hosted-service ordering, startup failure, graceful-stop, and shutdown-timeout tests. Coordinate any default change with deployment probes and termination grace periods. No data migration is required. Roll back the full application artifact if lifecycle or default composition regresses.

## Integration with the catalog

[Hosting.Abstractions](microsoft-extensions-hosting-abstractions.md) is appropriate for reusable hosted-service libraries. [DependencyInjection](microsoft-extensions-dependencyinjection.md), [Options](microsoft-extensions-options.md), and [Logging.Abstractions](microsoft-extensions-logging-abstractions.md) form the standard host stack.

Use the [abstraction-versus-runtime selection guide](../package-guidance/package-selection.md#microsoft-abstractions-and-runtime-implementations) for host ownership. See the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-hosting).

## Security, performance, AOT, trimming, and operations

Do not execute unbounded blocking work in host startup. Enforce cancellation in background loops and do not swallow service failures; choose explicit restart/exit policy. Avoid reflection-driven discovery in startup paths for trimmed/AOT deployment. Test SIGTERM/CTRL+C shutdown, readiness transition, and process exit behavior in the deployment environment.

The host stack itself supports trimming-friendly static composition, but registrations, configuration binding, serializers, and plugin discovery added by the application may introduce warnings. Publish and smoke-test the actual RID-specific artifact. Emit startup, ready, stopping, and stopped telemetry; monitor worker throughput, failure count, queue depth, and shutdown duration without logging secrets or whole payloads.

### Operational signals

| Signal | Meaning/action | Privacy/cardinality rule |
| --- | --- | --- |
| Host started/stopping/stopped lifecycle events | Establishes deployment and graceful-shutdown timing | Include deployment-safe instance identity only |
| Hosted-service startup duration/failure | Identifies services delaying readiness or failing host startup | Use stable service type/category; redact configuration values |
| Background-service unhandled exception | Indicates lost work and, by default, host termination | Preserve exception and stable worker name; omit message payloads |
| Shutdown duration and unfinished-work count | Validates termination grace and drain policy | Use bounded work-type labels, not job/customer IDs |

### Troubleshooting

| Symptom | Likely causes and diagnostics | Safe corrective action | Retry suitability |
| --- | --- | --- | --- |
| Host never becomes ready | Blocking/slow `StartAsync`, invalid startup options, DI failure, or dependency work before readiness; inspect startup logs and stacks | Move long work after startup, validate config early, and bound dependency initialization | Let the supervisor restart only after fixing deterministic startup failures |
| Host exits after worker exception | Unhandled `BackgroundService` exception with stop-host behavior; inspect the preserved exception and failed work type | Fix/handle only expected failures, make work resumable, and retain stop-host supervision | Retry individual safe work through an owned policy; do not swallow fatal loops |
| Shutdown exceeds grace period | Service ignores cancellation, long in-flight work, deadlock, or timeout mismatch | Propagate stopping tokens, checkpoint work, and align shutdown timeout below platform grace | Restart cannot safely replace missing checkpoint/idempotency |
| Environment-specific config is wrong | Incorrect environment name, provider order, or deployment variables | Correct deployment configuration and restart the immutable artifact | Retry without config change repeats the failure |

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
