# Asp.Versioning.Http

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `Asp.Versioning.Http` | `10.0.0` | HTTP API versioning primitives and Minimal API/FastEndpoints-adjacent integration | Centrally pinned; catalog-only until an API project consumes it |

- Owner: IX
- Last reviewed: 2026-07-27
- Review trigger: `Asp.Versioning.Http` version, target framework, public API-version policy, or upstream routing/reader behavior changes.

## Decision and scope

Use this package when the API contract needs explicit HTTP API versions. It is infrastructure separate from FastEndpoints endpoint versions and FastEndpoints.OpenApi release-document filters. Choose a single externally documented reader strategy (URL segment, query string, header, or media type) and keep it stable.

## Recommended registration and use

Add the centrally versioned package to the consuming web project without repeating the version:

```xml
<ItemGroup>
  <PackageReference Include="Asp.Versioning.Http" />
</ItemGroup>
```

Register versioning at application composition time. The following is a minimal **raw Minimal API** URL-segment workflow; it is not a FastEndpoints endpoint registration pattern:

```csharp
using Asp.Versioning;

builder.Services.AddApiVersioning(options =>
{
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = false;
});

var versionedApi = app.NewVersionedApi("Orders");
var orders = versionedApi
    .MapGroup("/api/v{version:apiVersion}/orders")
    .HasDeprecatedApiVersion(1.0)
    .HasApiVersion(2.0);

orders.MapGet("/{id:guid}", (Guid id) => Results.Ok(new { id, apiVersion = "1.0" }))
    .MapToApiVersion(1.0);
orders.MapGet("/{id:guid}", (Guid id) => Results.Ok(new { id, apiVersion = "2.0" }))
    .MapToApiVersion(2.0);
```

`ReportApiVersions` advertises supported and deprecated versions in response headers. Keeping `AssumeDefaultVersionWhenUnspecified` false makes the client contract explicit; if a product deliberately supports an unversioned compatibility route, document and test that policy instead of enabling it incidentally.

Material version-selection options are policy, not incidental tuning:

| Setting | Purpose | Default behavior | Production guidance | Reload / sensitivity / failure behavior |
| --- | --- | --- | --- | --- |
| `ApiVersionReader` | Selects the request location used for a version | A combined query-string (`api-version`) and URL-segment reader | Choose and publish one reader; combine readers only when ambiguity behavior is tested | Startup configuration; version values are contract data, not secrets; missing/invalid/ambiguous values produce versioning errors |
| `AssumeDefaultVersionWhenUnspecified` | Allows requests without an explicit version | `false` | Keep `false` for explicit public contracts; if enabled, pair with a deliberate `DefaultApiVersion` | Startup configuration; a policy change can silently reroute existing clients |
| `ReportApiVersions` | Emits supported/deprecated version headers | `false` | Enable when clients and gateways can consume the lifecycle signal | Startup configuration; exposes supported contract versions but no credentials |

With FastEndpoints, establish how endpoint version metadata, routes, and OpenAPI document release groups map to the selected HTTP versioning strategy before adding public endpoints. Do not copy the Minimal API mapping example into FastEndpoints endpoint classes.

The existing FastEndpoints documentation supports endpoint/document version filtering through `MinEndpointVersion`, `MaxEndpointVersion`, and `ReleaseVersion`; that does not automatically install or configure Asp.Versioning HTTP readers.

## Enterprise implementation guidance

- Publish an API-version lifecycle: supported versions, deprecation notice, sunset date, and migration target.
- Prefer URL-segment versions where gateways, caches, and humans need an unambiguous resource identity; use other readers only with a clear client contract.
- Keep version selection, deprecation, and error responses consistent across all routes.
- Separate breaking contract versions from additive, backward-compatible changes. Avoid creating new versions merely for implementation changes.
- Exercise the client migration workflow before deprecation: publish the replacement document, add deprecation headers and a sunset notice, measure remaining v1 traffic, then retire the route only after the announced date.

### Upgrade and rollback

