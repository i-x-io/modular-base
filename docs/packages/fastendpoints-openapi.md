# FastEndpoints.OpenApi

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `FastEndpoints.OpenApi` | `8.2.0` | FastEndpoints-aware OpenAPI document generation and export | Catalog-only; centrally pinned until an API project consumes it |

- Owner: IX
- Last reviewed: 2026-07-27
- Review trigger: FastEndpoints.OpenApi, ASP.NET Core OpenAPI, Microsoft.OpenApi, target framework, or document-export behavior changes.

## Decision and scope

Use this package as the sole OpenAPI registration path for FastEndpoints. It uses the Microsoft ASP.NET Core OpenAPI stack while adding FastEndpoints transformers and metadata handling. Pair it with Scalar for an explorer; this package generates documents but does not provide a UI.

## Recommended registration and use

Use the same direct versionless reference set as the complete composition in [FastEndpoints](fastendpoints.md): the endpoint framework, OpenAPI integration, JWT bearer authentication, and Scalar UI. `Directory.Packages.props` supplies their versions:

```xml
<ItemGroup>
  <PackageReference Include="FastEndpoints" />
  <PackageReference Include="FastEndpoints.OpenApi" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
  <PackageReference Include="Scalar.AspNetCore" />
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

This snippet uses the same document name throughout; compile and integration-test it in the consuming application as required by the verification checklist.

For a build/deployment workflow that produces a static contract, keep the document name identical and place export immediately after FastEndpoints middleware:

```csharp
app.UseFastEndpoints();
await app.ExportOpenApiDocsAndExitAsync("v1");

