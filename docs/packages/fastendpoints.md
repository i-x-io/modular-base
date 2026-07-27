# FastEndpoints

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `FastEndpoints` | `8.2.0` | Endpoint framework and ASP.NET Core registration/middleware | Centrally pinned; catalog-only until an application project consumes it |

## Decision and scope

Use FastEndpoints as the API endpoint framework. It owns endpoint discovery, binding, validation integration, endpoint configuration, and endpoint authorization metadata. It is the foundation of the FastEndpoints/OpenAPI/Scalar pipeline documented in this catalog; it is not a replacement for ASP.NET Core authentication or authorization middleware.

## Recommended registration and use

Register FastEndpoints before building the application and place its middleware after authentication and authorization. This minimal pipeline was compiled against the pinned FastEndpoints 8.2.0, FastEndpoints.OpenApi 8.2.0, FastEndpoints.Security 8.2.0, Scalar.AspNetCore 2.16.16, Asp.Versioning.Http 10.0.0, JWT bearer 10.0.10, Microsoft.AspNetCore.OpenApi 10.0.10, and Microsoft.OpenApi 2.11.0 packages.

```csharp
using FastEndpoints;
using FastEndpoints.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddAuthorization();
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

## Enterprise implementation guidance

- Keep endpoint types, request DTOs, validators, and response DTOs in the same vertical slice.
- Use `AddFastEndpoints()` once at composition-root level. Use `UseFastEndpoints()` once after security middleware.
- Add `FastEndpoints.Generator` to every project that contains endpoint types when using generator-based startup or serializer contexts; it discovers DTOs through endpoint declarations.
- Keep non-FastEndpoints routes deliberate. `FastEndpoints.OpenApi` includes all discovered endpoints by default; use `ExcludeNonFastEndpoints` or a document filter when an API document must be framework-only.

## Integration with the catalog

- [FastEndpoints.Generator](fastendpoints-generator.md) removes reflection-based discovery and supports AOT-oriented generation.
- [FastEndpoints.OpenApi](fastendpoints-openapi.md) registers FastEndpoints-aware OpenAPI transformers; [Scalar.AspNetCore](scalar-aspnetcore.md) renders those documents.
- [FastEndpoints.Security](fastendpoints-security.md) supplies FastEndpoints JWT and access-control conveniences; [JWT bearer](microsoft-aspnetcore-authentication-jwtbearer.md) remains the production validation baseline.
- [Asp.Versioning.Http](asp-versioning-http.md) is separate versioning infrastructure. Coordinate document grouping explicitly rather than assuming it changes FastEndpoints endpoint versions.

## Security, performance, AOT, trimming, and operations

- Run `UseAuthentication()` before `UseAuthorization()`, and both before `UseFastEndpoints()`.
- Avoid exposing API explorer endpoints and sensitive schemas indiscriminately in production. Treat endpoint summaries, examples, and schemas as externally visible contract data.
- Reflection discovery is convenient for ordinary deployments. Native AOT requires the generator, `WebApplication.CreateSlimBuilder(args)`, `AddFastEndpoints(DiscoveredTypes.All)`, generated serializer/reflection registration, and explicit OpenAPI export; see [FastEndpoints.Generator](fastendpoints-generator.md).
- Emit structured request, authorization, and dependency telemetry outside secrets and bearer tokens. Do not log authorization headers or request bodies by default.

## Avoid

- Do not call `UseFastEndpoints()` before authentication/authorization when protected endpoints exist.
- Do not assume endpoints are anonymous; FastEndpoints secures them by default.
- Do not register raw `AddOpenApi()` for FastEndpoints documents; use `.OpenApiDocument()` from FastEndpoints.OpenApi.

## Verification checklist

- [ ] The consuming project references the package without a version; central package management supplies `8.2.0`.
- [ ] The application starts with `AddFastEndpoints()` and has exactly one `UseFastEndpoints()`.
- [ ] Protected endpoints return 401/403 as designed; intentionally public endpoints explicitly use `AllowAnonymous()`.
- [ ] `dotnet build` succeeds with the chosen OpenAPI dependency graph.

## Sources

- [FastEndpoints getting started](https://fast-endpoints.com/docs/get-started) — Accessed 2026-07-27.
- [FastEndpoints security](https://fast-endpoints.com/docs/security) — Accessed 2026-07-27.
- [FastEndpoints native AOT](https://fast-endpoints.com/docs/native-aot) — Accessed 2026-07-27.
