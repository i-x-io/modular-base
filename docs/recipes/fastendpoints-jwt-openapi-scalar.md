# Secure FastEndpoints API with JWT, OpenAPI, and Scalar

## Problem and boundary

After recorded architecture/adoption approval, this recipe composes a protected FastEndpoints API with a live OpenAPI contract and a Scalar explorer. ASP.NET Core JWT bearer authentication owns token validation, ASP.NET Core authorization owns policy evaluation, FastEndpoints owns endpoint discovery and execution, `FastEndpoints.OpenApi` owns document generation, and Scalar only renders that document. The API does not issue tokens, and neither the OpenAPI description nor Scalar enforces access.

## Required packages

The following Web SDK block is a standalone application illustration outside
this repository's enforced project graph. All four packages in the composition
are catalog-only. Their central `PackageVersion` entries manage versions but do
not approve consumption. Record explicit architecture/adoption approval for the
complete composition, including the Scalar UI and route-exposure decision,
before adding these references:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FastEndpoints" />
    <PackageReference Include="FastEndpoints.OpenApi" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <PackageReference Include="Scalar.AspNetCore" />
  </ItemGroup>
</Project>
```

`FastEndpoints.Security` is intentionally absent: this boundary validates access tokens issued by an external OAuth/OIDC authority through ASP.NET Core's JWT bearer handler. Add the FastEndpoints security package only if its issuance or permission conveniences receive separate architecture/adoption approval.

## Composition

After the composition is approved, configure authentication, authorization, endpoint discovery, and one named OpenAPI document before building the host:

```csharp
using FastEndpoints;
using FastEndpoints.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var authority = builder.Configuration["Authentication:Authority"]
    ?? throw new InvalidOperationException("Authentication:Authority is required.");
var audience = builder.Configuration["Authentication:Audience"]
    ?? throw new InvalidOperationException("Authentication:Audience is required.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("orders.read", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "orders.read");
    });

builder.Services
    .AddFastEndpoints()
    .OpenApiDocument(options =>
    {
        options.DocumentName = "v1";
        options.Title = "Orders API";
        options.Version = "v1";
        options.ExcludeNonFastEndpoints = true;
    });
```

Missing authority or audience settings fail startup instead of silently accepting a weaker token-validation mode. The bearer handler validates the token signature through authority metadata as well as issuer, audience, and lifetime using its normal defaults. `MapInboundClaims = false` preserves issuer claim names such as `sub` and `scope`. The policy name is application-owned and becomes the stable bridge between issuer claims and endpoint authorization. If an issuer emits a space-delimited `scope` claim rather than one value per claim, use a reviewed assertion or authorization handler that parses that exact claim contract.

Map middleware and documentation in security-sensitive order:

```csharp
var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.AddDocuments("v1"));
}

await app.RunAsync();
```

Authentication must populate `HttpContext.User` before authorization evaluates the policy, and both middleware components must run before FastEndpoints executes protected endpoints. The document name `v1` is identical at production and UI boundaries. Development-only mapping avoids exposing route shapes and interactive request tooling by default. If production documentation is required, protect both `MapOpenApi()` and `MapScalarApiReference()` with the same explicit authorization policy, or serve a reviewed static export through an equally protected route.

Declare a protected endpoint; FastEndpoints endpoints are protected unless `AllowAnonymous()` is explicit:

```csharp
using FastEndpoints;

public sealed record OrderSummary(Guid Id, string Number);

