# FastEndpoints.Security

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `FastEndpoints.Security` | `8.2.0` | FastEndpoints JWT issuance/configuration conveniences and generated access-control permissions | Catalog-only; centrally pinned until an API project consumes it |

- Owner: IX
- Last reviewed: 2026-07-27
- Review trigger: FastEndpoints.Security/JWT bearer version, target framework, identity-provider claim contract, or authorization behavior changes.

## Decision and scope

Use this package only when FastEndpoints-specific JWT or permission-generation conveniences are needed. ASP.NET Core authentication and authorization middleware remains authoritative for production token validation and policy execution.

## Recommended registration and use

Add the package without a version so central package management supplies `8.2.0`:

```xml
<ItemGroup>
  <PackageReference Include="FastEndpoints.Security" />
</ItemGroup>
```

FastEndpoints documents this convenience registration. The signing key must come from a secret provider and startup should fail if it is absent; the literal below is deliberately not a key value:

```csharp
using FastEndpoints;
using FastEndpoints.Security;

builder.Services
    .AddAuthenticationJwtBearer(o =>
        o.SigningKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("JWT signing key is missing."))
    .AddAuthorization()
    .AddFastEndpoints();
```

The complete middleware order is shown in [FastEndpoints](fastendpoints.md). A focused access-control endpoint can declare and apply a generated permission in one call when `FastEndpoints.Generator` is installed in the endpoint project:

```csharp
using FastEndpoints;

public sealed class CreateOrderEndpoint : Endpoint<CreateOrderRequest>
{
    public override void Configure()
    {
        Post("/orders");
        AccessControl(
            keyName: "Orders_Create",
            behavior: Apply.ToThisEndpoint,
            groupNames: "OrderManagers");
    }

    public override Task HandleAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
        => Send.OkAsync(cancellationToken);
}
```

`AccessControl("Orders_Create")` without `Apply.ToThisEndpoint` only generates the permission member; it does not authorize the endpoint. Apply the requirement explicitly with `Permissions(Allow.Orders_Create)` or use the `Apply.ToThisEndpoint` overload shown above.

For production integrations with an external identity provider, prefer explicit [JWT bearer](microsoft-aspnetcore-authentication-jwtbearer.md) issuer, audience, signing-key, and lifetime validation. Require access through endpoint `Policies()`, `Roles()`, `Claims()`, `Permissions()`, or `Scopes()`; call `AllowAnonymous()` only on intentionally public routes.

## Enterprise implementation guidance

- Prefer OIDC/OAuth-issued access tokens; do not create production tokens from username/password requests.
- Use named ASP.NET Core authorization policies for cross-cutting access decisions and endpoint-local requirements for narrowly scoped rules.
- Use `AccessControl("Capability")` with stable domain names where generated permission codes are desirable. Put shared partial `Allow` declarations in an authorization-owned namespace.
- Document whether each rule is any-of or all-of. `Permissions()` and `Scopes()` accept any listed value; use `PermissionsAll()` or `ScopesAll()` when every listed value is mandatory.
- Configure `PermissionsClaimType`, `ScopeClaimType`, `ScopeParser`, and `RoleClaimType` only to match the identity-provider contract; test actual claims before changing defaults.

A typical rollout is to define stable capability names, generate and review their codes, map identity-provider claims to those codes, require them on endpoints, then exercise anonymous, expired, malformed, insufficient, and sufficient-token cases in the integration suite.

| Setting / method | Purpose | Default behavior | Production guidance | Reload / sensitivity / failure behavior |
| --- | --- | --- | --- | --- |
| `SigningKey` | Signs/validates locally issued symmetric tokens in the convenience path | Must be supplied for that path | Obtain from an approved secret provider; prefer external OIDC/OAuth issuance for federation | Treat as secret; key changes require a coordinated rotation and invalidate unmatched tokens |
| `PermissionsClaimType` / `ScopeClaimType` | Selects claims read by endpoint authorization helpers | FastEndpoints conventions apply | Change only to match the issuer contract | Startup-only; mismatch yields authenticated callers with 403 responses |
| `ScopeParser` | Parses multiple scopes from the configured claim | Built-in parsing convention | Override only for an issuer-specific, tested format | Startup-only; a parser change changes authorization semantics |
| `PermissionsAll()` / `ScopesAll()` | Requires every listed capability | `Permissions()` / `Scopes()` use any-of behavior | Choose explicitly during authorization design | Endpoint metadata; wrong choice grants too much or denies valid callers |

### Upgrade and rollback

Upgrade this package with the FastEndpoints family and revalidate generated permission values, claim types/parsers, any-of/all-of semantics, token issuance helpers, and the ASP.NET Core authentication scheme. Coordinate any identity-provider mapping change before deploying the application.

