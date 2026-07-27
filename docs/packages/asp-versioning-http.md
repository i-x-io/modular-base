# Asp.Versioning.Http

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `Asp.Versioning.Http` | `10.0.0` | HTTP API versioning primitives and Minimal API/FastEndpoints-adjacent integration | Centrally pinned; catalog-only until an API project consumes it |

## Decision and scope

Use this package when the API contract needs explicit HTTP API versions. It is infrastructure separate from FastEndpoints endpoint versions and FastEndpoints.OpenApi release-document filters. Choose a single externally documented reader strategy (URL segment, query string, header, or media type) and keep it stable.

## Recommended registration and use

Register versioning at application composition time, then map version-aware HTTP endpoints according to the package’s current API. With FastEndpoints, establish how endpoint version metadata, routes, and OpenAPI document release groups map to the selected HTTP versioning strategy before adding public endpoints.

The existing FastEndpoints documentation supports endpoint/document version filtering through `MinEndpointVersion`, `MaxEndpointVersion`, and `ReleaseVersion`; that does not automatically install or configure Asp.Versioning HTTP readers.

## Enterprise implementation guidance

- Publish an API-version lifecycle: supported versions, deprecation notice, sunset date, and migration target.
- Prefer URL-segment versions where gateways, caches, and humans need an unambiguous resource identity; use other readers only with a clear client contract.
- Keep version selection, deprecation, and error responses consistent across all routes.
- Separate breaking contract versions from additive, backward-compatible changes. Avoid creating new versions merely for implementation changes.

## Integration with the catalog

- [FastEndpoints](fastendpoints.md) owns endpoint routing; versioning must be deliberately aligned with route templates and endpoint metadata.
- [FastEndpoints.OpenApi](fastendpoints-openapi.md) supports named documents and release/version document filtering; align document names with published HTTP versions.
- [Scalar.AspNetCore](scalar-aspnetcore.md) must receive only the names of documents that clients may explore.

## Security, performance, AOT, trimming, and operations

- Version readers are request parsing and should be simple, deterministic, and tested behind gateways/proxies.
- Do not let multiple ambiguous version readers silently select different contracts.
- Emit version-selection and deprecation telemetry without storing client secrets or bearer tokens.
- Include supported/deprecated versions in operations dashboards and release gates.

## Avoid

- Do not confuse FastEndpoints release grouping with HTTP API-version negotiation.
- Do not expose multiple undocumented readers for the same API.
- Do not remove a published version before its announced retirement window.

## Verification checklist

- [ ] A single reader strategy and default/unspecified-version behavior are documented.
- [ ] Each supported version maps to an intended OpenAPI document.
- [ ] Unsupported, deprecated, and ambiguous version requests have tested responses.
- [ ] Gateway/cache routing preserves the version discriminator.

## Sources

- [ASP.NET API Versioning project documentation](https://github.com/dotnet/aspnet-api-versioning/wiki) — Accessed 2026-07-27.
- [FastEndpoints API versioning](https://fast-endpoints.com/docs/api-versioning) — Accessed 2026-07-27.
- [NuGet: Asp.Versioning.Http 10.0.0](https://www.nuget.org/packages/Asp.Versioning.Http/10.0.0) — Accessed 2026-07-27.
