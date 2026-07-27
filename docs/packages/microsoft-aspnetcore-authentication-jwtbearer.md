# Microsoft.AspNetCore.Authentication.JwtBearer

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.10` | ASP.NET Core bearer-token authentication handler | Centrally pinned; catalog-only until an API project consumes it |

- Owner: IX
- Last reviewed: 2026-07-27
- Review trigger: Package/target-framework servicing update, token-validation defaults, identity-provider metadata behavior, or trust-boundary changes.

## Decision and scope

Use this as the production bearer-token validation handler. It authenticates a request and constructs the principal; ASP.NET Core authorization and FastEndpoints endpoint rules then decide access.

## Recommended registration and use

Add the centrally versioned package to the consuming web project:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
</ItemGroup>
```

Configure a default bearer scheme from trusted issuer metadata, define authorization separately, and place `UseAuthentication()` before middleware that needs `HttpContext.User`:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("orders.read", policy =>
        policy.RequireClaim("permission", "orders.read"));

app.UseAuthentication();
app.UseAuthorization();
```

This syntax follows Microsoft’s current ASP.NET Core 10 documentation. `Authority` enables discovery of the issuer metadata and signing keys; `Audience` identifies this API. The consuming application must use values issued for its identity provider and deployment environment. If a provider requires a nonstandard metadata address or multiple issuers/audiences, configure explicit `TokenValidationParameters` and test every accepted trust boundary.

A normal client workflow is: acquire an OAuth/OIDC **access token**, send it in `Authorization: Bearer ...`, receive `401` when authentication fails, and receive `403` when the authenticated principal lacks the required policy. The API must not redirect clients to obtain a token.

| Setting | Purpose | Default behavior | Production guidance | Reload / sensitivity / failure behavior |
| --- | --- | --- | --- | --- |
| `Authority` / `MetadataAddress` | Locates trusted issuer metadata and signing keys | No external authority is inferred | Use HTTPS issuer metadata from the approved identity provider | Options are normally startup-bound; metadata refresh is handled by the configuration manager; URLs are not secrets but reveal trust topology |
| `Audience` | Restricts tokens to this API | No application-specific audience is inferred | Require the exact API resource/audience contract | Startup-bound; mismatch produces 401 |
| `MapInboundClaims` | Controls legacy claim-type mapping | Handler/framework default applies | Set deliberately; `false` preserves issuer claim names in the shown contract | Startup-bound; changing it can turn successful authentication into 403 policy failures |
| `TokenValidationParameters.ClockSkew` | Allows bounded issuer/API clock difference | IdentityModel default applies | Keep small and monitor time synchronization; do not use it to mask expired tokens | Startup-bound; wider skew extends token acceptance |
| `RequireHttpsMetadata` | Requires HTTPS for metadata retrieval | `true` | Keep `true` outside isolated local tests | Startup-bound; disabling it weakens metadata transport security |

### Upgrade and rollback

Apply ASP.NET Core servicing updates with the target framework/runtime and review security advisories plus authentication/IdentityModel dependency changes. Before rollout, test trusted and rejected token cases, discovery/JWKS refresh, key rollover, claim mapping, and challenge responses against a non-production issuer.

Rollback the application and compatible ASP.NET Core runtime/package set together. Do not weaken validation to restore service; if a new issuer key/claim contract was introduced, keep the previous and new trust material overlapped only for the approved rotation window while the application rollback completes.

## Enterprise implementation guidance

- Prefer OIDC/OAuth access tokens issued by a trusted identity provider. Do not mint production access tokens from username/password requests.
- Validate the token signature, issuer, audience, and expiration. Use the provider’s metadata/JWKS and asymmetric keys where possible.
- Define authorization policies around stable claims/scopes rather than scattered ad-hoc claim checks.
- Treat authentication configuration, authority availability, clock skew, key rollover, and metadata refresh as production operational concerns.
- Prefer a fallback policy requiring authenticated users when the API is private-by-default; mark the small set of genuinely public endpoints explicitly.

## Integration with the catalog

- [FastEndpoints](fastendpoints.md) requires authentication/authorization middleware before `UseFastEndpoints()`.
- [FastEndpoints.Security](fastendpoints-security.md) offers FastEndpoints conveniences but does not replace this handler for external identity-provider validation.
- [FastEndpoints.OpenApi](fastendpoints-openapi.md) describes bearer security for clients; it does not authenticate requests.
- [FastEndpoints.Testing](fastendpoints-testing.md) and [MVC Testing](microsoft-aspnetcore-mvc-testing.md) must use isolated test authentication.
- Use the [API authentication ownership decision](../package-guidance/package-selection.md#api-authentication-ownership) to choose direct JWT bearer registration or FastEndpoints conveniences without double registration.
- Follow the [FastEndpoints, JWT, OpenAPI, and Scalar recipe](../recipes/fastendpoints-jwt-openapi-scalar.md) for complete middleware and document composition.
- Review [JWT bearer supply-chain metadata](../package-guidance/supply-chain.md#microsoft-aspnetcore-authentication-jwtbearer) before approval or servicing updates.

## Security, performance, AOT, trimming, and operations

- Never log access tokens, signing material, or complete sensitive claims.
- Use HTTPS and restrict CORS. Token validation does not make an unsafe browser token-storage pattern safe.
- Monitor authentication failures and key/metadata refresh health without recording credential material.
- Cache identity-provider metadata according to the handler/provider model; do not hand-roll per-request key discovery.
- Keep detailed `JwtBearerEvents` diagnostics server-side and sanitized. Return standards-appropriate challenges, not validation internals that reveal issuer or key-selection details.

### Operational signals and troubleshooting

Monitor 401 and 403 rates separately, metadata/JWKS retrieval failures, token-validation exception categories, and identity-provider/key-rotation health using bounded dimensions. Never record the authorization header, raw token, signing keys, or full sensitive claims.

| Symptom | Likely cause and diagnostic | Safe corrective action | Retry? |
| --- | --- | --- | --- |
| All calls return 401 | Authority/audience/scheme/middleware order is wrong or metadata is unavailable; inspect sanitized handler category and issuer/audience values | Restore trusted configuration and middleware order; verify provider availability | Retry only after configuration/provider recovery |
| Token works at issuer but API reports invalid signature | Stale/missing JWKS, wrong issuer, or uncoordinated key rotation; compare token `kid` with trusted metadata without logging the token | Repair metadata reachability or complete coordinated key rotation | Bounded retry after metadata refresh |
| Authenticated caller gets 403 | Authentication succeeded but policy claim names/values or `MapInboundClaims` differ | Align authorization policy with the verified issuer claim contract | Only after entitlement/policy correction |
| Recently expired/not-yet-valid results vary by node | Clock synchronization or excessive/unequal skew differs across hosts | Repair host time synchronization and use one reviewed skew policy | Do not retry until clocks/token are valid |

## Avoid

- Do not accept a token only because it is structurally a JWT.
- Do not turn off issuer, audience, signature, or expiry validation to resolve integration issues.
- Do not send ID tokens to APIs or treat an ID token as an access token.

## Verification checklist

- [ ] Valid, expired, invalid-signature, wrong-issuer, and wrong-audience tokens have expected results.
- [ ] Authentication executes before authorization and FastEndpoints middleware.
- [ ] Token configuration is sourced from an approved secret/configuration system.
- [ ] Production logs and traces redact authorization headers.
- [ ] Key rollover and temporary identity-provider metadata failures have an operational runbook.

## Sources

- [Microsoft: configure JWT bearer authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [Microsoft: authentication overview](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [Microsoft: authentication and authorization in Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [NuGet: Microsoft.AspNetCore.Authentication.JwtBearer 10.0.10](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer/10.0.10) — Accessed 2026-07-27.
