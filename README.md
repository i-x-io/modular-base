# ModularBase

ModularBase centralizes the SDK, package, analyzer, source, build, and architecture policies for future `IX.Modularity.*` libraries. `IX.Modularity.slnx` contains one non-packable `ArchitectureTest` project, [`test/IX.Modularity.Architecture.Tests`](test/IX.Modularity.Architecture.Tests); `src/` contains no production projects.

## Current repository state

The repository pins the .NET SDK to `10.0.302` in [`global.json`](global.json), with roll-forward disabled and prerelease SDKs disallowed. Its shared C# policy targets `net10.0`, uses C# `14.0`, enables nullable reference types and implicit usings, and treats warnings as errors.

The architecture-test project validates repository architectural rules without creating a production package. Build, restore, test, audit, outdated-package scanning, and SBOM generation operate on the populated solution. Add production projects only when a package is ready to be implemented; do not add placeholder projects merely to make build commands appear green.

Each project declares one direct `IXModularityProjectRole` metadata value. The current project declares `ArchitectureTest`; the role controls its allowed dependency direction and non-packable status. See [architecture terminology](docs/architecture/terminology.md), [architectural rules](docs/architecture/architectural-rules.md), and [project structure](docs/architecture/project-structure.md) for the definitions and requirements.

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
- [Dependency policy](docs/architecture/dependency-policy.md)
- [Code quality policy](docs/architecture/code-quality-policy.md)
- [Build and SBOM](docs/architecture/build-and-sbom.md)
- [Architecture terminology](docs/architecture/terminology.md)
- [Architectural rules](docs/architecture/architectural-rules.md)
- [Project structure](docs/architecture/project-structure.md)
- [Package documentation schema](docs/architecture/package-documentation-schema.md)
