# FastEndpoints

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `FastEndpoints` | `8.2.0` | Endpoint framework and ASP.NET Core registration/middleware | Catalog-only; centrally pinned until an application project consumes it |

- Owner: IX
- Last reviewed: 2026-07-27
- Review trigger: FastEndpoints version, target framework, endpoint discovery/binding behavior, or ASP.NET Core middleware changes.

## Decision and scope

Use FastEndpoints as the API endpoint framework. It owns endpoint discovery, binding, validation integration, endpoint configuration, and endpoint authorization metadata. It is the foundation of the FastEndpoints/OpenAPI/Scalar pipeline documented in this catalog; it is not a replacement for ASP.NET Core authentication or authorization middleware.

## Recommended registration and use

The complete composition below uses direct versionless references for FastEndpoints, its OpenAPI integration, JWT bearer authentication, and Scalar. Central package management supplies every version:

```xml
<ItemGroup>
  <PackageReference Include="FastEndpoints" />
  <PackageReference Include="FastEndpoints.OpenApi" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
  <PackageReference Include="Scalar.AspNetCore" />
</ItemGroup>
```

Keep a request, response, and endpoint in one vertical slice. Returning the response DTO from `ExecuteAsync()` also makes the handler straightforward to unit test:

```csharp
using FastEndpoints;

public sealed record CreateOrderRequest(string Sku, int Quantity);
public sealed record CreateOrderResponse(Guid Id, string Sku, int Quantity);

public sealed class CreateOrderEndpoint
    : Endpoint<CreateOrderRequest, CreateOrderResponse>
{
    public override void Configure()
    {
        Post("/orders");
        Policies("orders.write");
    }

    public override Task<CreateOrderResponse> ExecuteAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var response = new CreateOrderResponse(
            Guid.NewGuid(), request.Sku, request.Quantity);

        return Task.FromResult(response);
    }
}
```

Register FastEndpoints before building the application and place its middleware after authentication and authorization. This is the complete composition workflow; the companion guides document only their package-specific additions:

```csharp
using FastEndpoints;
using FastEndpoints.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddAuthorization(options =>
    options.AddPolicy("orders.write", policy =>
        policy.RequireClaim("scope", "orders.write")));
builder.Services
    .AddFastEndpoints()
    .OpenApiDocument(o => o.DocumentName = "v1");

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.AddDocuments("v1"));
}
app.Run();
```

Endpoints are secure by default. Each endpoint that is intentionally public must call `AllowAnonymous()`; use endpoint `Roles()`, `Claims()`, `Policies()`, `Permissions()`, or `Scopes()` only after configuring the corresponding ASP.NET Core security services. Production OpenAPI and Scalar exposure requires an explicit protected authorization policy; do not expose either route by default.

A common delivery workflow is:

1. Add the versionless package reference to the web project.
2. Add request/response DTOs and an endpoint; make anonymous access explicit rather than relying on a global bypass.
3. Register application services, authentication, authorization, `AddFastEndpoints()`, and any OpenAPI documents.
4. Order middleware as authentication, authorization, then `UseFastEndpoints()`.
5. Exercise one anonymous route, one authenticated success, one 401 response, and one 403 response before publishing.

Material composition settings should remain explicit at the host boundary:

| Setting / call | Purpose | Default behavior | Production guidance | Reload / sensitivity / failure behavior |
| --- | --- | --- | --- | --- |
| `AddFastEndpoints()` | Registers endpoint discovery and framework services | No endpoints are registered until called | Call once in the composition root; pass generated discovered types for the documented AOT path | Startup-only; missing registration fails application composition |
| `UseFastEndpoints()` | Maps and executes discovered endpoints | No FastEndpoints routes are mapped until called | Call once after authentication and authorization | Startup-only; wrong order changes security behavior |
| `AllowAnonymous()` | Opts an endpoint out of secure-by-default behavior | Endpoints require authorization | Use only on reviewed public routes | Compile/startup metadata; accidental use expands access |

## Enterprise implementation guidance

- Keep endpoint types, request DTOs, validators, and response DTOs in the same vertical slice.
- Use `AddFastEndpoints()` once at composition-root level. Use `UseFastEndpoints()` once after security middleware.
- Prefer strongly typed `Endpoint<TRequest,TResponse>` contracts when clients and tests need stable request/response types; use the request-less base types for routes that genuinely have no request DTO.
- Keep persistence and external calls behind injected application services. The endpoint should coordinate binding, authorization, and the use case rather than become the domain layer.
- Add `FastEndpoints.Generator` to every project that contains endpoint types when using generator-based startup or serializer contexts; it discovers DTOs through endpoint declarations.
- Keep non-FastEndpoints routes deliberate. `FastEndpoints.OpenApi` includes all discovered endpoints by default; use `ExcludeNonFastEndpoints` or a document filter when an API document must be framework-only.

### Upgrade and rollback

Move `FastEndpoints`, `FastEndpoints.Generator`, `FastEndpoints.OpenApi`, `FastEndpoints.Security`, and `FastEndpoints.Testing` as one reviewed 8.x family unless upstream compatibility explicitly permits a split. Check release notes for binding, validation, endpoint discovery, middleware, serializer, and security-metadata changes; rebuild generated artifacts and run protocol-level endpoint tests before deployment.