Upgrade `Asp.Versioning.Http` with its companion ASP.NET API Versioning packages and the target framework after checking the upstream release notes for reader, route-constraint, error-response, and deprecation behavior. Re-run every supported/unsupported/ambiguous version case and regenerate each versioned OpenAPI contract before deployment.

Rollback by restoring the previous centrally pinned package set and application artifact together. Do not roll back the documented external contract or remove a newly advertised version unless clients have also been rolled back; keep compatible routes active while the package rollback is validated.

## Integration with the catalog

- [FastEndpoints](fastendpoints.md) owns endpoint routing; versioning must be deliberately aligned with route templates and endpoint metadata.
- [FastEndpoints.OpenApi](fastendpoints-openapi.md) supports named documents and release/version document filtering; align document names with published HTTP versions.
- [Scalar.AspNetCore](scalar-aspnetcore.md) must receive only the names of documents that clients may explore.
- The [package-selection guide](../package-guidance/package-selection.md#api-authentication-ownership) places HTTP versioning beside, but outside, API authentication ownership.
- See the [FastEndpoints, JWT, OpenAPI, and Scalar recipe](../recipes/fastendpoints-jwt-openapi-scalar.md) for the surrounding API composition boundary.
- Review [Asp.Versioning.Http supply-chain metadata](../package-guidance/supply-chain.md#asp-versioning-http) before approval or upgrade.

## Security, performance, AOT, trimming, and operations

- Version readers are request parsing and should be simple, deterministic, and tested behind gateways/proxies.
- Do not let multiple ambiguous version readers silently select different contracts.
- Emit version-selection and deprecation telemetry without storing client secrets or bearer tokens.
- Include supported/deprecated versions in operations dashboards and release gates.

### Operational signals and troubleshooting

Track request counts and error rates by the normalized selected API version, plus traffic to deprecated versions. Do not record authorization headers, tokens, request bodies, or unbounded raw header/media-type values as telemetry dimensions.

| Symptom | Likely cause and diagnostic | Safe corrective action | Retry? |
| --- | --- | --- | --- |
| Requests return an unsupported-version response | Reader does not match the published client contract, route constraint is missing, or the requested version is not mapped; inspect route metadata and the normalized version value | Restore the documented reader/route mapping or update the client to a supported version | Only after correcting the request |
| A request reaches the wrong version | Default-version assumptions or combined readers make selection ambiguous; test the exact URL/query/header/media type through the gateway | Remove ambiguous readers or make the default policy explicit and documented | No; fix routing policy first |
| Deprecated-version traffic does not decline | Clients did not receive or act on lifecycle communication; compare response headers and version-tagged traffic | Publish migration guidance and preserve the route until the announced retirement criteria are met | Not applicable |

## Avoid

- Do not confuse FastEndpoints release grouping with HTTP API-version negotiation.
- Do not expose multiple undocumented readers for the same API.
- Do not remove a published version before its announced retirement window.

## Verification checklist

- [ ] A single reader strategy and default/unspecified-version behavior are documented.
- [ ] Each supported version maps to an intended OpenAPI document.
- [ ] Unsupported, deprecated, and ambiguous version requests have tested responses.
- [ ] Gateway/cache routing preserves the version discriminator.
- [ ] Observability distinguishes requested versions without recording credentials or sensitive request data.

## Sources

- [ASP.NET API Versioning project documentation](https://github.com/dotnet/aspnet-api-versioning/wiki) — Accessed 2026-07-27.
- [ASP.NET API Versioning: versioning a service](https://github.com/dotnet/aspnet-api-versioning/wiki/How-to-Version-Your-Service) — Accessed 2026-07-27.
- [ASP.NET API Versioning: API versioning options](https://github.com/dotnet/aspnet-api-versioning/wiki/API-Versioning-Options) — Accessed 2026-07-27.
- [FastEndpoints API versioning](https://fast-endpoints.com/docs/api-versioning) — Accessed 2026-07-27.
- [NuGet: Asp.Versioning.Http 10.0.0](https://www.nuget.org/packages/Asp.Versioning.Http/10.0.0) — Accessed 2026-07-27.