if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    app.UseStaticFiles();
```

The `UseStaticFiles()` branch makes the exported file available without an endpoint authorization policy. Use it only when the reviewed document is intentionally public or an authenticated gateway protects it; otherwise serve the artifact through an authorized endpoint or internal artifact system.

Manual export uses the documented configuration flag and writes to `wwwroot/openapi` unless `OpenApiExportPath` changes it:

```bash
dotnet run -p:PublishAot=false --export-openapi-docs true
```

| Setting | Purpose | Default behavior | Production guidance | Reload / sensitivity / failure behavior |
| --- | --- | --- | --- | --- |
| `DocumentName` | Stable identifier used in document routes, Scalar, and export | A library-generated/default name applies if omitted | Set an explicit unique name per published contract | Startup-only; a mismatch produces missing UI/export documents |
| `ExcludeNonFastEndpoints` | Controls whether non-FastEndpoints operations enter the document | Non-FastEndpoints may be included | Enable for framework-only contracts or apply an explicit filter | Startup-only; incorrect filtering can disclose internal routes |
| `EnableJWTBearerAuth` | Adds bearer security description to the document | Enabled | Disable only when another reviewed scheme owns the document | Startup-only; affects documentation, not enforcement |
| `OpenApiExportPath` | Selects static export output | `wwwroot/openapi` in the documented export workflow | Use a controlled artifact path and publish only reviewed output | Process/startup configuration; an invalid/unwritable path fails export |

## Enterprise implementation guidance

- Define one document per intentional API contract/release group. Use unique document names.
- Use `EndpointFilter`, `ExcludeNonFastEndpoints`, and version/release options to prevent internal or unrelated routes from entering a published contract.
- Diff exported JSON in CI when a contract change needs review, and generate clients only from an approved artifact rather than a developer-local live endpoint.
- Keep summaries, examples, response descriptions, and authorization schemes accurate; generated documents are client-facing contracts.
- Let FastEndpoints infer `Accepts`/`Produces` metadata where sufficient, and describe exceptions explicitly with endpoint configuration.

### Upgrade and rollback

Upgrade with `FastEndpoints` and verify the pinned `Microsoft.AspNetCore.OpenApi` and `Microsoft.OpenApi` compatibility boundary. Regenerate every named document, diff paths/schemas/security requirements, exercise export mode, and confirm Scalar still resolves each name before deployment.

Rollback the FastEndpoints OpenAPI family and serve the last approved static document artifact while compatibility is restored. Never leave a newly generated contract published when the runtime has rolled back to behavior that no longer implements it.

## Integration with the catalog

- [FastEndpoints](fastendpoints.md) must be registered first.
- [Scalar.AspNetCore](scalar-aspnetcore.md) consumes the named documents with `AddDocuments(...)`.
- [FastEndpoints.Generator](fastendpoints-generator.md) and static export support Native AOT deployments.
- [Asp.Versioning.Http](asp-versioning-http.md) requires an intentional document strategy; FastEndpoints `MinEndpointVersion`, `MaxEndpointVersion`, and `ReleaseVersion` are document filters, not a substitute for HTTP API-version readers.
- [Microsoft.AspNetCore.OpenApi](microsoft-aspnetcore-openapi.md) is transitive infrastructure; do not separately call its `AddOpenApi()`.
- Central transitive pinning is disabled: FastEndpoints.OpenApi 8.2.0 declares Microsoft.AspNetCore.OpenApi 10.0.9. Add a direct versionless `Microsoft.AspNetCore.OpenApi` reference when the application intentionally adopts the catalog's 10.0.10 servicing pin or uses its types, but still let `.OpenApiDocument()` own FastEndpoints registration.
- The [package-selection guide](../package-guidance/package-selection.md#api-authentication-ownership) separates described bearer security from the component that validates tokens.
- See the [FastEndpoints, JWT, OpenAPI, and Scalar recipe](../recipes/fastendpoints-jwt-openapi-scalar.md) for end-to-end document ownership.
- Review [FastEndpoints.OpenApi supply-chain metadata](../package-guidance/supply-chain.md#fastendpoints-openapi) before approval or upgrade.

## Security, performance, AOT, trimming, and operations

- JWT bearer security is added automatically unless `EnableJWTBearerAuth = false`; configure additional schemes with `AddAuth()`.
- Publish live documents only where the disclosure risk is accepted. For AOT production, export named JSON at build/deployment time and serve the static files.
- Runtime OpenAPI generation can repeat work for each request unless the host adds the documented output caching; prefer reviewed static artifacts or bounded caching for production exposure.
- `ExportOpenApiDocsAndExitAsync("v1")` performs work only when `export-openapi-docs=true`; place it immediately after `UseFastEndpoints()` so external side effects do not run during export.
- OpenAPI export document names must exactly match `.OpenApiDocument()` registrations.
- Treat export mode as a short-lived application startup: guard migrations, queue consumers, schedulers, and other side effects with the documented JSON-export-mode checks.

### Operational signals and troubleshooting

Observe generation/export duration, result status, artifact hash/size, and HTTP availability for each bounded document name. Never put secrets, real tokens, internal examples, or unreviewed server URLs in document metadata or logs.

| Symptom | Likely cause and diagnostic | Safe corrective action | Retry? |
| --- | --- | --- | --- |
| `/openapi/{name}.json` returns 404 | Document name differs between registration, route, UI, or export; enumerate registered names and inspect the requested URL | Use one exact, case-consistent name across producer and consumers | After correcting the name |
| Document omits or unexpectedly includes endpoints | Discovery, `EndpointFilter`, release-version bounds, or `ExcludeNonFastEndpoints` differs from policy; compare generated paths with endpoint metadata | Correct the reviewed filter/version configuration and regenerate | No automatic retry needed |
| Export starts normal application side effects | Export check occurs after migrations/workers or export call is misplaced; inspect startup ordering | Put export immediately after `UseFastEndpoints()` and guard side effects in export mode | Retry only after fixing startup order |
| Scalar shows an authorization scheme but calls remain unauthorized | OpenAPI describes security but JWT middleware/policy owns enforcement | Configure and test the authentication/authorization pipeline; do not weaken it | Only with a valid token |

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
