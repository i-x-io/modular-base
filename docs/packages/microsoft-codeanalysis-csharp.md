# Microsoft.CodeAnalysis.CSharp

## Catalog entry

`Microsoft.CodeAnalysis.CSharp` **5.6.0** — centrally pinned C#-specific Roslyn API for C# analyzers, source generators, and their isolated tests.

## Decision and scope

Use it when compiler tooling must parse C# source, inspect C# syntax, or create C# compilations. It extends the common Roslyn APIs; it is not a runtime dependency for normal libraries.

## Recommended registration and use

Reference it versionlessly from `Analyzer`, `SourceGenerator`, or compiler-tool test projects. Prefer symbols/semantic models for rules and use C# syntax only where the diagnostic contract explicitly requires source location or syntax form.

## Enterprise implementation guidance

Set deterministic parse and compilation options in tests. Test triggering and non-triggering snippets, diagnostic IDs, locations, messages, severities, generated-code exclusion, and role-sensitive behavior. Keep the package version aligned with the common Roslyn API.

## Integration with the catalog

`Directory.Packages.props` owns `5.6.0` with the companion Roslyn packages. The produced `IX.Modularity.Analyzers` package consumes it only at build/package time and exposes no C# compiler API as public runtime surface.

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
