# Microsoft.Extensions.DependencyModel

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Runtime access to the `.deps.json` dependency context | Direct; approved only for metadata/discovery scenarios |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, `.deps.json` format, publish mode, or runtime dependency-context change |

## Decision and scope

Use this package only when tooling or infrastructure must inspect compile/runtime library metadata emitted into a `.deps.json` file. It is not a general-purpose plugin loader, assembly scanner, or DI registration mechanism.

## Recommended registration and use

Read `DependencyContext.Default` only in a controlled metadata-discovery boundary and handle its absence. Load known assemblies through normal references whenever possible. Keep any reflection scan narrow, deterministic, and independent from the dependency context's ordering.

With Central Package Management, reference the catalog-managed version from the tooling or infrastructure project that owns discovery:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyModel" />
</ItemGroup>
```

Use the context as metadata, not as permission to load or execute code. This diagnostic example lists package libraries recorded for the current application and handles deployment modes where no context is available:

```csharp
using Microsoft.Extensions.DependencyModel;

DependencyContext? context = DependencyContext.Default;
if (context is null)
{
    Console.Error.WriteLine("No dependency context is available for this application.");
    return 2;
}

var packages = context.RuntimeLibraries
    .Where(library => string.Equals(library.Type, "package", StringComparison.Ordinal))
    .OrderBy(library => library.Name, StringComparer.Ordinal)
    .Select(library => $"{library.Name} {library.Version}");

foreach (string package in packages)
{
    Console.WriteLine(package);
}

return 0;
```

For a specific loaded assembly, `DependencyContext.Load(assembly)` returns that assembly's context or `null` when it is unavailable. `CompileLibraries` describes compile-time libraries, while `RuntimeLibraries` describes runtime assets selected for the target; neither collection guarantees that an arbitrary assembly file exists beside the application.

## Enterprise implementation guidance

Prefer explicit extension points, manifests, or compile-time source generation for plugins and registrations. If metadata discovery is unavoidable, constrain the allowed assemblies, validate names and versions, and publish the expected runtime assets as part of release verification.

A common controlled workflow is: read metadata, filter against an application-owned allowlist, sort it for deterministic output, record the decision, and then invoke a separately reviewed loader only for approved extensions. Keep discovery and loading as separate interfaces so metadata inspection cannot accidentally become code execution.

### Upgrade and rollback

Upgrade only after testing against `.deps.json` files produced by every supported target framework, runtime identifier, single-file setting, and trim mode. Treat metadata shape and file availability as deployment contracts, not implementation details. No application-data migration is required. Roll back the tooling/runtime component together with its parser assumptions; retain representative previous deployment artifacts for regression tests.

## Integration with the catalog

Use [DependencyInjection.Abstractions](microsoft-extensions-dependencyinjection-abstractions.md) for normal registration contracts; do not use dependency metadata to replace explicit registration. [Hosting](microsoft-extensions-hosting.md) remains responsible for process composition.

See the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-dependencymodel) for provenance and dependency metadata.

## Security, performance, AOT, trimming, and operations

Reflection and dynamic assembly loading are trimming/AOT-sensitive and can create an untrusted-code execution path. Do not load assemblies based on untrusted configuration or tenant input. Measure startup overhead and cache discovery output when it is safe to do so. Test self-contained, single-file, trimmed, and native deployments if this package remains necessary.

Single-file deployment bundles `.deps.json` data, so do not infer physical file paths from dependency metadata. Dynamic assembly loading and broad reflection are not statically analyzable by the trimmer; resolve trim warnings rather than suppressing them by default. For a plugin that genuinely requires separate files, make its publish layout and integrity policy explicit and test the published artifact for every supported runtime identifier.

## Avoid

- Do not assume `DependencyContext.Default` is present in every deployment model.
- Do not scan/load every dependency at application startup.
- Do not use it to discover or execute untrusted plugins.
- Do not treat `RuntimeLibraries` order or contents as a stable application protocol.

## Verification checklist

- [ ] The metadata need cannot be met by an explicit manifest or source generation.
- [ ] Missing dependency context has deterministic behavior.
- [ ] Every supported publish mode has an integration test for discovery.
- [ ] Discovery filters and sorts metadata and never loads code before allowlist validation.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.DependencyModel) (Accessed 2026-07-27)
- [DependencyContext API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencymodel.dependencycontext) (Accessed 2026-07-27)
- [`DependencyContext.Load` API reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencymodel.dependencycontext.load?view=net-10.0) (Accessed 2026-07-27)
- [Single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview) (Accessed 2026-07-27)
- [Known trimming incompatibilities](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/incompatibilities) (Accessed 2026-07-27)
