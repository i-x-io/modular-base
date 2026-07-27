# ModularBase

ModularBase is a **configuration-only .NET repository**. It centralizes the SDK, package, analyzer, source, and build policies that future package projects consume. The empty `IX.Modularity.slnx` solution and retained `src/`, `test/`, and `assets/` directories provide the initial layout; no source or test projects exist yet.

## Current repository state

The repository pins the .NET SDK to `10.0.302` in [`global.json`](global.json), with roll-forward disabled and prerelease SDKs disallowed. Its shared C# policy targets `net10.0`, uses C# `14.0`, enables nullable reference types and implicit usings, and treats warnings as errors.

Because the solution contains no projects, this repository cannot compile, test, restore project dependencies, audit a project graph, generate a project SBOM, or pack a package. Add a project only when a package is ready to be implemented; do not add placeholder projects merely to make build commands appear green.

## Repository commands

Use the repository `Makefile` as the public build interface:

```sh
make
make validate
make tool-restore
make build CONFIGURATION=Debug
```

`make` validates the repository and restores its pinned local tools. The remaining targets are `restore`, `format`, `build`, `test`, `audit`, `outdated`, and `sbom`. Configuration is restricted to `Debug` or `Release` and defaults to `Release`.

[`eng/Build.proj`](eng/Build.proj) remains an internal orchestration detail. Invoke it through `make`, not directly.

`make`, `make validate`, and all project-dependent targets recognize that the solution is empty. Project-dependent targets report an intentional skip. This validates policy configuration and orchestration only; it does not compile, test, restore, audit, pack, or generate an SBOM until a real project is added.

See the architecture policies for the operational contract:

- [Package catalog and implementation guides](docs/packages/README.md)
- [Dependency policy](docs/architecture/dependency-policy.md)
- [Code quality policy](docs/architecture/code-quality-policy.md)
- [Build and SBOM](docs/architecture/build-and-sbom.md)
- [Package documentation schema](docs/architecture/package-documentation-schema.md)
