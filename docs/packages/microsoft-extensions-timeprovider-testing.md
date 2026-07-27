# Microsoft.Extensions.TimeProvider.Testing

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.8.0` | Test-only controllable `TimeProvider` implementation (`FakeTimeProvider`) | Approved test dependency only |

## Decision and scope

Use this package in tests to control wall-clock time, timestamps, and timers deterministically. Domain and application code depend on a consuming-project-owned `IClock` contract, which is not supplied by this catalog; infrastructure may implement that contract by wrapping `System.TimeProvider`, with production composition supplying `TimeProvider.System`. `FakeTimeProvider` must not be registered in production services.

## Recommended registration and use

Inject `IClock` into domain and application components that need business time. Keep `TimeProvider` in an infrastructure adapter for current time, elapsed-time measurement, delays, or timers. In adapter tests, create a `FakeTimeProvider`, pass it to the adapter, and advance time explicitly rather than waiting. Use its timer support to test scheduled/timeout behavior without real sleeps.

## Enterprise implementation guidance

Make the `IClock` boundary explicit in services with business time rules, token/expiry handling, retry timing, and scheduled work. Keep the `TimeProvider` adapter in infrastructure. Store UTC instants, convert to local time only for presentation, and distinguish wall-clock decisions from monotonic elapsed-time measurement. Test daylight-saving, expiry boundary, and timer-cancellation logic with controlled time.

## Integration with the catalog

Register the production `IClock` implementation and its `TimeProvider.System` adapter through [DependencyInjection](microsoft-extensions-dependencyinjection.md); background jobs commonly execute under [Hosting](microsoft-extensions-hosting.md). Validate time-based options through [Options](microsoft-extensions-options.md), but do not bind an operational clock from configuration.

## Security, performance, AOT, trimming, and operations

Time governs token expiry, audit records, rate limiting, retries, and retention; use a trusted system clock in production and account for skew across hosts. Avoid `Task.Delay`, `DateTimeOffset.UtcNow`, or direct `TimeProvider` use in time-dependent domain/application services because they make tests nondeterministic and bypass `IClock`. `FakeTimeProvider` is a test utility for the infrastructure adapter and has no production deployment or trimming role.

## Avoid

- Do not ship or register `FakeTimeProvider` in production.
- Do not make `TimeProvider` the primary domain/application time contract; use the consuming project's `IClock`.
- Do not mix local `DateTime` values with UTC instants for durable business rules.
- Do not test timer behavior with real delays when controlled time is available.

## Verification checklist

- Production composition registers `IClock` through an adapter backed by `TimeProvider.System`; adapter tests use `FakeTimeProvider`.
- Expiry, retry, timer, cancellation, and boundary conditions advance fake time deterministically.
- Clock-skew and UTC/local-time assumptions are documented where they affect security or operations.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.TimeProvider.Testing) (Accessed 2026-07-27)
- [TimeProvider overview](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview) (Accessed 2026-07-27)
- [FakeTimeProvider API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.time.testing.faketimeprovider) (Accessed 2026-07-27)
