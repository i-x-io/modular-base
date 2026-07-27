# FastEndpoints.Generator

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `FastEndpoints.Generator` | `8.2.0` | Compile-time FastEndpoints registrations, permissions, and serializer-context support | Centrally pinned; catalog-only until an endpoint project consumes it |

## Decision and scope

Use the generator in each project that declares FastEndpoints endpoint types when opting into source-generated startup, generated permissions, or Native AOT support. It is a build-time asset, not a runtime service and must not flow to package consumers.

## Recommended registration and use

In a centrally managed endpoint project, declare the generator as a private analyzer asset:

```xml
<PackageReference Include="FastEndpoints.Generator"
                  PrivateAssets="all"
                  IncludeAssets="runtime; build; native; contentfiles; analyzers" />
```

Enable generated JSON serializer contexts for the AOT path:

```xml
<PropertyGroup>
  <GenerateSerializerContexts>true</GenerateSerializerContexts>
  <SerializerContextOutputPath>Generated/SerializerCtx</SerializerContextOutputPath>
</PropertyGroup>
```

The package must be referenced by the project containing endpoints, not merely by a host that references an endpoint library. The generator discovers DTOs from endpoint declarations during compilation.

## Enterprise implementation guidance

- Treat generated output as compiler output: do not edit it. FastEndpoints requires generated serializer-context files to be checked in because its development-time tool bridges a source-generator chaining limitation.
- Use `PrivateAssets="all"` so a library does not impose the generator on consumers.
- Make permission keys stable and review them as API authorization identifiers. `AccessControl()` and partial `Allow` members generate stable hashed permission codes.
- Keep source generation in CI and validate both regular build and publish/AOT build when AOT is a deployment target.

## Integration with the catalog

- [FastEndpoints](fastendpoints.md) owns the endpoint declarations that generator discovery examines.
- [FastEndpoints.Security](fastendpoints-security.md) documents `AccessControl()` and generated `Allow` permissions.
- [FastEndpoints.OpenApi](fastendpoints-openapi.md) exports documents for AOT deployments; serializer contexts and static document export complete that path.

## Security, performance, AOT, trimming, and operations

- Source-generated startup removes endpoint-assembly scanning from startup and is the FastEndpoints route for reflection-constrained publishing.
- Native AOT also needs `WebApplication.CreateSlimBuilder(args)`, `AddFastEndpoints(DiscoveredTypes.All)`, the generated serializer-context and reflection-cache extension methods, plus OpenAPI export; generator installation alone is insufficient.
- Retain generated serializer contexts and endpoint registrations through trimming by using the documented generator features rather than undocumented reflection workarounds.
- Store generated source only when build diagnostics require it; otherwise keep it under the intermediate/output path to avoid accidental source-control drift.

## Avoid

- Do not install this only in a host project when endpoint declarations compile in another project.
- Do not expose it transitively from a reusable package.
- Do not hand-copy generated permission codes into access-control decisions; use the generated `Allow` members or stable declared keys.

## Verification checklist

- [ ] Every endpoint project has the private analyzer reference and central version `8.2.0`.
- [ ] A normal `dotnet build` succeeds with generated output enabled.
- [ ] If publishing AOT, `dotnet publish -p:PublishAot=true` succeeds and the published process starts.
- [ ] Generated permissions retain their expected values across builds.

## Sources

- [FastEndpoints configuration settings](https://fast-endpoints.com/docs/configuration-settings) — Accessed 2026-07-27.
- [FastEndpoints model binding: serializer contexts](https://fast-endpoints.com/docs/model-binding) — Accessed 2026-07-27.
- [FastEndpoints security: access control](https://fast-endpoints.com/docs/security) — Accessed 2026-07-27.
- [NuGet: FastEndpoints.Generator 8.2.0](https://www.nuget.org/packages/FastEndpoints.Generator/8.2.0) — Accessed 2026-07-27.
