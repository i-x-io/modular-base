# FastEndpoints.Security

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `FastEndpoints.Security` | `8.2.0` | FastEndpoints JWT issuance/configuration conveniences and generated access-control permissions | Centrally pinned; catalog-only until an API project consumes it |

## Decision and scope

Use this package only when FastEndpoints-specific JWT or permission-generation conveniences are needed. ASP.NET Core authentication and authorization middleware remains authoritative for production token validation and policy execution.

## Recommended registration and use

FastEndpoints documents this convenience registration:

```csharp
builder.Services
    .AddAuthenticationJwtBearer(o => o.SigningKey = configuration["Jwt:SigningKey"]!)
    .AddAuthorization()
    .AddFastEndpoints();
```

For production integrations with an external identity provider, prefer explicit [JWT bearer](microsoft-aspnetcore-authentication-jwtbearer.md) issuer, audience, signing-key, and lifetime validation. Require access through endpoint `Policies()`, `Roles()`, `Claims()`, `Permissions()`, or `Scopes()`; call `AllowAnonymous()` only on intentionally public routes.

## Enterprise implementation guidance

- Prefer OIDC/OAuth-issued access tokens; do not create production tokens from username/password requests.
- Use named ASP.NET Core authorization policies for cross-cutting access decisions and endpoint-local requirements for narrowly scoped rules.
- Use `AccessControl("Capability")` with stable domain names where generated permission codes are desirable. Put shared partial `Allow` declarations in an authorization-owned namespace.
- Configure `PermissionsClaimType`, `ScopeClaimType`, `ScopeParser`, and `RoleClaimType` only to match the identity-provider contract; test actual claims before changing defaults.

## Integration with the catalog

- [FastEndpoints](fastendpoints.md) applies endpoint security metadata and defaults endpoints to protected.
- [FastEndpoints.Generator](fastendpoints-generator.md) generates `AccessControl()` permission members.
- [Microsoft.AspNetCore.Authentication.JwtBearer](microsoft-aspnetcore-authentication-jwtbearer.md) validates external access tokens.
- [FastEndpoints.OpenApi](fastendpoints-openapi.md) describes JWT security in the API contract; it does not replace token validation.

## Security, performance, AOT, trimming, and operations

- Protect signing keys and never commit them. Rotate keys through an approved secret store.
- Validate signature, issuer, audience, and expiration. Return 401 for invalid tokens and 403 for authenticated callers lacking authorization.
- Keep scope and permission claim parsing deterministic; a custom parser changes authorization semantics.
- Never log tokens, claims with sensitive data, or signing keys. Record only non-sensitive authorization outcomes and correlation IDs.

## Avoid

- Do not use an application-local symmetric signing key as the default production federation strategy.
- Do not disable endpoint security to make a test or API explorer work outside an isolated test/development environment.
- Do not assume `Permissions()` requires all permissions; use `PermissionsAll()` when all are required. Likewise use `ScopesAll()` when all scopes are required.

## Verification checklist

- [ ] Authentication middleware precedes authorization and FastEndpoints middleware.
- [ ] Valid, expired, wrong-issuer, wrong-audience, and insufficient-permission tokens have covered outcomes.
- [ ] Every anonymous endpoint is explicitly marked and reviewed.
- [ ] Permission/scope claim types match the identity provider’s emitted claims.

## Sources

- [FastEndpoints security](https://fast-endpoints.com/docs/security) — Accessed 2026-07-27.
- [Microsoft: configure JWT bearer authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0) — Accessed 2026-07-27.
