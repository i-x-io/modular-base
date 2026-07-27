# FastEndpoints.Generator

## Catalog entry

| Package | Pinned version | Role | Status |
| --- | --- | --- | --- |
| `FastEndpoints.Generator` | `8.2.0` | Compile-time FastEndpoints registrations, permissions, and serializer-context support | Centrally pinned; catalog-only until an endpoint project consumes it |

- Owner: IX
- Last reviewed: 2026-07-27
- Review trigger: Generator/FastEndpoints version, target framework, compiler, source-generation, trimming, or Native AOT changes.

## Decision and scope

Use the generator in each project that declares FastEndpoints endpoint types when opting into source-generated startup, generated permissions, or Native AOT support. It is a build-time asset, not a runtime service and must not flow to package consumers.

## Recommended registration and use

In every centrally managed project that declares endpoint types, add the versionless generator reference as a private analyzer/build asset:

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

For the Native AOT workflow, switch the host to generated discovery and register the generated JSON and reflection metadata. The generated extension-method suffix is derived from the assembly name, so replace `MyWebApp` with the actual suffix:

```csharp
using FastEndpoints;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddFastEndpoints(DiscoveredTypes.All);

var app = builder.Build();
app.UseFastEndpoints(options =>
{
    options.Serializer.Options.AddSerializerContextsFromMyWebApp();
    options.Binding.ReflectionCache.AddFromMyWebApp();
});
app.Run();
```

The same generator can create stable permission members from endpoint configuration; authorization behavior remains documented in [FastEndpoints.Security](fastendpoints-security.md):

```csharp
public override void Configure()
{
    Post("/orders");
    AccessControl(
        keyName: "Orders_Create",
        behavior: Apply.ToThisEndpoint);
}
```

## Enterprise implementation guidance

- Do not edit generated output. FastEndpoints requires generated serializer-context files to be checked in because its development-time tool bridges a source-generator chaining limitation; ordinary analyzer output remains build output.
- Enabling serializer-context generation invokes the package build targets, which can create a local tool manifest and install/update `FastEndpoints.Generator.Cli`. Pre-restore the pinned tool in controlled CI, account for feed/network access, and review manifest/generated-output changes.
- Use `PrivateAssets="all"` so a library does not impose the generator on consumers.
- Make permission keys stable and review them as API authorization identifiers. `AccessControl()` and partial `Allow` members generate stable hashed permission codes.
- When endpoints live in several assemblies, reference the generator in each endpoint assembly, pass all generated discovered-type collections needed by the host, and chain each assembly's serializer-context/reflection-cache extension methods.
- Keep source generation in CI and validate both regular build and publish/AOT build when AOT is a deployment target.

### Upgrade and rollback

Pin the generator to the same FastEndpoints version as the runtime packages. On upgrade, clean/rebuild each endpoint project, regenerate checked-in serializer contexts, review permission-code and discovered-type changes, and run both normal build and AOT publish checks where applicable; compiler and target-framework changes also trigger this review.

Rollback the generator and all FastEndpoints runtime companions together, then regenerate artifacts with the restored toolchain. Do not retain generated serializer contexts or permission mappings produced by a newer incompatible generator without a clean rebuild and review.

## Integration with the catalog

- [FastEndpoints](fastendpoints.md) owns the endpoint declarations that generator discovery examines.
- [FastEndpoints.Security](fastendpoints-security.md) documents `AccessControl()` and generated `Allow` permissions.
- [FastEndpoints.OpenApi](fastendpoints-openapi.md) exports documents for AOT deployments; serializer contexts and static document export complete that path.
- The [package-selection guide](../package-guidance/package-selection.md#api-authentication-ownership) explains the runtime authentication boundary for generated access-control metadata.
- The [FastEndpoints, JWT, OpenAPI, and Scalar recipe](../recipes/fastendpoints-jwt-openapi-scalar.md) shows the generated endpoint stack in context.
- Review [FastEndpoints.Generator supply-chain metadata](../package-guidance/supply-chain.md#fastendpoints-generator) before approval or upgrade.

## Security, performance, AOT, trimming, and operations

- Source-generated startup removes endpoint-assembly scanning from startup and is the FastEndpoints route for reflection-constrained publishing.
- Native AOT also needs `WebApplication.CreateSlimBuilder(args)`, `AddFastEndpoints(DiscoveredTypes.All)`, the generated serializer-context and reflection-cache extension methods, plus OpenAPI export; generator installation alone is insufficient.
- Retain generated serializer contexts and endpoint registrations through trimming by using the documented generator features rather than undocumented reflection workarounds.
- Review checked-in serializer-context changes alongside DTO changes, regenerate them before CI, and fail the build when the working tree would drift. Keep other diagnostic generated source under intermediate output.

## Avoid

- Do not install this only in a host project when endpoint declarations compile in another project.
- Do not expose it transitively from a reusable package.
- Do not hand-copy generated permission codes into access-control decisions; use the generated `Allow` members or stable declared keys.

## Verification checklist

- [ ] Every endpoint project has the private analyzer reference and central version `8.2.0`.
- [ ] A normal `dotnet build` succeeds with generated output enabled.
- [ ] Generated discovered types contain endpoints from every endpoint assembly.
- [ ] Checked-in serializer contexts are regenerated after DTO changes and have no unexplained diff.
- [ ] If publishing AOT, `dotnet publish -p:PublishAot=true` succeeds and the published process starts.
- [ ] Generated permissions retain their expected values across builds.

## Sources

- [FastEndpoints configuration settings](https://fast-endpoints.com/docs/configuration-settings) — Accessed 2026-07-27.
- [FastEndpoints model binding: serializer contexts](https://fast-endpoints.com/docs/model-binding) — Accessed 2026-07-27.
- [FastEndpoints native AOT project and generator setup](https://fast-endpoints.com/docs/native-aot) — Accessed 2026-07-27.
- [FastEndpoints security: access control](https://fast-endpoints.com/docs/security) — Accessed 2026-07-27.
- [FastEndpoints.Generator upstream source](https://github.com/FastEndpoints/FastEndpoints/tree/main/Src/Generator) — Accessed 2026-07-27.
- [NuGet: FastEndpoints.Generator 8.2.0](https://www.nuget.org/packages/FastEndpoints.Generator/8.2.0) — Accessed 2026-07-27.
