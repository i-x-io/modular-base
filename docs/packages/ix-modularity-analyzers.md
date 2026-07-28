# IX.Modularity.Analyzers

## Catalog entry

`IX.Modularity.Analyzers` **0.1.0** — produced analyzer-only package that enforces complete XML documentation, FluentResults service return contracts, coded business failures, stack-preserving broad catches, and suggests records for eligible class-shaped data objects.

- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** Review whenever the package version, Roslyn dependency baseline, diagnostic IDs/default severities, analyzer asset layout, or repository target-framework/compiler policy changes.

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

Configure `IXM1001` through `IXM1005` and `IXM3001` through `IXM3003` as errors where the consumer adopts this repository’s public-library policy. `IXM2001` remains nonblocking because changing a public class to a record can change equality and compatibility semantics. `IXM3001` accepts only `Result`, `Result<T>`, and their `Task`/`ValueTask` wrappers for externally visible service operations. `IXM3002` rejects direct string-only, base-`Error`, and uncoded failures, including `Result.Try`. `IXM3003` requires an untyped or exact `Exception` catch to finish every reachable path with bare `throw;`.

The package has no DI or runtime configuration. Consumers control analyzer loading and policy through MSBuild and analyzer configuration:

| Setting | Repository recommendation | Effect |
| --- | --- | --- |
| `PrivateAssets="all"` | Required | Prevents the analyzer package from flowing transitively from a library to its consumers. |
| `IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive"` | Retain the standard private-analyzer form | Keeps the analyzer asset available to the current project while the private reference prevents downstream propagation. |
| `dotnet_diagnostic.IXM1001.severity` through `IXM1005` | `error` | Adopts the repository’s build-blocking documentation contract; the package descriptors themselves default these rules to warning. |
| `dotnet_diagnostic.IXM2001.severity` | `suggestion` | Preserves the package’s nonblocking design recommendation; its descriptor defaults to info. |
| `dotnet_diagnostic.IXM3001.severity` through `IXM3003` | `error` | Adopts the repository’s build-blocking result and exception-handling contract; the package descriptors themselves default these rules to warning. |

Put shared severities in a checked-in `.globalconfig` or `.editorconfig`. Keep any exception narrow to the affected diagnostic and declaration; do not disable the entire analyzer family to accommodate one intentional class-shaped contract.

## Enterprise implementation guidance

Adopt the package first in a library with an explicit documentation ownership model. Correct public XML documentation before broadening the analyzer’s scope. Treat a local suppression as an exception record with justification, owner, and review date. Review documentation, public API baseline, tests, and package validation together for public changes.

### Upgrade and rollback

The initial `0.1.0` package has not been published. Its shipped contract includes `IXM1001`–`IXM1005`, `IXM2001`, and `IXM3001`–`IXM3003`. Before publishing it, review `AnalyzerReleases.Shipped.md` for that complete contract and verify `AnalyzerReleases.Unshipped.md` contains only future work. Keep `ModularBase.globalconfig`, `.editorconfig`, the analyzer taxonomy, per-diagnostic help, tests, and package version aligned. Pack the candidate and inspect it before publishing: the analyzer DLL and symbols belong under `analyzers/dotnet/cs/`, while `lib/` and `ref/` must remain absent.

Pin one consumer to the `0.1.0` candidate, restore, build, and classify every diagnostic before publishing. An `IXM100x` failure normally requires completing documentation or narrowing public surface; an `IXM2001` result requires semantic review, not an automatic class-to-record conversion; `IXM3001` requires a supported result return shape; `IXM3002` requires an own coded error; and `IXM3003` requires bare rethrow from broad catches. After publication, treat a diagnostic-ID reuse, changed default severity, or changed symbol-selection behavior as a contract change.

Rollback by restoring the previously approved package version in the consumer or release manifest and rebuilding from a clean restore. If the upgrade also changed repository severity policy, revert those severity entries with the package pin so the previous diagnostic contract is restored as one unit. Do not use blanket `NoWarn` or project-wide `none` severities as a rollback: they hide whether the prior analyzer is loaded and leave documentation enforcement weakened.

## Integration with the catalog

This is a produced package, not an external central-catalog dependency. Its implementation uses centrally pinned [Microsoft.CodeAnalysis.Analyzers](microsoft-codeanalysis-analyzers.md), [Microsoft.CodeAnalysis.Common](microsoft-codeanalysis-common.md), and [Microsoft.CodeAnalysis.CSharp](microsoft-codeanalysis-csharp.md) packages only while building the analyzer. Those Roslyn dependencies are private and do not become consumer runtime dependencies.

The diagnostic contract is defined by [analyzer taxonomy](../architecture/analyzer-taxonomy.md), with navigation in [analyzer index](../architecture/analyzer-index.md). Repository severity is defined in `ModularBase.globalconfig` and `.editorconfig`. Publisher, license, source, provenance, and dependency facts are consolidated in the [supply-chain reference](../package-guidance/supply-chain.md#ix-modularity-analyzers).

## Security, performance, AOT, trimming, and operations

The analyzer runs during compilation and has no consumer runtime, trimming, or NativeAOT cost when packaged correctly. Runtime troubleshooting, logs, metrics, health checks, and deployment signals therefore do not apply. It must not access networks, repository paths, runtime reflection, or arbitrary files. Analyzer callbacks must remain bounded and cancellation-aware because they execute in developers’ and CI compiler processes; track build duration and compiler memory externally when evaluating a future analyzer implementation change.

## Avoid

Do not reference the package as a runtime assembly, package it under `lib/` or `ref/`, use a project-wide suppression to bypass XML documentation, or convert a class to a record without compatibility review solely to remove `IXM2001`.

## Verification checklist

- [ ] Confirm the package contains the DLL under `analyzers/dotnet/cs/` and no `lib/` or `ref/` assets.
- [ ] Compile a minimal consumer and confirm `IXM1001`–`IXM1005` and `IXM3001`–`IXM3003` load at the configured severity.
- [ ] Confirm `IXM2001` remains an info/suggestion diagnostic.
- [ ] Verify generated, inherited, implicit, and non-user-authored symbols do not produce the documentation diagnostics.
- [ ] Before publishing `0.1.0`, compare the complete shipped `IXM1001`–`IXM1005`, `IXM2001`, and `IXM3001`–`IXM3003` contract with the analyzer taxonomy, help pages, descriptor defaults, and consumer severity configuration.
- [ ] Exercise rollback by restoring the last approved package pin and severity policy, then confirm the previous diagnostic set and severities on a clean build.

## Sources

- [Analyzer policy](../architecture/analyzer-policy.md) — Accessed 2026-07-27.
- Analyzer release record: `src/IX.Modularity.Analyzers/AnalyzerReleases.Shipped.md` — Accessed 2026-07-27.
- [Produced package project](../../src/IX.Modularity.Analyzers/IX.Modularity.Analyzers.csproj) — Accessed 2026-07-27.
- [Roslyn analyzer tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix) — Accessed 2026-07-27.
- [Microsoft: configure code-analysis rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files) — Accessed 2026-07-27.
- [NuGet analyzer conventions](https://learn.microsoft.com/en-us/nuget/guides/analyzers-conventions) — Accessed 2026-07-27.
