# xunit.v3

## Catalog entry

`xunit.v3` **3.2.2** — test-only catalog package; preferred xUnit v3 framework and Microsoft Testing Platform (MTP) runner stack for new tests. Its package metadata installs `xunit.v3.mtp-v1`.

## Decision and scope

Use for all new unit and integration tests. xUnit v3 is the preferred replacement for catalog-only SpecsFor. Keep tests deterministic, isolated, and focused on externally meaningful behavior.

## Recommended registration and use

Create an `IsTestProject=true` project, reference the catalog's `xunit.v3`, and run it through the MTP-compatible `dotnet test` path. Mark parameterless tests with `[Fact]`; use asynchronous APIs and fixtures for external resources rather than blocking calls. Do not add the cataloged VSTest SDK/adapter/collector packages to this MTP configuration.

## Enterprise implementation guidance

Keep unit tests free of time, random, filesystem, and network nondeterminism through injected abstractions. Use collection/fixture sharing only with a clear lifecycle and reset contract. Partition Docker-backed integration tests from fast unit tests and keep test names behavior-oriented.

## Integration with the catalog

The cataloged VSTest alternative and its required central package addition are documented in `xunit-runner-visualstudio.md` and `microsoft-net-test-sdk.md`; do not mix them with this package. Assertions and containers are covered by `awesomeassertions.md` and the Testcontainers documents. The cataloged `coverlet.collector` is VSTest-only, so MTP coverage needs a separately approved MTP-native package.

## Security, performance, AOT, trimming, and operations

Test code can execute arbitrary fixtures and external calls. Never put production secrets in test configuration or assertion messages. Test frameworks have no production AOT/trimming role; published-artifact testing must be designed separately when needed.

## Avoid

Do not use SpecsFor for new tests, mix VSTest adapter/collector packages into this MTP stack, hide dependencies in shared mutable fixtures, block on asynchronous work, or make tests rely on execution order.

## Verification checklist

- Run a `[Fact]` test through the MTP-compatible `dotnet test` path.
- Run unit and Docker-backed integration categories independently.
- Confirm failing tests expose useful diagnostics without secrets and pass repeatedly in a clean environment.

## Sources

- https://www.nuget.org/packages/xunit.v3/3.2.2 (Accessed 2026-07-27)
- https://xunit.net/docs/getting-started/v3/getting-started (Accessed 2026-07-27; Context7 consulted first)
- https://github.com/xunit/xunit (Accessed 2026-07-27)
