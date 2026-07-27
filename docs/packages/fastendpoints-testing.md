# FastEndpoints.Testing

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `FastEndpoints.Testing` | `8.2.0` | FastEndpoints integration/unit-test fixtures, route-less request helpers, and message receivers | Catalog-only; this repository intentionally has no test project |

## Decision and scope

Reserve this package for a future dedicated test project. It adds `AppFixture<TProgram>`, `TestBase`, test-service configuration, request helpers, and command/event receivers. It has no dependency on the main FastEndpoints package, so it can test compatible ASP.NET applications as well.

## Recommended registration and use

Reference the package only from a test project. All versions come from the central catalog:

```xml
<ItemGroup>
  <PackageReference Include="FastEndpoints.Testing" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
  <PackageReference Include="xunit.v3" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="../MyApi/MyApi.csproj" />
</ItemGroup>
```

Create a test-project-only fixture deriving from `AppFixture<Program>`; expose `public partial class Program` from the application entry point when `WebApplicationFactory` needs it. Use `ConfigureApp` for host configuration and `ConfigureServices` for test-only service replacement. Reuse a fixture type across test classes: its WebApplicationFactory/SUT cache avoids repeated host startup. Use `[DisableWafCache]` only when test isolation requires a new host per test class.

The route-less helpers derive the route and HTTP method from the endpoint type, keeping the test aligned with endpoint metadata:

```csharp
using FastEndpoints;
using FastEndpoints.Testing;
using Xunit;

public sealed class ApiFixture : AppFixture<Program>
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Replace only external dependencies with deterministic test services.
    }
}

public sealed class CreateOrderTests(ApiFixture app) : TestBase<ApiFixture>
{
    [Fact]
    public async Task Creates_an_order()
    {
        var (httpResponse, body) =
            await app.Client.POSTAsync<
                CreateOrderEndpoint,
                CreateOrderRequest,
                CreateOrderResponse>(new("SKU-42", 2));

        Assert.True(httpResponse.IsSuccessStatusCode);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal("SKU-42", body.Sku);
    }
}
```

The repository currently has no test project, so this example is a template for the first consuming application rather than a repository-runnable test. Once that project exists, run its scoped suite with `dotnet test path/to/MyApi.Tests.csproj`.

## Enterprise implementation guidance

- Keep test projects referencing application projects; production projects must never reference test projects.
- Use a small number of explicit fixture configurations per application profile and a state fixture for per-test-class mutable state.
- Use `appsettings.Testing.json` and `ConfigureServices` for isolated dependencies, not runtime switches that weaken production behavior.
- Use `RegisterTestEventReceivers()` and `RegisterTestCommandReceivers()` to assert messaging without replacing the production handlers.
- Prefer route-less request helpers for endpoint-focused tests; retain a small set of raw `HttpClient` tests for protocol concerns such as unknown routes, headers, content negotiation, and middleware outside FastEndpoints.
- Create authenticated clients with `CreateClient(...)` and test 401 separately from 403. Never reuse production credentials or signing material.

## Integration with the catalog

- [Microsoft.AspNetCore.Mvc.Testing](microsoft-aspnetcore-mvc-testing.md) provides the underlying `WebApplicationFactory<TEntryPoint>` model.
- [FastEndpoints](fastendpoints.md) provides endpoint types and route-less helpers used by this package.
- [FastEndpoints.Security](fastendpoints-security.md) and [JWT bearer](microsoft-aspnetcore-authentication-jwtbearer.md) require isolated test credentials/environments.

## Security, performance, AOT, trimming, and operations

- Cache app fixtures to reduce repeated host creation; disable caching only for deliberate isolation.
- Do not add a production bypass for authentication. Create isolated test environments and test-issued tokens.
- For dual AOT testing, configure the AOT executable, health endpoint, timeouts, and `Testing` environment explicitly.
- Native AOT tests are black-box process tests rather than in-memory `WebApplicationFactory` tests. Enable the documented `NativeAotTestMode` only in the test project and keep readiness endpoints free of sensitive diagnostics.
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
- [ ] Route-less helpers resolve the intended endpoint after route or DTO changes.
- [ ] If Native AOT is supported, the same suite passes in WAF and `NativeAotTestMode` black-box modes.

## Sources

- [FastEndpoints integration and unit testing](https://fast-endpoints.com/docs/integration-unit-testing) — Accessed 2026-07-27.
- [FastEndpoints Native AOT testing](https://fast-endpoints.com/docs/native-aot) — Accessed 2026-07-27.
- [FastEndpoints Testing API reference](https://api-ref.fast-endpoints.com/api/FastEndpoints.Testing.html) — Accessed 2026-07-27.
- [Microsoft: configure JWT bearer authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [FastEndpoints.Testing upstream source](https://github.com/FastEndpoints/FastEndpoints/tree/main/Src/Testing) — Accessed 2026-07-27.
- [NuGet: FastEndpoints.Testing 8.2.0](https://www.nuget.org/packages/FastEndpoints.Testing/8.2.0) — Accessed 2026-07-27.
