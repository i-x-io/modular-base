# Microsoft.Extensions.TimeProvider.Testing

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.8.0` | Test-only controllable `TimeProvider` implementation (`FakeTimeProvider`) | Approved test dependency only |

## Decision and scope

Use this package in tests to control wall-clock time, timestamps, and timers deterministically. Domain and application code depend on a consuming-project-owned `IClock` contract, which is not supplied by this catalog; infrastructure may implement that contract by wrapping `System.TimeProvider`, with production composition supplying `TimeProvider.System`. `FakeTimeProvider` must not be registered in production services.

## Recommended registration and use

Reference this package without a version from a test project only:

```xml
<PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />
```

Inject a consuming-project-owned `IClock` into domain/application code and adapt `TimeProvider` in infrastructure:

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class TimeProviderClock(TimeProvider timeProvider) : IClock
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}

services.AddSingleton(TimeProvider.System);
services.AddSingleton<IClock, TimeProviderClock>();
```

Use `FakeTimeProvider` to test the adapter and infrastructure delays without sleeping:

```csharp
using Microsoft.Extensions.Time.Testing;

var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
var fakeTime = new FakeTimeProvider(start);
var clock = new TimeProviderClock(fakeTime);

var delay = Task.Delay(TimeSpan.FromMinutes(5), fakeTime);
fakeTime.Advance(TimeSpan.FromMinutes(5));

await delay;
Assert.Equal(start.AddMinutes(5), clock.UtcNow);
```

For timers, create them through the injected provider, dispose them after the test, and advance time in explicit increments before asserting callback counts. Do not mix real and fake time in the same test.

## Enterprise implementation guidance

Make the `IClock` boundary explicit in services with business time rules, token/expiry handling, retry timing, and scheduled work. Keep the `TimeProvider` adapter in infrastructure. Store UTC instants, convert to local time only for presentation, and distinguish wall-clock decisions from monotonic elapsed-time measurement.

A deterministic workflow starts the fake provider at an explicit instant, constructs the system under test, starts the pending delay/timer operation, advances only enough time to cross one boundary, and then awaits/asserts the result. Test just-before, exact-boundary, and just-after cases. Set `LocalTimeZone` only for tests that intentionally cover daylight-saving or presentation behavior. `AutoAdvanceAmount` advances on time reads and can hide extra reads, so prefer explicit `Advance` for most behavior tests.

## Integration with the catalog

Register the production `IClock` implementation and its `TimeProvider.System` adapter through [DependencyInjection](microsoft-extensions-dependencyinjection.md); background jobs commonly execute under [Hosting](microsoft-extensions-hosting.md). Validate time-based options through [Options](microsoft-extensions-options.md), but do not bind an operational clock from configuration.

## Security, performance, AOT, trimming, and operations

Time governs token expiry, audit records, rate limiting, retries, and retention; use a trusted system clock in production and account for skew across hosts. Avoid `Task.Delay`, `DateTimeOffset.UtcNow`, or direct `TimeProvider` use in time-dependent domain/application services because they make tests nondeterministic and bypass `IClock`. Infrastructure that needs delay/timer behavior may use `Task.Delay(delay, timeProvider, cancellationToken)` or `timeProvider.CreateTimer`. Dispose timers and verify cancellation so callbacks do not leak across tests. `FakeTimeProvider` is a test utility for the infrastructure adapter and has no production deployment or trimming role.

## Avoid

- Do not ship or register `FakeTimeProvider` in production.
- Do not make `TimeProvider` the primary domain/application time contract; use the consuming project's `IClock`.
- Do not mix local `DateTime` values with UTC instants for durable business rules.
- Do not test timer behavior with real delays when controlled time is available.

## Verification checklist

- [ ] Only test projects reference the package, without a project-level version, and restore catalog version `10.8.0`.
- [ ] Production composition registers `IClock` through an adapter backed by `TimeProvider.System`; adapter tests use `FakeTimeProvider`.
- [ ] Expiry, retry, delay, timer, cancellation, and boundary cases advance fake time deterministically with no real sleeps.
- [ ] Timers are disposed and tests do not mix real and fake time.
- [ ] Clock-skew and UTC/local-time assumptions are documented where they affect security or operations.

## Sources

- [NuGet: Microsoft.Extensions.TimeProvider.Testing 10.8.0](https://www.nuget.org/packages/Microsoft.Extensions.TimeProvider.Testing/10.8.0) (Accessed 2026-07-27)
- [TimeProvider overview](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview) (Accessed 2026-07-27)
- [Testing with FakeTimeProvider](https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing) (Accessed 2026-07-27)
- [FakeTimeProvider API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.time.testing.faketimeprovider?view=net-10.0-pp) (Accessed 2026-07-27)
