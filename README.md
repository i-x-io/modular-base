# ModularBase

ModularBase is a baseline for future `IX.Modularity.*` libraries. It centralizes the .NET SDK version, common build and analyzer settings, package versions, NuGet source policy, package guidance, and local dependency-review tools.

[`IX.Modularity.slnx`](IX.Modularity.slnx) is intentionally empty. The repository does not currently contain source projects, test projects, custom analyzers, architecture tests, runtime libraries, or applications.

Start with the [documentation index](docs/README.md). The main documentation branches are the [package catalog](docs/packages/README.md), [package-selection guidance](docs/package-guidance/README.md), and [composition recipes](docs/recipes/README.md).

## Shared baseline

The repository provides:

- an exact .NET SDK requirement of `10.0.302`, with roll-forward and prerelease SDKs disabled;
- shared .NET 10 and C# 14 build settings, nullable reference types, deterministic output, and warnings as errors;
- central package versions and private, build-only third-party analyzers;
- NuGet.org-only package source mapping and vulnerability auditing;
- a banned-symbol policy supplied to projects through `BannedSymbols.txt`;
- package selection and integration documentation; and
- pinned local CycloneDX and outdated-package tools.

Future projects inherit the applicable settings from `Directory.Build.props`, `Directory.Packages.props`, `ModularBase.globalconfig`, and `.editorconfig`. Packable projects must declare and include their own package README rather than relying on a repository-wide package-readme default.

## Adding and validating a project

Install exactly .NET SDK `10.0.302`, then create a project and add it to the solution. For example:

```sh
dotnet new classlib --name IX.Modularity.Example --output src/IX.Modularity.Example
dotnet sln IX.Modularity.slnx add src/IX.Modularity.Example/IX.Modularity.Example.csproj
```

The shared configuration enables NuGet lock files. Generate a new project's initial `packages.lock.json` with an unlocked restore, inspect it, and commit it with the project:

```sh
dotnet restore IX.Modularity.slnx -p:RestoreLockedMode=false
```

After the lock file exists, use the standard .NET commands directly:

```sh
dotnet tool restore
dotnet restore IX.Modularity.slnx --locked-mode
dotnet format IX.Modularity.slnx --verify-no-changes --no-restore
dotnet build IX.Modularity.slnx --configuration Release --no-restore
dotnet test IX.Modularity.slnx --configuration Release --no-build
dotnet pack IX.Modularity.slnx --configuration Release --no-build
dotnet outdated IX.Modularity.slnx
dotnet CycloneDX IX.Modularity.slnx
```

NuGet auditing is enabled during restore. Review audit findings and generated dependency reports rather than treating successful command completion alone as evidence of package safety.

Because the solution is currently empty, restore, build, and format commands may complete without evaluating meaningful product code, while test may have no runnable configuration. Those results are not repository validation until at least one project is present.
