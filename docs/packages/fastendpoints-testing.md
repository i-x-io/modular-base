# FastEndpoints.Testing

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `FastEndpoints.Testing` | `8.2.0` | FastEndpoints integration/unit-test fixtures, route-less request helpers, and message receivers | Catalog-only; this repository intentionally has no test project |

## Decision and scope

Reserve this package for a future dedicated test project. It adds `AppFixture<TProgram>`, `TestBase`, test-service configuration, request helpers, and command/event receivers. It has no dependency on the main FastEndpoints package, so it can test compatible ASP.NET applications as well.

## Recommended registration and use

Create a test-project-only fixture deriving from `AppFixture<Program>`; use `ConfigureApp` for host configuration and `ConfigureServices` for test-only service replacement. Reuse a fixture type across test classes: its WebApplicationFactory/SUT cache avoids repeated host startup. Use `[DisableWafCache]` only when test isolation requires a new host per test class.

The repository has no test project, so no test snippet or command is presented as repository-runnable.

## Enterprise implementation guidance

- Keep test projects referencing application projects; production projects must never reference test projects.
- Use a small number of explicit fixture configurations per application profile and a state fixture for per-test-class mutable state.
- Use `appsettings.Testing.json` and `ConfigureServices` for isolated dependencies, not runtime switches that weaken production behavior.
- Use `RegisterTestEventReceivers()` and `RegisterTestCommandReceivers()` to assert messaging without replacing the production handlers.

## Integration with the catalog

- [Microsoft.AspNetCore.Mvc.Testing](microsoft-aspnetcore-mvc-testing.md) provides the underlying `WebApplicationFactory<TEntryPoint>` model.
- [FastEndpoints](fastendpoints.md) provides endpoint types and route-less helpers used by this package.
- [FastEndpoints.Security](fastendpoints-security.md) and [JWT bearer](microsoft-aspnetcore-authentication-jwtbearer.md) require isolated test credentials/environments.

## Security, performance, AOT, trimming, and operations

- Cache app fixtures to reduce repeated host creation; disable caching only for deliberate isolation.
- Do not add a production bypass for authentication. Create isolated test environments and test-issued tokens.
- For dual AOT testing, configure the AOT executable, health endpoint, timeouts, and `Testing` environment explicitly.
- Dispose custom `HttpClient` instances and external resources in fixture teardown.

## Avoid

- Do not place this reference in a production project.
- Do not share mutable state across independent tests without a declared collection/state-fixture strategy.
- Do not disable security broadly for tests; scope changes to the test host only.

## Verification checklist

- [ ] A future test project references this package at central version `8.2.0`.
- [ ] The application project does not reference the test project.
- [ ] Fixture configuration keeps external systems isolated and teardown disposes resources.
- [ ] Secure endpoint tests use test-only tokens/configuration and cover 401/403 behavior.

## Sources

- [FastEndpoints integration and unit testing](https://fast-endpoints.com/docs/integration-unit-testing) — Accessed 2026-07-27.
- [FastEndpoints Testing API reference](https://api-ref.fast-endpoints.com/api/FastEndpoints.Testing.html) — Accessed 2026-07-27.
- [Microsoft: configure JWT bearer authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0) — Accessed 2026-07-27.
