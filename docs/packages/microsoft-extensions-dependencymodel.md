# Microsoft.Extensions.DependencyModel

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Runtime access to the `.deps.json` dependency context | Approved only for metadata/discovery scenarios |

## Decision and scope

Use this package only when tooling or infrastructure must inspect compile/runtime library metadata emitted into a `.deps.json` file. It is not a general-purpose plugin loader, assembly scanner, or DI registration mechanism.

## Recommended registration and use

Read `DependencyContext.Default` only in a controlled metadata-discovery boundary and handle its absence. Load known assemblies through normal references whenever possible. Keep any reflection scan narrow, deterministic, and independent from the dependency context's ordering.

## Enterprise implementation guidance

Prefer explicit extension points, manifests, or compile-time source generation for plugins and registrations. If metadata discovery is unavoidable, constrain the allowed assemblies, validate names and versions, and publish the expected runtime assets as part of release verification.

## Integration with the catalog

Use [DependencyInjection.Abstractions](microsoft-extensions-dependencyinjection-abstractions.md) for normal registration contracts; do not use dependency metadata to replace explicit registration. [Hosting](microsoft-extensions-hosting.md) remains responsible for process composition.

## Security, performance, AOT, trimming, and operations

Reflection and dynamic assembly loading are trimming/AOT-sensitive and can create an untrusted-code execution path. Do not load assemblies based on untrusted configuration or tenant input. Measure startup overhead and cache discovery output when it is safe to do so. Test self-contained, single-file, trimmed, and native deployments if this package remains necessary.

## Avoid

- Do not assume `DependencyContext.Default` is present in every deployment model.
- Do not scan/load every dependency at application startup.
- Do not use it to discover or execute untrusted plugins.

## Verification checklist

- The metadata need cannot be met by an explicit manifest or source generation.
- Missing dependency context has deterministic behavior.
- Every supported publish mode has an integration test for discovery.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.DependencyModel) (Accessed 2026-07-27)
- [DependencyContext API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencymodel.dependencycontext) (Accessed 2026-07-27)
