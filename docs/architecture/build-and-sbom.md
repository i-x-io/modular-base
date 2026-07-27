# Build and SBOM

## Build entry points

The root [`Makefile`](../../Makefile) is the public build interface. It delegates to the internal `eng/Build.proj` orchestration project; developers and automation call `make` targets rather than invoking the MSBuild project directly. The build discovers `.sln`, `.slnx`, and language project files and prefers the solution when it contains projects. `CONFIGURATION` is allow-listed to `Debug` and `Release` and defaults to `Release`.

Common invocations are `make`, `make validate`, `make tool-restore`, `make build CONFIGURATION=Debug`, and `make test CONFIGURATION=Debug`.

The Makefile is the only public build interface. Documentation must not direct consumers to invoke `eng/Build.proj` or a shared target directly; those are internal implementation details.

The target contract is:

| Target | Behavior for the current solution |
| --- | --- |
| `Validate` | Validate `global.json`, the central catalog, `NuGet.Config`, and tool manifest. |
| `ToolRestore` | Validate, then restore local tools. |
| `Restore` | Restore the populated `IX.Modularity.slnx` solution. |
| `Format` | Run `dotnet format --no-restore --verify-no-changes` for the solution. |
| `Build` | Build the solution in the selected configuration. |
| `Test` | Run the solution tests in the selected configuration. `make test CONFIGURATION=Debug` is the authoritative ArchUnit bytecode check; `Release` is also supported. |
| `Audit` | Run the noun-first `dotnet package list --project <project> ... --include-transitive --vulnerable --format json --output-version 1 --no-restore` audit for every restored project. Project-level batching avoids incomplete `.slnx` package-list output. |
| `Outdated`, `Sbom` | Restore tools, then operate on the solution. |

### Current solution contents

`IX.Modularity.slnx` contains `test/IX.Modularity.Architecture.Tests`, a non-packable project with the `ArchitectureTest` role. It enforces the repository's architectural governance without adding a production project under `src/`. Build, restore, test, audit, outdated-package scanning, and SBOM generation therefore run against a real solution and project graph.

The project role, permitted dependency direction, and rule force are defined by [project structure](project-structure.md), [architectural rules](architectural-rules.md), and [architecture terminology](terminology.md). This build document describes how the checks run; those documents remain the normative architecture contract.

## Build output and package artifacts

Future projects inherit deterministic builds, portable PDBs, source embedding, repository URLs, CI build behavior, XML documentation files, and an `artifacts/` output root. Packable projects additionally inherit MIT package metadata, symbols (`snupkg`), package validation, and the repository `README.md` and `NOTICE` as package content.

## SBOM decision

Use **CycloneDX JSON** as the repository SBOM output format. The checked-in local tool is `CycloneDX` `6.2.0`, exposed as `dotnet-CycloneDX`. CycloneDX is selected because the configured .NET generator natively produces a dependency BOM from solution/project input and supports an explicit JSON output format. SPDX is not a second generated format in this build: it is an alternative SBOM standard, not an additional authoritative artifact. Produce SPDX only if a downstream compliance consumer explicitly requires it, with that consumer’s validation rules recorded alongside the export.

For the current root solution, `Sbom` writes `bom.cdx.json` beneath `artifacts/sbom/solutions/IX.Modularity/`. When no solution exists, it writes one BOM per discovered project beneath `artifacts/sbom/projects/<relative-entry>/`. The target owns this location and filename; callers do not override them.

For the current solution, the configured target executes this equivalent command:

```sh
dotnet tool run dotnet-CycloneDX -- IX.Modularity.slnx \
  --output artifacts/sbom/solutions/IX.Modularity \
  --filename bom.cdx.json \
  --output-format Json
```

The command requires the restored local tools and the solution's restored dependencies.

## Future per-package BOMs

For package releases, retain the project-level `artifacts/sbom/projects/<relative-entry>/bom.cdx.json` artifact and associate it with the package ID and version in release automation. Do not generate a catalog-wide BOM: catalog entries are not an installed dependency graph. A solution-level BOM in `artifacts/sbom/solutions/<relative-entry>/bom.cdx.json` may complement an application release, but it must not replace package-specific provenance.

## Sources

- [CycloneDX for .NET](https://github.com/CycloneDX/cyclonedx-dotnet) — Accessed 2026-07-27.
- [CycloneDX specification](https://github.com/CycloneDX/specification) — Accessed 2026-07-27.
- [dotnet tool restore](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-restore) — Accessed 2026-07-27.
- [dotnet outdated](https://github.com/dotnet-outdated/dotnet-outdated) — Accessed 2026-07-27.
