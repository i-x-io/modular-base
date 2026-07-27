# Testcontainers.PostgreSql

## Catalog entry

`Testcontainers.PostgreSql` **4.13.0** — test-only catalog package; disposable PostgreSQL containers for integration tests.

## Decision and scope

Use for integration tests that need PostgreSQL behavior unavailable from unit-test doubles. It is not a local-development database manager or a production provisioning mechanism.

## Recommended registration and use

Reference it only from `IsTestProject=true` projects. Start a `PostgreSqlContainer` through xUnit's asynchronous fixture lifecycle, obtain its connection string only after startup, and dispose it asynchronously. Use a deterministic image tag and an application-owned schema setup.

## Enterprise implementation guidance

Share a fixture only when tests can reset data safely; otherwise isolate containers. Bound startup time, record container logs on failures, control parallelism for scarce Docker resources, and keep credentials/test data ephemeral. Run Docker-capable integration tests in a separately labeled CI job.

## Integration with the catalog

Use with `xunit-v3.md` and the catalog's `Npgsql`/EF Core packages where a real provider test is required. Redis integration has separate lifecycle guidance in `testcontainers-redis.md`.

## Security, performance, AOT, trimming, and operations

Docker access is privileged infrastructure: restrict the daemon/socket and do not mount host secrets or broad host directories. Container startup dominates test cost; reuse only with reliable cleanup. Testcontainers is test infrastructure and has no production NativeAOT/trimming role.

## Avoid

Do not point tests at a shared production-like database, use `latest` image tags, hard-code host ports, or parallelize stateful tests without isolation/reset policy.

## Verification checklist

- Run a Docker-backed test that starts PostgreSQL and uses the post-start connection string.
- Verify migrations/schema initialization and cleanup from a clean environment.
- Force a test failure and confirm CI retains bounded, non-secret container diagnostics.
- Verify the CI worker has Docker access only for the integration-test job.

## Sources

- https://www.nuget.org/packages/Testcontainers.PostgreSql/4.13.0 (Accessed 2026-07-27)
- https://dotnet.testcontainers.org/modules/postgres/ (Accessed 2026-07-27; Context7 consulted first)
