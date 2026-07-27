# Microsoft.Extensions.Hosting.Abstractions

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Host and hosted-service contracts including `IHost`, `IHostApplicationLifetime`, and `IHostedService` | Approved library-facing abstraction |

## Decision and scope

Reference this package when a reusable component needs host lifecycle contracts, especially an `IHostedService`, without taking a dependency on the Generic Host implementation. It does not build a host or configure application services.

## Recommended registration and use

Implement `IHostedService` or derive from `BackgroundService` for host-managed work. Start quickly, run asynchronous work after startup, honor both start/stop cancellation, and make the operation idempotent enough for the host lifecycle. Register the implementation through the application's hosting/DI composition root.

## Enterprise implementation guidance

Define the work's delivery guarantee, shutdown drain/checkpoint limit, failure policy, and observability. Split independent workers instead of building one large supervisor service. Keep infrastructure lifecycle code separate from domain processing logic so it remains testable.

## Integration with the catalog

[Hosting](microsoft-extensions-hosting.md) provides the Generic Host and registration APIs. Use [DependencyInjection.Abstractions](microsoft-extensions-dependencyinjection-abstractions.md) for dependencies and [Logging.Abstractions](microsoft-extensions-logging-abstractions.md) for safe structured diagnostics.

## Security, performance, AOT, trimming, and operations

Background services run with process authority; constrain credentials and tenant scope. Honor `StopAsync` cancellation and avoid unbounded queues/memory. These contracts are AOT/trimming-friendly; dynamic worker discovery requires separate publish-mode validation.

## Avoid

- Do not register a background service as a singleton and separately as a hosted service unless the identity/lifetime is intentional.
- Do not block `StartAsync` on endless work.
- Do not ignore stop signals or background exceptions.

## Verification checklist

- Start, run, failure, and cancellation paths are covered.
- Shutdown drains/checkpoints according to the documented delivery guarantee.
- The worker has least-privilege credentials and useful logs/metrics.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Hosting.Abstractions) (Accessed 2026-07-27)
- [Hosted services in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers) (Accessed 2026-07-27)
