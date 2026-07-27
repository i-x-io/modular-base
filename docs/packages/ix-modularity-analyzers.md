# IX.Modularity.Analyzers

## Catalog entry

`IX.Modularity.Analyzers` **0.1.0** — produced analyzer-only package that enforces complete XML documentation for public data objects, interfaces, interface members, service types, and service members, and suggests records for eligible class-shaped data objects.

## Decision and scope

The package is a deliberate compiler-time consumer opt-in. It ships analyzer assets only and does not include a runtime library, source generator, code fix, application framework, or external service integration.

## Recommended registration and use

Consumers add it as a private analyzer reference, with no runtime asset flow:

```xml
<PackageReference Include="IX.Modularity.Analyzers"
                  Version="0.1.0"
                  PrivateAssets="all"
                  IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
```

Configure `IXM1001` through `IXM1005` as errors where the consumer adopts this repository’s public-library documentation policy. `IXM2001` remains nonblocking because changing a public class to a record can change equality and compatibility semantics.

## Enterprise implementation guidance

Adopt the package first in a library with an explicit documentation ownership model. Correct public XML documentation before broadening the analyzer’s scope. Treat a local suppression as an exception record with justification, owner, and review date. Review documentation, public API baseline, tests, and package validation together for public changes.

## Integration with the catalog

This is a produced package, not an external central-catalog dependency. Its implementation uses centrally pinned Roslyn packages; its diagnostic contract is defined by [analyzer taxonomy](../architecture/analyzer-taxonomy.md), with navigation in [analyzer index](../architecture/analyzer-index.md). Repository severity is defined in `ModularBase.globalconfig` and `.editorconfig`.

## Security, performance, AOT, trimming, and operations

The analyzer runs during compilation and has no consumer runtime, trimming, or NativeAOT cost when packaged correctly. It must not access networks, repository paths, runtime reflection, or arbitrary files. Analyzer callbacks must remain bounded and cancellation-aware because they execute in developers’ and CI compiler processes.

## Avoid

Do not reference the package as a runtime assembly, package it under `lib/` or `ref/`, use a project-wide suppression to bypass XML documentation, or convert a class to a record without compatibility review solely to remove `IXM2001`.

## Verification checklist

- [ ] Confirm the package contains the DLL under `analyzers/dotnet/cs/` and no `lib/` or `ref/` assets.
- [ ] Compile a minimal consumer and confirm `IXM1001`–`IXM1005` load at the configured severity.
- [ ] Confirm `IXM2001` remains an info/suggestion diagnostic.
- [ ] Verify generated, inherited, implicit, and non-user-authored symbols do not produce the documentation diagnostics.

## Sources

- [Analyzer policy](../architecture/analyzer-policy.md) — Accessed 2026-07-27.
- [Roslyn analyzer tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix) — Accessed 2026-07-27.
- [NuGet analyzer conventions](https://learn.microsoft.com/en-us/nuget/guides/analyzers-conventions) — Accessed 2026-07-27.
