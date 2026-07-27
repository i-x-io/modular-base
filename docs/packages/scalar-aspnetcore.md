# Scalar.AspNetCore

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `Scalar.AspNetCore` | `2.16.16` | Interactive Scalar API reference UI for generated OpenAPI documents | Centrally pinned; catalog-only until an API project consumes it |

## Decision and scope

Use Scalar as the interactive UI over the FastEndpoints-generated OpenAPI documents. It does not generate FastEndpoints metadata or enforce security; it only renders configured document endpoints/files.

## Recommended registration and use

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

This syntax was compilation-verified in the package pipeline described in [FastEndpoints](fastendpoints.md). Add all intended named documents explicitly; `AddDocuments("v1", "v2")` is appropriate only when both documents are registered.

## Enterprise implementation guidance

- Treat the Scalar route as a public application surface: choose a route, hosting environment, access policy, and change-control owner.
- Keep document titles, server URLs, authorization descriptions, and examples accurate; Scalar faithfully exposes whatever the OpenAPI document declares.
- Use a single OpenAPI document source of truth. Do not configure Scalar against a stale copied file in development while clients use a different live contract.
- In Native AOT production, serve FastEndpoints-exported static documents and point Scalar at those names/paths; keep live document generation for approved development workflows.

## Integration with the catalog

- [FastEndpoints.OpenApi](fastendpoints-openapi.md) creates named documents and maps `/openapi/{documentName}.json`.
- [Microsoft.AspNetCore.OpenApi](microsoft-aspnetcore-openapi.md) is underlying document infrastructure; [Microsoft.OpenApi](microsoft-openapi.md) stays on 2.11.0 for compatibility.
- [JWT bearer](microsoft-aspnetcore-authentication-jwtbearer.md) protects APIs. Scalar’s authorization input is a client convenience, not an identity solution.

## Security, performance, AOT, trimming, and operations

- Do not deploy an API explorer by default if it materially expands attack surface or reveals internal APIs. Prefer development/internal exposure or an explicitly protected production route.
- Avoid publishing real credentials in examples or preconfigured authorization values.
- Serve exported static JSON in production AOT deployments to avoid runtime document generation.
- Monitor route access and document availability, but do not log authorization headers submitted through the explorer.

## Avoid

- Do not assume `MapScalarApiReference()` generates an OpenAPI document.
- Do not specify a document name that FastEndpoints did not register.
- Do not treat “Authorize” support in the UI as a reason to weaken OAuth/OIDC flows or API security.

## Verification checklist

- [ ] Each configured Scalar document exists at the expected OpenAPI endpoint/static path.
- [ ] The UI is exposed only in approved environments or protected by the approved policy.
- [ ] Document titles/operation names and auth schemes match the published API contract.
- [ ] Production AOT deployments serve exported document files successfully.

## Sources

- [FastEndpoints OpenAPI documents: Scalar integration](https://fast-endpoints.com/docs/openapi-documents) — Accessed 2026-07-27.
- [Scalar ASP.NET Core package repository](https://github.com/scalar/scalar/tree/main/integrations/aspnetcore) — Accessed 2026-07-27.
- [NuGet: Scalar.AspNetCore 2.16.16](https://www.nuget.org/packages/Scalar.AspNetCore/2.16.16) — Accessed 2026-07-27.