Rollback the package family and application authorization metadata together. Preserve prior permission-to-role mappings until the restored application is serving traffic; rotating signing material or claim contracts during rollback requires an explicit overlap window rather than silently accepting invalid tokens.

## Integration with the catalog

- [FastEndpoints](fastendpoints.md) applies endpoint security metadata and defaults endpoints to protected.
- [FastEndpoints.Generator](fastendpoints-generator.md) generates `AccessControl()` permission members.
- [Microsoft.AspNetCore.Authentication.JwtBearer](microsoft-aspnetcore-authentication-jwtbearer.md) validates external access tokens.
- Central transitive pinning is disabled: FastEndpoints.Security 8.2.0 declares JWT bearer 10.0.9 for `net10.0`. Keep the direct versionless JWT bearer reference when the application requires the catalog's 10.0.10 servicing pin or directly configures its APIs; choose exactly one registration owner for each bearer scheme.
- [FastEndpoints.OpenApi](fastendpoints-openapi.md) describes JWT security in the API contract; it does not replace token validation.
- Use the [API authentication ownership decision](../package-guidance/package-selection.md#api-authentication-ownership) before selecting this convenience layer instead of direct JWT bearer registration.
- Follow the [FastEndpoints, JWT, OpenAPI, and Scalar recipe](../recipes/fastendpoints-jwt-openapi-scalar.md) for the complete secure pipeline.
- Review [FastEndpoints.Security supply-chain metadata](../package-guidance/supply-chain.md#fastendpoints-security) before approval or upgrade.

## Security, performance, AOT, trimming, and operations

- Protect signing keys and never commit them. Rotate keys through an approved secret store.
- Validate signature, issuer, audience, and expiration. Return 401 for invalid tokens and 403 for authenticated callers lacking authorization.
- Keep scope and permission claim parsing deterministic; a custom parser changes authorization semantics.
- Keep authorization decisions server-side. Token contents are caller-controlled until signature and validation succeed, and OpenAPI declarations do not enforce access.
- Never log tokens, claims with sensitive data, or signing keys. Record only non-sensitive authorization outcomes and correlation IDs.

### Operational signals and troubleshooting

Monitor bounded counts of authentication failures, authorization denials, and endpoint policy names; distinguish 401 from 403. Never record bearer tokens, signing keys, complete claim sets, or permission values that reveal sensitive tenancy/business data.

| Symptom | Likely cause and diagnostic | Safe corrective action | Retry? |
| --- | --- | --- | --- |
| Every protected call returns 401 | Authentication scheme/signing configuration or middleware order is wrong; inspect sanitized handler diagnostics | Restore the selected scheme, trusted keys/issuer, and middleware order | Only after obtaining/configuring a valid token |
| Validly authenticated caller gets 403 | Claim type, parser, permission mapping, or any/all semantics differs from the issuer contract | Align the reviewed claim mapping or caller entitlement | Only after entitlement/configuration changes |
| `AccessControl()` appears to have no effect | The call generated a member but did not apply it to the endpoint | Use `Apply.ToThisEndpoint` or explicitly require the generated permission | No |
| Tokens fail during key rotation | Issuer and validator do not share an overlap window or metadata/key refresh has not completed | Use a coordinated rotation with both valid keys available for the planned overlap | Retry after trusted metadata refresh, with bounded backoff |

## Avoid

- Do not use an application-local symmetric signing key as the default production federation strategy.
- Do not disable endpoint security to make a test or API explorer work outside an isolated test/development environment.
- Do not assume `Permissions()` requires all permissions; use `PermissionsAll()` when all are required. Likewise use `ScopesAll()` when all scopes are required.

## Verification checklist

- [ ] Authentication middleware precedes authorization and FastEndpoints middleware.
- [ ] Valid, expired, wrong-issuer, wrong-audience, and insufficient-permission tokens have covered outcomes.
- [ ] Every anonymous endpoint is explicitly marked and reviewed.
- [ ] Permission/scope claim types match the identity provider’s emitted claims.
- [ ] Generated permission values are stable and identity-provider role/group mappings are reviewed after renames.

## Sources

- [FastEndpoints security](https://fast-endpoints.com/docs/security) — Accessed 2026-07-27.
- [Microsoft: configure JWT bearer authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [FastEndpoints.Security upstream source](https://github.com/FastEndpoints/FastEndpoints/tree/main/Src/Security) — Accessed 2026-07-27.
- [NuGet: FastEndpoints.Security 8.2.0](https://www.nuget.org/packages/FastEndpoints.Security/8.2.0) — Accessed 2026-07-27.
