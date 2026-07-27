# Microsoft.CodeAnalysis.CSharp

## Catalog entry

`Microsoft.CodeAnalysis.CSharp` **5.6.0** — centrally pinned C#-specific Roslyn API for C# analyzers, source generators, and their isolated tests.

- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** Review when the CSharp/Common pins, supported C# language version, SDK/compiler host, or analyzer test compilation options change.

## Decision and scope

Use it when compiler tooling must parse C# source, inspect C# syntax, or create C# compilations. It extends the common Roslyn APIs; it is not a runtime dependency for normal libraries.

## Recommended registration and use

Reference it versionlessly from `Analyzer`, `SourceGenerator`, or compiler-tool test projects. Prefer symbols/semantic models for rules and use C# syntax only where the diagnostic contract explicitly requires source location or syntax form.

| Test compilation setting | Catalog guidance |
| --- | --- |
| `LanguageVersion` | Set explicitly for language-version-sensitive diagnostics and include the oldest supported version. |
| `NullableContextOptions` | Exercise enabled and disabled contexts when nullability affects the rule contract. |
| `OutputKind` and references | Construct the minimal deterministic compilation shape required by the test. |
| `DocumentationMode` / preprocessor symbols | Pin only when the diagnostic depends on documentation or conditional syntax; include the setting in the fixture name. |

## Enterprise implementation guidance

Set deterministic parse and compilation options in tests. Test triggering and non-triggering snippets, diagnostic IDs, locations, messages, severities, generated-code exclusion, and role-sensitive behavior. Keep the package version aligned with the common Roslyn API.

### Upgrade and rollback

Upgrade with `Microsoft.CodeAnalysis.Common` and `Microsoft.CodeAnalysis.Analyzers`. Run the diagnostic suite across the supported C# language-version matrix and inspect syntax/operation changes, diagnostic spans, code-fix output, and generator snapshots. If compiler API or syntax-tree changes alter the public diagnostic contract or an intended host cannot load the analyzer, restore all Roslyn pins and lock files to `5.6.0`; do not compensate by weakening assertions until the behavior change is explicitly accepted.

## Integration with the catalog

`Directory.Packages.props` owns `5.6.0` with the companion Roslyn packages. The produced `IX.Modularity.Analyzers` package consumes it only at build/package time and exposes no C# compiler API as public runtime surface. Its [supply-chain record](../package-guidance/supply-chain.md#microsoft-codeanalysis-csharp) identifies the approved source and upstream.

## Security, performance, AOT, trimming, and operations

It runs in compiler tooling. Do not read arbitrary files, execute code, or make network calls from analyzers. Its runtime/AOT/trimming implications are avoided by analyzer-only packaging.

## Avoid

Do not use C# textual heuristics where semantic analysis is required, mix incompatible Roslyn package versions, or reference it from normal runtime projects.

## Verification checklist

- [ ] The reference is versionless and centrally resolves `5.6.0`.
- [ ] Tests use deterministic C# compilation inputs.
- [ ] The analyzer package contains no `lib/` or `ref/` runtime assets.

## Sources

- [Roslyn SDK overview](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/) — Accessed 2026-07-27.
- [Microsoft.CodeAnalysis.CSharp 5.6.0 on NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/5.6.0) — Accessed 2026-07-27.
