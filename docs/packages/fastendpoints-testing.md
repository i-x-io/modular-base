# FastEndpoints.Testing

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `FastEndpoints.Testing` | `8.2.0` | FastEndpoints integration/unit-test fixtures, route-less request helpers, and message receivers | Catalog-only; this repository intentionally has no test project |

- Owner: IX
- Last reviewed: 2026-07-27
- Review trigger: FastEndpoints.Testing, xUnit, MVC Testing, target framework, fixture-cache, or Native AOT test-mode changes.

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

| Setting / hook | Purpose | Default behavior | Test guidance | Reload / sensitivity / failure behavior |
| --- | --- | --- | --- | --- |
| `ConfigureApp` | Overrides host/application configuration | Application configuration is used | Supply only test-environment values and deterministic endpoints | Applied when the fixture host starts; missing values should fail setup visibly |
| `ConfigureServices` | Replaces or adds test services | Application services remain registered | Replace external boundaries without bypassing authorization/routing | Applied at host creation; fixture caching retains the resulting graph |
| `[DisableWafCache]` | Creates a fresh host instead of reusing the fixture cache | Compatible fixture instances are cached | Use only when host isolation is required | Increases startup cost; does not itself isolate external state |
| `NativeAotTestMode` | Runs tests against a published executable | In-memory WAF mode | Enable only in the test project with explicit executable/readiness configuration | Process configuration; readiness/timeout failures should fail the suite |

### Upgrade and rollback

Upgrade with the FastEndpoints runtime family, `Microsoft.AspNetCore.Mvc.Testing`, and the chosen xUnit v3 packages after checking fixture/cache and route-less helper changes. Rebuild the test host and run representative WAF and Native AOT modes before accepting the upgrade.

Rollback only the test dependency set if no production assembly depends on it, but keep the test packages mutually compatible. A rollback that makes tests green by no longer exercising the deployed runtime is invalid; align the fixture packages with the application version under test.

## Integration with the catalog

- [Microsoft.AspNetCore.Mvc.Testing](microsoft-aspnetcore-mvc-testing.md) provides the underlying `WebApplicationFactory<TEntryPoint>` model.
- Central transitive pinning is disabled: FastEndpoints.Testing 8.2.0 declares MVC Testing 10.0.9 for `net10.0`. The direct versionless MVC Testing reference shown above intentionally selects the catalog's 10.0.10 servicing pin and exposes its APIs to test code.
- [FastEndpoints](fastendpoints.md) provides endpoint types and route-less helpers used by this package.
- [FastEndpoints.Security](fastendpoints-security.md) and [JWT bearer](microsoft-aspnetcore-authentication-jwtbearer.md) require isolated test credentials/environments.
- The [package-selection guide](../package-guidance/package-selection.md#api-authentication-ownership) clarifies which authentication registration the test host must reproduce or replace.
- The [FastEndpoints, JWT, OpenAPI, and Scalar recipe](../recipes/fastendpoints-jwt-openapi-scalar.md) defines the production pipeline that integration tests should preserve.
- Review [FastEndpoints.Testing supply-chain metadata](../package-guidance/supply-chain.md#fastendpoints-testing) before approval or upgrade.

## Security, performance, AOT, trimming, and operations

- Cache app fixtures to reduce repeated host creation; disable caching only for deliberate isolation.
- Do not add a production bypass for authentication. Create isolated test environments and test-issued tokens.
- For dual AOT testing, configure the AOT executable, health endpoint, timeouts, and `Testing` environment explicitly.
- Native AOT tests are black-box process tests rather than in-memory `WebApplicationFactory` tests. Enable the documented `NativeAotTestMode` only in the test project and keep readiness endpoints free of sensitive diagnostics.
- Dispose custom `HttpClient` instances and external resources in fixture teardown.

### Operational signals and troubleshooting

Testing has no production signals. In CI, record bounded host-start duration, test duration, fixture mode, exit status, and sanitized readiness failures; never attach test tokens, connection strings, or captured sensitive bodies to shared logs.

| Symptom | Likely cause and diagnostic | Safe corrective action | Retry? |
| --- | --- | --- | --- |
| Fixture cannot locate/start the application | Entry point is inaccessible, content root is wrong, or startup requires unavailable production configuration | Expose the documented partial `Program`, set test configuration/content root, and replace external boundaries | Retry only after setup is corrected |
| Tests pass alone but fail together | Cached host or external state leaks across tests; inspect fixture/state lifetimes | Reset state, use isolated resources, or deliberately disable WAF cache for that fixture | Not as a blanket flaky-test retry |
| Route-less helper targets no endpoint | Endpoint type/route metadata changed or the application assembly is not discovered | Update the typed request and ensure the real endpoint assembly is loaded | No |
| Native AOT mode times out | Executable path, environment, readiness route, port, or startup failure is wrong | Inspect sanitized process output and correct the explicit AOT fixture configuration | Retry only for a proven transient resource condition |

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
