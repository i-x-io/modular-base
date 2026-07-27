# Microsoft.AspNetCore.Mvc.Testing

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.10` | In-memory ASP.NET Core functional test host and `WebApplicationFactory<TEntryPoint>` | Catalog-only; this repository intentionally has no test project |

## Decision and scope

Use this package in a future test project for functional/integration tests through an in-memory ASP.NET Core host. Despite its name, `WebApplicationFactory<TEntryPoint>` is useful for FastEndpoints applications because it hosts the ASP.NET Core application, not only MVC controllers.

## Recommended registration and use

Reference the package only from a future test project; central package management supplies the version:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
  <ProjectReference Include="../../src/MyApi/MyApi.csproj" />
</ItemGroup>
```

Derive a test-only factory from `WebApplicationFactory<Program>`, replace outbound dependencies in `ConfigureWebHost`, and create clients from that factory:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IClock>(new FakeClock(
                new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        });
    }
}

public sealed class HealthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_is_ready()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

The example assumes xUnit and test-owned `IClock`/`FakeClock` types. With top-level `Program.cs`, expose the entry point to the test assembly, commonly with `public partial class Program { }` in the API project. Prefer `ConfigureTestServices` for overrides so the test registration runs after application services.

The repository has no test project, so no sample is represented as repository-runnable.

## Enterprise implementation guidance

- Keep test host configuration isolated from production configuration and external dependencies.
- Replace database, external HTTP, queues, and secrets with controlled test equivalents; exercise a limited set of real infrastructure dependencies only in dedicated integration environments.
- Use factory instances deliberately: shared factory lifetime improves speed, but mutable shared state requires reset/isolation.
- Make content-root behavior explicit when tests need content files; the factory has conventions and attributes for locating it.
- For authentication tests, replace the handler with a test-only scheme and create explicit anonymous, authenticated, and forbidden cases. Do not bypass authorization middleware.
- For persistence tests, give each test an isolated database/schema or reset known state; an in-memory substitute does not prove provider-specific queries or transaction behavior.

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
- [ ] Outbound HTTP, queues, clocks, and other nondeterministic dependencies are controlled by the factory.

## Sources

- [Microsoft: integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [Microsoft API: Microsoft.AspNetCore.Mvc.Testing](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [Microsoft API: `WebApplicationFactory<TEntryPoint>`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [NuGet: Microsoft.AspNetCore.Mvc.Testing 10.0.10](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/10.0.10) — Accessed 2026-07-27.
