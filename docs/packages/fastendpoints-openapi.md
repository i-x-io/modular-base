# FastEndpoints.OpenApi

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `FastEndpoints.OpenApi` | `8.2.0` | FastEndpoints-aware OpenAPI document generation and export | Centrally pinned; catalog-only until an API project consumes it |

## Decision and scope

Use this package as the sole OpenAPI registration path for FastEndpoints. It uses the Microsoft ASP.NET Core OpenAPI stack while adding FastEndpoints transformers and metadata handling. Pair it with Scalar for an explorer; this package generates documents but does not provide a UI.

## Recommended registration and use

Add the package without a version; `Directory.Packages.props` supplies `8.2.0`:

```xml
<ItemGroup>
  <PackageReference Include="FastEndpoints.OpenApi" />
</ItemGroup>
```

The complete host pipeline lives in [FastEndpoints](fastendpoints.md). The focused delta is to register documents with `.OpenApiDocument()`, then expose them with `.MapOpenApi()` after `UseFastEndpoints()`. A document name must match wherever it is referenced, including Scalar and export calls. The recommended development route is gated below; production exposure requires an explicit protected authorization policy.

```csharp
builder.Services
    .AddFastEndpoints()
    .OpenApiDocument(o =>
    {
        o.DocumentName = "v1";
        o.Title = "Modular Base API";
        o.Version = "v1";
        o.ExcludeNonFastEndpoints = true;
    });

app.UseFastEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.AddDocuments("v1"));
}
```

This snippet is part of the compile-verified pipeline in [FastEndpoints](fastendpoints.md).

For a build/deployment workflow that produces a static contract, keep the document name identical and place export immediately after FastEndpoints middleware:

```csharp
app.UseFastEndpoints();
await app.ExportOpenApiDocsAndExitAsync("v1");

if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    app.UseStaticFiles();
```

Manual export uses the documented configuration flag and writes to `wwwroot/openapi` unless `OpenApiExportPath` changes it:

```bash
dotnet run -p:PublishAot=false --export-openapi-docs true
```

## Enterprise implementation guidance

- Define one document per intentional API contract/release group. Use unique document names.
- Use `EndpointFilter`, `ExcludeNonFastEndpoints`, and version/release options to prevent internal or unrelated routes from entering a published contract.
- Diff exported JSON in CI when a contract change needs review, and generate clients only from an approved artifact rather than a developer-local live endpoint.
- Keep summaries, examples, response descriptions, and authorization schemes accurate; generated documents are client-facing contracts.
- Let FastEndpoints infer `Accepts`/`Produces` metadata where sufficient, and describe exceptions explicitly with endpoint configuration.

## Integration with the catalog

- [FastEndpoints](fastendpoints.md) must be registered first.
- [Scalar.AspNetCore](scalar-aspnetcore.md) consumes the named documents with `AddDocuments(...)`.
- [FastEndpoints.Generator](fastendpoints-generator.md) and static export support Native AOT deployments.
- [Asp.Versioning.Http](asp-versioning-http.md) requires an intentional document strategy; FastEndpoints `MinEndpointVersion`, `MaxEndpointVersion`, and `ReleaseVersion` are document filters, not a substitute for HTTP API-version readers.
- [Microsoft.AspNetCore.OpenApi](microsoft-aspnetcore-openapi.md) is transitive infrastructure; do not separately call its `AddOpenApi()`.

## Security, performance, AOT, trimming, and operations

- JWT bearer security is added automatically unless `EnableJWTBearerAuth = false`; configure additional schemes with `AddAuth()`.
- Publish live documents only where the disclosure risk is accepted. For AOT production, export named JSON at build/deployment time and serve the static files.
- `ExportOpenApiDocsAndExitAsync("v1")` performs work only when `export-openapi-docs=true`; place it immediately after `UseFastEndpoints()` so external side effects do not run during export.
- OpenAPI export document names must exactly match `.OpenApiDocument()` registrations.
- Treat export mode as a short-lived application startup: guard migrations, queue consumers, schedulers, and other side effects with the documented JSON-export-mode checks.

## Avoid

- Do not call `builder.Services.AddOpenApi()` directly for a FastEndpoints document; it bypasses FastEndpoints transformers and metadata handling.
- Do not use a Scalar document name that was not registered with `.OpenApiDocument()`.
- Do not treat the OpenAPI file as authorization enforcement. It describes security; middleware enforces it.

## Verification checklist

- [ ] `/openapi/v1.json` returns the expected document in the approved environment.
- [ ] Every Scalar document name maps to a registered FastEndpoints document.
- [ ] The document contains only approved endpoints and correct 401/403 responses.
- [ ] A reviewed export diff detects unintended route, schema, or security-scheme changes.
- [ ] AOT deployments export documents and serve static JSON successfully.

## Sources

- [FastEndpoints OpenAPI documents](https://fast-endpoints.com/docs/openapi-documents) — Accessed 2026-07-27.
- [FastEndpoints native AOT](https://fast-endpoints.com/docs/native-aot) — Accessed 2026-07-27.
- [FastEndpoints API versioning](https://fast-endpoints.com/docs/api-versioning) — Accessed 2026-07-27.
- [FastEndpoints.OpenApi upstream source](https://github.com/FastEndpoints/FastEndpoints/tree/main/Src/OpenApi) — Accessed 2026-07-27.
- [NuGet: FastEndpoints.OpenApi 8.2.0](https://www.nuget.org/packages/FastEndpoints.OpenApi/8.2.0) — Accessed 2026-07-27.
