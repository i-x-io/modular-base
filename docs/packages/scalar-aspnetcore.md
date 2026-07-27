# Scalar.AspNetCore

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `Scalar.AspNetCore` | `2.16.16` | Interactive Scalar API reference UI for generated OpenAPI documents | Centrally pinned; catalog-only until an API project consumes it |

- Owner: IX
- Last reviewed: 2026-07-27
- Review trigger: Scalar.AspNetCore version, target framework, document routing, browser security, or OpenAPI producer changes.

## Decision and scope

Use Scalar as the interactive UI over the FastEndpoints-generated OpenAPI documents. It does not generate FastEndpoints metadata or enforce security; it only renders configured document endpoints/files.

## Recommended registration and use

Add the centrally versioned package to the consuming web project:

```xml
<ItemGroup>
  <PackageReference Include="Scalar.AspNetCore" />
</ItemGroup>
```

Map Scalar after mapping the OpenAPI document(s), passing the exact FastEndpoints document names. Gate both routes to development unless production exposure has an explicit protected authorization policy:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.AddDocuments("v1");
        options.OperationTitleSource = OperationTitleSource.Path;
    });
}
```

Add all intended named documents explicitly; `AddDocuments("v1", "v2")` is appropriate only when both documents are registered. Compile and browser-test this mapping in the consuming application as required by the verification checklist.

For an approved non-development deployment, protect both the UI and its source document with the same documentation policy:

```csharp
app.MapOpenApi("/openapi/{documentName}.json")
    .RequireAuthorization("ApiDocs");

app.MapScalarApiReference("/docs", options =>
    options.AddDocument("v1", routePattern: "/openapi/{documentName}.json"))
    .RequireAuthorization("ApiDocs");