Rollback the family pin and deployed artifact together. If endpoint routes or DTO contracts changed in the same release, retain backward-compatible routes/contracts or roll those application changes back as well; a package-only rollback cannot repair an already-published breaking API contract.

## Integration with the catalog

- [FastEndpoints.Generator](fastendpoints-generator.md) removes reflection-based discovery and supports AOT-oriented generation.
- [FastEndpoints.OpenApi](fastendpoints-openapi.md) registers FastEndpoints-aware OpenAPI transformers; [Scalar.AspNetCore](scalar-aspnetcore.md) renders those documents.
- [FastEndpoints.Security](fastendpoints-security.md) supplies FastEndpoints JWT and access-control conveniences; [JWT bearer](microsoft-aspnetcore-authentication-jwtbearer.md) remains the production validation baseline.
- [Asp.Versioning.Http](asp-versioning-http.md) is separate versioning infrastructure. Coordinate document grouping explicitly rather than assuming it changes FastEndpoints endpoint versions.
- The [package-selection guide](../package-guidance/package-selection.md#api-authentication-ownership) identifies whether FastEndpoints conveniences or ASP.NET Core JWT bearer owns authentication registration.
- Follow the [FastEndpoints, JWT, OpenAPI, and Scalar recipe](../recipes/fastendpoints-jwt-openapi-scalar.md) or the [validation and results recipe](../recipes/fastendpoints-validation-results.md) for complete composition examples.
- Review [FastEndpoints supply-chain metadata](../package-guidance/supply-chain.md#fastendpoints) before approval or upgrade.

## Security, performance, AOT, trimming, and operations

- Run `UseAuthentication()` before `UseAuthorization()`, and both before `UseFastEndpoints()`.
- Bound request sizes and validation work at the edge, pass cancellation tokens to I/O, and avoid buffering large payloads in endpoint handlers.
- Avoid exposing API explorer endpoints and sensitive schemas indiscriminately in production. Treat endpoint summaries, examples, and schemas as externally visible contract data.
- Reflection discovery is convenient for ordinary deployments. Native AOT requires the generator, `WebApplication.CreateSlimBuilder(args)`, `AddFastEndpoints(DiscoveredTypes.All)`, generated serializer/reflection registration, and explicit OpenAPI export; see [FastEndpoints.Generator](fastendpoints-generator.md).
- Emit structured request, authorization, and dependency telemetry outside secrets and bearer tokens. Do not log authorization headers or request bodies by default.

### Operational signals and troubleshooting

Use ASP.NET Core request metrics/traces and structured application logs to observe route, method, status, duration, validation rejection, and downstream dependency outcomes. Keep route templates bounded; never attach bearer tokens, raw bodies, passwords, or sensitive claims.

| Symptom | Likely cause and diagnostic | Safe corrective action | Retry? |
| --- | --- | --- | --- |
| Endpoint returns 404 or is absent at startup | Endpoint assembly was not discovered, generated types were omitted, or `UseFastEndpoints()` was not called; inspect startup registration and route inventory | Register the declaring assembly/generated discovered types and map middleware once | No |
| Protected endpoint returns unexpected 401/403 | Authentication/authorization middleware order, scheme, claim, or endpoint policy mismatch; inspect sanitized auth diagnostics and endpoint metadata | Restore middleware order and align the policy/claim contract | Only after credentials or policy are corrected |
| Binding/validation rejects a valid-looking request | DTO shape, content type, serializer option, validator, or route parameter differs from the contract; inspect validation errors without logging sensitive input | Correct the client contract or reviewed binding/validator configuration | Only after correcting input |
| Latency or allocation rises after a release | Handler is buffering work, performing blocking I/O, or lost generated/AOT configuration; compare route-level traces and deployment settings | Stream/bound payloads, propagate cancellation, and restore the intended discovery/serializer path | Do not blindly retry non-idempotent work |

## Avoid

- Do not call `UseFastEndpoints()` before authentication/authorization when protected endpoints exist.
- Do not assume endpoints are anonymous; FastEndpoints secures them by default.
- Do not register raw `AddOpenApi()` for FastEndpoints documents; use `.OpenApiDocument()` from FastEndpoints.OpenApi.

## Verification checklist

- [ ] The consuming project references the package without a version; central package management supplies `8.2.0`.
- [ ] The application starts with `AddFastEndpoints()` and has exactly one `UseFastEndpoints()`.
- [ ] Endpoint DTO binding and validation are covered for valid, invalid, and cancellation paths.
- [ ] Protected endpoints return 401/403 as designed; intentionally public endpoints explicitly use `AllowAnonymous()`.
- [ ] `dotnet build` and the scoped integration-test project succeed with the chosen dependency graph.

## Sources

- [FastEndpoints getting started and endpoint types](https://fast-endpoints.com/docs) — Accessed 2026-07-27.
- [FastEndpoints security](https://fast-endpoints.com/docs/security) — Accessed 2026-07-27.
- [FastEndpoints native AOT](https://fast-endpoints.com/docs/native-aot) — Accessed 2026-07-27.
- [FastEndpoints upstream repository](https://github.com/FastEndpoints/FastEndpoints) — Accessed 2026-07-27.
- [NuGet: FastEndpoints 8.2.0](https://www.nuget.org/packages/FastEndpoints/8.2.0) — Accessed 2026-07-27.
