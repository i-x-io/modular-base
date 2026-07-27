# Testcontainers.Redis

## Catalog entry

`Testcontainers.Redis` **4.13.0** — test-only catalog package; disposable Redis containers for integration tests.

## Decision and scope

Use for integration tests that require actual Redis protocol, expiration, serialization, or connection behavior. It does not configure production cache clients or replace a dedicated performance environment.

## Recommended registration and use

Reference it only from `IsTestProject=true` projects. Use `RedisBuilder` in an xUnit asynchronous fixture, await startup, pass `GetConnectionString()` to the test-owned client, and asynchronously dispose the container. Use a deterministic image tag.

## Enterprise implementation guidance

Give every test a known key namespace and clear it before assertions. Limit fixture sharing to suites with a reset contract. Make Docker availability, image pull limits, resource caps, and failure-log retention explicit in CI.

## Integration with the catalog

Use alongside `xunit-v3.md`; production cache client guidance belongs with `Microsoft.Extensions.Caching.StackExchangeRedis`, not this test package. PostgreSQL containers are documented in `testcontainers-postgresql.md`.

## Security, performance, AOT, trimming, and operations

Treat Docker daemon access and container logs as sensitive operational surfaces. Redis startup and network I/O make these tests slower than unit tests. The package has no production AOT/trimming role.

## Avoid

Do not connect integration tests to a shared Redis server, use static host ports or `latest`, leave state for another test, or expose Docker credentials in test output.

## Verification checklist

- Run an isolated test that writes, reads, expires, and clears a namespaced key.
- Verify connection strings are resolved only after container startup.
- Confirm parallel test execution cannot share keys or leak state.
- Verify CI cleanup and bounded redacted logs after a failed container test.

## Sources

- https://www.nuget.org/packages/Testcontainers.Redis/4.13.0 (Accessed 2026-07-27)
- https://dotnet.testcontainers.org/modules/redis/ (Accessed 2026-07-27; Context7 consulted first)
