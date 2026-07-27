# Microsoft.AspNetCore.Mvc.Testing

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.10` | In-memory ASP.NET Core functional test host and `WebApplicationFactory<TEntryPoint>` | Catalog-only; this repository intentionally has no test project |

## Decision and scope

Use this package in a future test project for functional/integration tests through an in-memory ASP.NET Core host. Despite its name, `WebApplicationFactory<TEntryPoint>` is useful for FastEndpoints applications because it hosts the ASP.NET Core application, not only MVC controllers.

## Recommended registration and use

Derive a test-only factory from `WebApplicationFactory<Program>` and create clients from the factory. Override the web host only for test-owned configuration and service replacements. With top-level `Program.cs`, make `Program` visible to the test assembly as required by the test project’s pattern.

The repository has no test project, so no sample is represented as repository-runnable.

## Enterprise implementation guidance

- Keep test host configuration isolated from production configuration and external dependencies.
- Replace database, external HTTP, queues, and secrets with controlled test equivalents; exercise a limited set of real infrastructure dependencies only in dedicated integration environments.
- Use factory instances deliberately: shared factory lifetime improves speed, but mutable shared state requires reset/isolation.
- Make content-root behavior explicit when tests need content files; the factory has conventions and attributes for locating it.

## Integration with the catalog

- [FastEndpoints.Testing](fastendpoints-testing.md) builds conveniences on the same in-memory-host model.
- [FastEndpoints](fastendpoints.md) describes the middleware order the host must run.
- [JWT bearer](microsoft-aspnetcore-authentication-jwtbearer.md) must be configured safely in a test-only environment.

## Security, performance, AOT, trimming, and operations

- Never add test authentication backdoors to production paths; constrain overrides to the factory/test environment.
- Reuse a factory where safe to avoid host startup cost, and reset state between tests.
- A `WebApplicationFactory` tests managed hosting behavior; it is not a substitute for testing a Native AOT published executable when that is a deployment target.
- Dispose factory-created resources and avoid cross-test external state.

## Avoid

- Do not add this dependency to a production application project.
- Do not rely on a machine-specific working directory or implicit content-root discovery in CI.
- Do not disable authorization globally for all test paths.

## Verification checklist

- [ ] A future test project references this package at central version `10.0.10`.
- [ ] Test-only service overrides cannot execute in production.
- [ ] Tests cover routing, binding, validation, authentication, authorization, and persistence boundaries.
- [ ] CI runs the test project from a clean working directory.

## Sources

- [Microsoft: integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [Microsoft API: Microsoft.AspNetCore.Mvc.Testing](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing?view=aspnetcore-10.0) — Accessed 2026-07-27.
