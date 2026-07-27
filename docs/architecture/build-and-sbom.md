# Build and SBOM

## Build entry points

The root [`Makefile`](../../Makefile) is the public build interface. It delegates to the internal `eng/Build.proj` orchestration project; developers and automation call `make` targets rather than invoking the MSBuild project directly. The build discovers `.sln`, `.slnx`, and language project files. It prefers a solution when projects exist and otherwise reports the projectless skip behavior. `CONFIGURATION` is allow-listed to `Debug` and `Release` and defaults to `Release`.

Common invocations are `make`, `make validate`, `make tool-restore`, and `make build CONFIGURATION=Debug`.

The target contract is:

| Target | Intended behavior when an entry exists | Projectless behavior |
| --- | --- | --- |
| `Validate` | Validate `global.json`, the central catalog, `NuGet.Config`, and tool manifest. | Runs without a project in principle. |
| `ToolRestore` | Validate, then restore local tools. | Does not require a project. |
| `Restore` | Restore each discovered solution or project. | Invokes `SkipRestore`; no project operation is attempted. |
| `Format` | Run `dotnet format --no-restore --verify-no-changes` for each discovered solution or project. | Invokes `SkipFormat`; no project operation is attempted. |
| `Build`, `Test` | Build or test each discovered solution or project. | Invoke their `Skip*` path; no project operation is attempted. |
| `Audit` | Run the noun-first `dotnet package list ... --include-transitive --vulnerable --format json --output-version 1` audit for each discovered solution or project. | Invokes `SkipAudit`; no project operation is attempted. |
| `Outdated`, `Sbom` | Restore tools, then operate on each discovered solution or project. | Invoke their `Skip*` path; no project operation is attempted. |

### Current projectless behavior

The repository currently has an empty `IX.Modularity.slnx` solution and zero supported project files, so no compilation, tests, package audit, outdated-package scan, package lock-file generation, or project SBOM can occur. `make`, `make validate`, `make tool-restore`, and every projectless target path pass: `Validate` checks the repository configuration, `ToolRestore` restores the local tools, and project-dependent targets emit their intentional skip message. These checks validate the repository policy and orchestration, not a package build.

## Build output and package artifacts

Future projects inherit deterministic builds, portable PDBs, source embedding, repository URLs, CI build behavior, XML documentation files, and an `artifacts/` output root. Packable projects additionally inherit MIT package metadata, symbols (`snupkg`), package validation, and the repository `README.md` and `NOTICE` as package content.

## SBOM decision

Use **CycloneDX JSON** as the repository SBOM output format. The checked-in local tool is `CycloneDX` `6.2.0`, exposed as `dotnet-CycloneDX`. CycloneDX is selected because the configured .NET generator natively produces a dependency BOM from solution/project input and supports an explicit JSON output format. SPDX is not a second generated format in this build: it is an alternative SBOM standard, not an additional authoritative artifact. Produce SPDX only if a downstream compliance consumer explicitly requires it, with that consumer’s validation rules recorded alongside the export.

For every discovered solution, `Sbom` writes `bom.cdx.json` to a collision-safe directory beneath `artifacts/sbom/solutions/<relative-entry>/`. When no solution exists, it writes one BOM per discovered project beneath `artifacts/sbom/projects/<relative-entry>/`. The target owns this location and filename; callers do not override them.

When projects exist, the configured target executes this equivalent command for each discovered entry:

```sh
dotnet tool run dotnet-CycloneDX -- <solution-or-project> \
  --output artifacts/sbom/<solutions-or-projects>/<relative-entry> \
  --filename bom.cdx.json \
  --output-format Json
```

This command has not been run because this repository intentionally has no solution or project. The local tool restore that makes the command available was verified on 2026-07-27.

## Future per-package BOMs

For package releases, retain the project-level `artifacts/sbom/projects/<relative-entry>/bom.cdx.json` artifact and associate it with the package ID and version in release automation. Do not generate a catalog-wide BOM: catalog entries are not an installed dependency graph. A solution-level BOM in `artifacts/sbom/solutions/<relative-entry>/bom.cdx.json` may complement an application release, but it must not replace package-specific provenance.

## Sources

- [CycloneDX for .NET](https://github.com/CycloneDX/cyclonedx-dotnet) — Accessed 2026-07-27.
- [CycloneDX specification](https://github.com/CycloneDX/specification) — Accessed 2026-07-27.
- [dotnet tool restore](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-restore) — Accessed 2026-07-27.
- [dotnet outdated](https://github.com/dotnet-outdated/dotnet-outdated) — Accessed 2026-07-27.
