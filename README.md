# ModularBase

ModularBase centralizes the SDK, package, analyzer, source, build, and architecture policies for future `IX.Modularity.*` libraries. `IX.Modularity.slnx` currently contains three projects: the packable compiler-tooling package [`src/IX.Modularity.Analyzers`](src/IX.Modularity.Analyzers/IX.Modularity.Analyzers.csproj), its non-packable analyzer test project [`test/IX.Modularity.Analyzers.Tests`](test/IX.Modularity.Analyzers.Tests/IX.Modularity.Analyzers.Tests.csproj), and the non-packable architecture test project [`test/IX.Modularity.Architecture.Tests`](test/IX.Modularity.Architecture.Tests/IX.Modularity.Architecture.Tests.csproj). It contains no runtime or application library.

Start with the [documentation index](docs/README.md), which routes to every
documentation branch.

Useful direct links include the [architecture policy index](docs/architecture/README.md), the [package catalog](docs/packages/README.md), the [package-selection guidance](docs/package-guidance/README.md), the [illustrated recipes](docs/recipes/README.md), and the [analyzer index](docs/architecture/analyzer-index.md). The repository documents library policy and illustrative composition workflows; it deliberately does not include permanent sample applications or application projects.

## Current repository state

The repository pins the .NET SDK to `10.0.302` in [`global.json`](global.json), with roll-forward disabled and prerelease SDKs disallowed. Its shared C# policy targets `net10.0`, uses C# `14.0`, enables nullable reference types and implicit usings, and treats warnings as errors.

The `Analyzer` project produces packable compiler tooling; the `Test` project verifies that analyzer; and the `ArchitectureTest` project validates repository architecture and documentation rules. Build, restore, test, audit, outdated-package scanning, and SBOM generation operate on this populated solution. No project is a runtime or application library.

Each project declares one direct `IXModularityProjectRole` metadata value: `Analyzer`, `Test`, or `ArchitectureTest`. The role controls its allowed dependency direction and packability. See [architecture terminology](docs/architecture/terminology.md), [architectural rules](docs/architecture/architectural-rules.md), and [project structure](docs/architecture/project-structure.md) for the definitions and requirements.

## Repository commands

Use the repository `Makefile` as the public build interface:

```sh
make
make validate
make tool-restore
make build CONFIGURATION=Debug
make test CONFIGURATION=Debug
```

`make` validates the repository and restores its pinned local tools. The remaining targets are `restore`, `format`, `build`, `test`, `audit`, `outdated`, and `sbom`. Configuration is restricted to `Debug` or `Release` and defaults to `Release`.

[`eng/Build.proj`](eng/Build.proj) remains an internal orchestration detail. Invoke it through `make`, not directly.

`make test CONFIGURATION=Debug` is the authoritative ArchUnit bytecode check. `Release` remains a supported configuration for every target, including `make test CONFIGURATION=Release`.

See the architecture policies for the operational contract:

- [Package catalog and implementation guides](docs/packages/README.md)
- [Package selection and supply-chain guidance](docs/package-guidance/README.md)
- [Illustrated package recipes](docs/recipes/README.md)
- [Dependency policy](docs/architecture/dependency-policy.md)
- [Code quality policy](docs/architecture/code-quality-policy.md)
- [Build and SBOM](docs/architecture/build-and-sbom.md)
- [Architecture terminology](docs/architecture/terminology.md)
- [Architectural rules](docs/architecture/architectural-rules.md)
- [Project structure](docs/architecture/project-structure.md)
- [Package documentation schema](docs/architecture/package-documentation-schema.md)
- [Architecture policy index](docs/architecture/README.md)
- [Analyzer policy and diagnostics](docs/architecture/analyzer-index.md)
