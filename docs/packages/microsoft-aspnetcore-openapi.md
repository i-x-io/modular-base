# Microsoft.AspNetCore.OpenApi

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `Microsoft.AspNetCore.OpenApi` | `10.0.10` | ASP.NET Core OpenAPI generation, endpoint mapping, and transformer APIs | Centrally pinned; consumed as FastEndpoints.OpenApi infrastructure |

- Owner: IX
- Last reviewed: 2026-07-27
- Review trigger: Package/target-framework servicing update, OpenAPI transformer/source-generator behavior, or Microsoft.OpenApi compatibility changes.

## Decision and scope

This is the first-party ASP.NET Core OpenAPI implementation. In a FastEndpoints application, `FastEndpoints.OpenApi` owns the public registration surface and wires its transformers into this package. Use raw `AddOpenApi()`/`MapOpenApi()` directly only for non-FastEndpoints endpoints or a deliberately separate document pipeline.

## Recommended registration and use

Add the centrally versioned package only to a project that owns an OpenAPI pipeline:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
</ItemGroup>
```

For the FastEndpoints document pipeline, use:

```csharp
builder.Services
    .AddFastEndpoints()
    .OpenApiDocument(o => o.DocumentName = "v1");

app.UseFastEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

For a raw Minimal API-only pipeline, Microsoft documents `builder.Services.AddOpenApi()` followed by `app.MapOpenApi()`; gate the mapped route to development or require an explicit protected authorization policy in production. Do not combine that service registration with a FastEndpoints document for the same contract.

A separate raw Minimal API pipeline can name and customize a document at registration time:

```csharp
builder.Services.AddOpenApi("internal", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Internal Operations API";
        document.Info.Version = "internal";
        return Task.CompletedTask;
    });
});

app.MapOpenApi("/openapi/{documentName}.json")
    .RequireAuthorization("ApiDocs");
```

Document transformers run once per generated document, operation transformers run once per operation, and schema transformers run once per schema. Keep transformations deterministic and use endpoint metadata when security requirements or visibility vary by endpoint. The authorization on `MapOpenApi()` protects document retrieval; it does not protect the API operations described by the document.

| Setting / API | Purpose | Default behavior | Production guidance | Reload / sensitivity / failure behavior |
| --- | --- | --- | --- | --- |
| Document name | Selects an independently generated contract | The default document is used when no name is supplied | Use stable, explicit names for every published contract | Registered at startup; unknown names produce no matching document |
| `MapOpenApi(pattern)` | Maps the runtime JSON endpoint | Framework route pattern applies when omitted | Gate to development or require a reviewed authorization policy | Endpoint mapping is startup-only; route output may disclose contract details |
| Document transformer | Changes document-level metadata/content | No custom transformation | Keep deterministic and output-test the result | Executes during generation; exceptions fail document generation |
| Operation/schema transformers | Change each operation/schema | No custom transformation | Avoid network calls and unbounded per-item work | Execute per item during generation; cost scales with contract size |

## Enterprise implementation guidance

- Register and expose only intentional named documents.
- Use the package transformer APIs only when they complement the FastEndpoints transformer path; otherwise document and test precedence carefully.
- Keep runtime documents limited to approved environments. Publish static artifacts where the production contract must be available without runtime generation.
- Treat document generation as build/deployment contract verification, not merely an explorer feature.
- For a contract workflow, generate the document in CI, lint or diff the artifact, review intentional breaking changes, and publish the approved immutable artifact with the release.

### Upgrade and rollback

Apply the package with the compatible ASP.NET Core runtime/target framework and keep `Microsoft.OpenApi` on the supported 2.x line for the pinned 10.0.10 package. Regenerate and parse every named document, test transformer output and route authorization, and repeat Native AOT generation checks before rollout.

Rollback the ASP.NET Core OpenAPI package/runtime-compatible set and restore the last approved document artifact together. Do not keep a document produced by a newer incompatible model/source generator if the restored application no longer implements that contract.

## Integration with the catalog