public sealed class GetOrderEndpoint : EndpointWithoutRequest<OrderSummary>
{
    public override void Configure()
    {
        Get("/orders/{id:guid}");
        Policies("orders.read");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id");

        await Send.OkAsync(
            new OrderSummary(id, $"ORD-{id:N}"),
            cancellationToken);
    }
}
```

FastEndpoints binds the route value and applies the named ASP.NET Core policy before calling the handler. A missing or invalid token produces a bearer challenge (`401`); a valid principal without the required authorization produces `403`. The sample response is deterministic documentation data. A production handler should call an injected application service, pass the cancellation token, return `404` without leaking inaccessible resource existence when policy requires it, and never put tokens or sensitive claims in responses or logs.

## Failure modes and operations

| Symptom | Likely boundary | Observation and safe response |
| --- | --- | --- |
| Every protected call returns `401` | JWT bearer validation | Check authority metadata reachability, issuer, audience, signature-key rollover, and server clock. Log a stable failure category and correlation identifier, never the token. |
| Authenticated callers receive `403` | Authorization policy | Inspect the normalized principal's claim names and the issuer's documented scope representation. Change the claim-to-policy adapter, not token validation. |
| `/openapi/v1.json` returns `404` | Document production/routing | Confirm `.OpenApiDocument()` and Scalar both use the exact `v1` name and that routes are mapped in the current environment. |
| Scalar loads but calls fail | Client/explorer configuration | Verify the document server URL, HTTPS trust, CORS boundary, and a non-production test token. Scalar does not bypass API authorization. |
| Startup fails after identity-provider rotation | Authentication configuration | Treat metadata/signing-key failures as availability failures; restore provider connectivity or configuration. Do not disable signature, issuer, audience, or lifetime validation. |

Monitor request rates and latency by route/status, `401` versus `403` counts, identity-provider metadata/key-refresh failures, and document-route access. Alert on sudden authentication failure changes. Redact `Authorization` headers, tokens, claim values, query secrets, and generated document examples from logs. Use a production readiness probe that does not require weakening a protected business endpoint.

## Verification checklist

Authoring evidence:

- [x] The complete sample compiled in a temporary `net10.0` web project with the catalog's pinned packages.
- [ ] No token was issued and no external identity provider was contacted during authoring.

Consuming-application checks:

- [ ] Architecture/adoption approval is recorded for `FastEndpoints`, `FastEndpoints.OpenApi`, `Microsoft.AspNetCore.Authentication.JwtBearer`, and `Scalar.AspNetCore` before their references are added.
- [ ] A valid issuer token with the expected audience and `orders.read` scope receives `200`.
- [ ] Missing, expired, wrong-issuer, wrong-audience, and invalid-signature tokens receive `401`.
- [ ] A valid token without the required scope receives `403`.
- [ ] `/openapi/v1.json` and Scalar are unavailable outside approved environments or protected by an approved policy.
- [ ] The document advertises bearer security and the expected `401`/`403` behavior without containing secrets.
- [ ] Logs and traces contain correlation data but no bearer token or sensitive claim values.

## Related guides

- [FastEndpoints](../packages/fastendpoints.md)
- [FastEndpoints.OpenApi](../packages/fastendpoints-openapi.md)
- [Microsoft.AspNetCore.Authentication.JwtBearer](../packages/microsoft-aspnetcore-authentication-jwtbearer.md)
- [Scalar.AspNetCore](../packages/scalar-aspnetcore.md)
- [API authentication ownership](../package-guidance/package-selection.md#api-authentication-ownership)

## Primary sources

Accessed 2026-07-27.

- [FastEndpoints security](https://fast-endpoints.com/docs/security)
- [FastEndpoints OpenAPI documents](https://fast-endpoints.com/docs/openapi-documents)
- [ASP.NET Core JWT bearer authentication (.NET 10)](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)
- [ASP.NET Core authentication overview (.NET 10)](https://learn.microsoft.com/aspnet/core/security/authentication/?view=aspnetcore-10.0)
- [Scalar ASP.NET Core integration](https://scalar.com/products/api-references/integrations/aspnetcore/integration)
- [FastEndpoints 8.2.0 on NuGet](https://www.nuget.org/packages/FastEndpoints/8.2.0)
- [FastEndpoints.OpenApi 8.2.0 on NuGet](https://www.nuget.org/packages/FastEndpoints.OpenApi/8.2.0)
- [Microsoft.AspNetCore.Authentication.JwtBearer 10.0.10 on NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer/10.0.10)
- [Scalar.AspNetCore 2.16.16 on NuGet](https://www.nuget.org/packages/Scalar.AspNetCore/2.16.16)