```

`MapScalarApiReference()` returns an endpoint convention builder, so normal ASP.NET Core endpoint authorization applies. The document name and case must match the producer. Scalar can preconfigure an OpenAPI security scheme for development convenience, but any prefilled token, client secret, or API key is delivered to the browser; never configure real credentials this way.

| Setting / API | Purpose | Default behavior | Production guidance | Reload / sensitivity / failure behavior |
| --- | --- | --- | --- | --- |
| `MapScalarApiReference(route)` | Maps the browser UI | Default Scalar route is used when omitted | Use an intentional route and gate it to development or a documentation policy | Startup endpoint mapping; wrong route returns 404 |
| `AddDocument(s)` | Identifies source OpenAPI documents | Producer/default document discovery applies | List only registered, approved names and route patterns | Startup options; name/pattern mismatch makes document fetch fail |
| `OperationTitleSource` | Selects displayed operation labels | Scalar default applies | Choose a stable source aligned with the published contract | UI-only startup option; does not change API behavior |
| Authentication preconfiguration | Helps users enter credentials in the UI | No production credential is required by Scalar | Configure scheme metadata only; never embed a token, API key, or client secret | Options become browser-visible; treat any value as disclosed to UI users |
| `DisableAgent()` / `WithAgentKey(...)` | Controls Scalar's optional AI chat agent | A limited Agent experience is available on localhost without a key | Call `DisableAgent()` wherever the feature and its data flow are not explicitly approved; treat an Agent key as a separate service credential, never API authentication | Startup/browser UI configuration; source keys from approved secret management and verify the pinned browser-exposure model before production |

## Enterprise implementation guidance

- Treat the Scalar route as a public application surface: choose a route, hosting environment, access policy, and change-control owner.
- Keep document titles, server URLs, authorization descriptions, and examples accurate; Scalar faithfully exposes whatever the OpenAPI document declares.
- Use a single OpenAPI document source of truth. Do not configure Scalar against a stale copied file in development while clients use a different live contract.
- In Native AOT production, serve FastEndpoints-exported static documents and point Scalar at those names/paths; keep live document generation for approved development workflows.
- Make the explorer workflow reproducible: select the intended named document, authenticate through its declared OAuth/OIDC flow, exercise a safe read operation, and confirm the generated request uses the expected server URL and security scheme.

### Upgrade and rollback

Upgrade Scalar independently only after checking ASP.NET Core target-framework compatibility and testing the pinned integration APIs against every named OpenAPI document, protected route, content-security/browser policy, and authentication flow. Review release notes for option/route/UI asset changes and repeat a real browser smoke test.

Rollback the package and application artifact together if mapping/options APIs changed. The OpenAPI producer can continue serving the reviewed JSON without Scalar while rollback completes; disable or unmap the explorer rather than weakening its route policy or embedding credentials.

## Integration with the catalog

- [FastEndpoints.OpenApi](fastendpoints-openapi.md) creates named documents and maps `/openapi/{documentName}.json`.
- [Microsoft.AspNetCore.OpenApi](microsoft-aspnetcore-openapi.md) is underlying document infrastructure; [Microsoft.OpenApi](microsoft-openapi.md) stays on 2.11.0 for compatibility.
- [JWT bearer](microsoft-aspnetcore-authentication-jwtbearer.md) protects APIs. Scalar’s authorization input is a client convenience, not an identity solution.
- Use the [API authentication ownership decision](../package-guidance/package-selection.md#api-authentication-ownership) before wiring the explorer to bearer-protected operations.
- Follow the [FastEndpoints, JWT, OpenAPI, and Scalar recipe](../recipes/fastendpoints-jwt-openapi-scalar.md) for producer, validator, and UI ownership.
- Review [Scalar.AspNetCore supply-chain metadata](../package-guidance/supply-chain.md#scalar-aspnetcore) before approval or upgrade.

## Security, performance, AOT, trimming, and operations

- Do not deploy an API explorer by default if it materially expands attack surface or reveals internal APIs. Prefer development/internal exposure or an explicitly protected production route.
- Avoid publishing real credentials in examples or preconfigured authorization values.
- Make an explicit approval decision for Scalar Agent. Disable it when AI-assisted API chat is outside the environment's privacy/data-handling policy, and never reuse an API credential as an Agent key.
- Serve exported static JSON in production AOT deployments to avoid runtime document generation.
- Monitor route access and document availability, but do not log authorization headers submitted through the explorer.

### Operational signals and troubleshooting

Monitor UI-route status/latency, source-document fetch status, browser-side asset errors, and access-policy outcomes. Keep document names and route templates bounded; never log entered bearer tokens, API keys, OAuth codes, client secrets, or full authorization headers.

| Symptom | Likely cause and diagnostic | Safe corrective action | Retry? |
| --- | --- | --- | --- |
| UI loads but reports that the document cannot be fetched | Document name, route pattern, base path, proxy prefix, authorization, or CORS policy differs; inspect the browser network request and server route | Align the exact producer URL/name and protect UI/document consistently | After correcting routing/authentication |
| UI route returns 404 | Environment gate or mapped Scalar route differs from the requested path | Use the approved environment and configured route; do not expose it globally as a shortcut | No |
| “Authorize” succeeds in UI but API returns 401/403 | OpenAPI scheme, token audience/issuer/scopes, or runtime authorization policy differs | Correct the document and obtain a valid access token with required entitlement | Only with corrected token/policy |
| Explorer reveals internal endpoints/examples | Source OpenAPI document contains them; Scalar is rendering the producer output | Fix/filter the producer, regenerate and review the contract, and assess disclosure | No |

## Avoid

- Do not assume `MapScalarApiReference()` generates an OpenAPI document.
- Do not specify a document name that FastEndpoints did not register.
- Do not treat “Authorize” support in the UI as a reason to weaken OAuth/OIDC flows or API security.

## Verification checklist

- [ ] Each configured Scalar document exists at the expected OpenAPI endpoint/static path.
- [ ] The UI is exposed only in approved environments or protected by the approved policy.
- [ ] Document titles/operation names and auth schemes match the published API contract.
- [ ] Production AOT deployments serve exported document files successfully.
- [ ] No production credential or confidential OAuth client secret is embedded in Scalar options or generated HTML.

## Sources

- [FastEndpoints OpenAPI documents: Scalar integration](https://fast-endpoints.com/docs/openapi-documents) — Accessed 2026-07-27.
- [Scalar ASP.NET Core package repository](https://github.com/scalar/scalar/tree/main/integrations/dotnet/aspnetcore) — Accessed 2026-07-27.
- [Scalar: ASP.NET Core API Reference integration](https://scalar.com/products/api-references/integrations/aspnetcore/integration) — Accessed 2026-07-27.
- [Scalar: multiple OpenAPI documents](https://github.com/scalar/scalar/blob/main/integrations/dotnet/aspnetcore/docs/multiple-openapi-documents.md) — Accessed 2026-07-27.
- [NuGet: Scalar.AspNetCore 2.16.16](https://www.nuget.org/packages/Scalar.AspNetCore/2.16.16) — Accessed 2026-07-27.
