# Microsoft.Extensions.Hosting

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Generic Host implementation, configuration, DI, lifetime, and hosted-service integration | Approved application host implementation |

## Decision and scope

Use the Generic Host as the application composition and lifecycle boundary for services, workers, and console applications. It implements the hosting contracts and owns the root DI provider, configuration pipeline, logging, and graceful shutdown coordination.

## Recommended registration and use

Create one host, register infrastructure/services before `Build`, and use `IHostedService`/`BackgroundService` for managed background work. Make start and stop cancellation-aware. Keep business workflows out of `Program` and expose feature registration through owned extension methods.

## Enterprise implementation guidance

Set the environment, configuration sources, logging, shutdown period, and host lifetime intentionally. Treat startup as a validation gate: options and required dependencies should fail clearly before accepting work. Coordinate background consumers so stop drains or checkpoints according to each data-delivery contract.

## Integration with the catalog

[Hosting.Abstractions](microsoft-extensions-hosting-abstractions.md) is appropriate for reusable hosted-service libraries. [DependencyInjection](microsoft-extensions-dependencyinjection.md), [Options](microsoft-extensions-options.md), and [Logging.Abstractions](microsoft-extensions-logging-abstractions.md) form the standard host stack.

## Security, performance, AOT, trimming, and operations

Do not execute unbounded blocking work in host startup. Enforce cancellation in background loops and do not swallow service failures; choose explicit restart/exit policy. Avoid reflection-driven discovery in startup paths for trimmed/AOT deployment. Test SIGTERM/CTRL+C shutdown, readiness transition, and process exit behavior in the deployment environment.

## Avoid

- Do not build multiple root hosts/providers in one process without a defined ownership model.
- Do not start detached background tasks outside host lifecycle management.
- Do not assume startup validation proves remote dependencies remain healthy.

## Verification checklist

- Startup validates required options and registrations before serving work.
- Hosted services start, stop, and honor cancellation deterministically.
- Deployment shutdown/probe behavior is integration-tested.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Hosting) (Accessed 2026-07-27)
- [Generic Host in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host) (Accessed 2026-07-27)