- [FastEndpoints.OpenApi](fastendpoints-openapi.md) wraps the correct registration path for FastEndpoints.
- [Microsoft.OpenApi](microsoft-openapi.md) is the document object model dependency and is constrained to the compatible 2.x line.
- [Scalar.AspNetCore](scalar-aspnetcore.md) serves a UI over the document endpoint/static JSON.
- [FastEndpoints.Generator](fastendpoints-generator.md) and document export provide the AOT-compatible FastEndpoints path.
- The [package-selection guide](../package-guidance/package-selection.md#api-authentication-ownership) explains why document security metadata does not own request authentication.
- Follow the [FastEndpoints, JWT, OpenAPI, and Scalar recipe](../recipes/fastendpoints-jwt-openapi-scalar.md) when FastEndpoints owns registration.
- Review [Microsoft.AspNetCore.OpenApi supply-chain metadata](../package-guidance/supply-chain.md#microsoft-aspnetcore-openapi) before approval or servicing updates.

## Security, performance, AOT, trimming, and operations

- ASP.NET Core OpenAPI supports runtime documents and transformer APIs; it is compatible with Native AOT in the documented first-party path.
- Runtime documents can reveal routes, schemas, parameter names, examples, and security descriptions. Gate them intentionally.
- Runtime document endpoints regenerate output per request unless the host applies the documented output caching. Prefer reviewed static artifacts or bounded caching when production retrieval is required.
- Keep generated documents in release verification and check for internal types/routes before publication.
- With the pinned 10.0.10 package, keep `Microsoft.OpenApi` in the compatible **2.x** range. The catalog pins `2.11.0`; direct 3.x references are incompatible with the ASP.NET Core 10 OpenAPI source generator.

### Operational signals and troubleshooting

Observe document-route status, generation duration/failures, artifact hash/size, and transformer exceptions for bounded document names. Do not place credentials, confidential examples, internal server addresses, or sensitive endpoint metadata in the document or diagnostic payloads.

| Symptom | Likely cause and diagnostic | Safe corrective action | Retry? |
| --- | --- | --- | --- |
| OpenAPI route returns 404 | `MapOpenApi()` was not mapped in this environment, the name/pattern is wrong, or authorization hides expected access; inspect endpoint mapping and requested name | Map the reviewed route and use the registered name/pattern; keep intended authorization | After correcting route/access |
| Generation fails in a transformer | Transformer throws, performs unavailable I/O, or assumes missing metadata; inspect the sanitized exception and transformer stage | Make transformation deterministic/null-safe and cover output in tests | Only for a proven transient dependency; prefer no external I/O |
| Build/source generation fails with model-type errors | Incompatible `Microsoft.OpenApi` major is resolved | Restore the catalog's compatible 2.x dependency graph and clean/rebuild | No |
| Document exposes an internal route/schema | Filtering or endpoint metadata is incomplete | Remove it from the public pipeline, regenerate, diff, and rotate any accidentally disclosed secrets | No; treat disclosure separately |

## Avoid

- Do not call raw `AddOpenApi()` for a FastEndpoints document.
- Do not add `Microsoft.OpenApi` 3.x alongside `Microsoft.AspNetCore.OpenApi` 10.0.10.
- Do not mistake an OpenAPI description for authorization enforcement.

## Verification checklist

- [ ] FastEndpoints applications use `.OpenApiDocument()`, not direct `AddOpenApi()`.
- [ ] The resolved dependency graph contains `Microsoft.OpenApi` `2.11.0` (or a documented compatible 2.x update), never 3.x.
- [ ] Document endpoints/static artifacts contain only approved public contract data.
- [ ] The chosen runtime/AOT document generation path is tested in CI.
- [ ] Named-document routes, transformer output, and endpoint authorization are covered by integration tests.

## Sources

- [Microsoft: OpenAPI support overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [Microsoft: generate OpenAPI documents](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [Microsoft: customize an OpenAPI document](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [Microsoft: use generated OpenAPI documents](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [FastEndpoints OpenAPI documents](https://fast-endpoints.com/docs/openapi-documents) — Accessed 2026-07-27.
- [NuGet: Microsoft.AspNetCore.OpenApi 10.0.10](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/10.0.10) — Accessed 2026-07-27.
