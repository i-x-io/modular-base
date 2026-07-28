# ModularBase

ModularBase is the executable baseline for `IX.Modularity.*` libraries. Its
first package, `IX.Modularity`, provides reflection-free contracts for
explicitly composing modular .NET applications.

The baseline uses .NET 10, stable C# 14, SDK-style projects, and NUKE as the
single developer and CI build entry point. GitHub Actions provisions the
environment and invokes the same NUKE targets that run locally.

## What the baseline includes

- exact SDK selection through `global.json`;
- central package management and committed lock files;
- nullable reference types, warnings as errors, deterministic builds, public
  API analysis, banned APIs, and private analyzer dependencies;
- a real package and a 24-test Microsoft Testing Platform test suite;
- package, symbols-package, metadata, vulnerability, and CycloneDX SBOM
  validation;
- pinned `pre-commit`, Markdownlint CLI2, Gitleaks, Typos, JSON-schema,
  actionlint, zizmor, and Conventional Commits hooks;
- cross-platform CI, pull-request policy, Dependabot, scheduled maintenance,
  issue forms, and release automation; and
- MinVer-derived package versions and Release Please release pull requests.

Optional packages documented in the package catalog are not runtime
dependencies. The shipping package currently exposes only
`Microsoft.Extensions.DependencyInjection.Abstractions`.

## Get started

Install the exact SDK selected by `global.json`, then restore the repository
tools and run the authoritative validation:

```sh
dotnet tool restore
dotnet nuke Validate --configuration Release
```

The checked-in launchers are equivalent entry points:

```sh
./build.sh Validate
```

```powershell
./build.ps1 Validate
```

Run `dotnet nuke --help` for parameter completion and the complete target
graph. Common targets are:

| Target | Purpose |
| --- | --- |
| `Restore` | Restore tools, the solution, and the build project from locks. |
| `Format` | Verify solution formatting without modifying files. |
| `Compile` | Compile product and tests with the selected configuration. |
| `Test` | Run the MTP suite and reject zero discovered tests. |
| `Pack` | Create `.nupkg` and `.snupkg` artifacts. |
| `InspectPackages` | Check package contents, metadata, and runtime dependencies. |
| `Audit` | Audit direct and transitive packages for vulnerabilities. |
| `Sbom` | Generate and semantically validate a CycloneDX JSON SBOM. |
| `Validate` | Run the complete local and CI quality gate. |
| `UpdateLocks` | Regenerate locks after a reviewed dependency change. |
| `Outdated` | Fail when scheduled maintenance finds package updates. |
| `Publish` | Validate and publish an exact release tag in GitHub Actions. |

Generated output is written below `artifacts/`.

## Install the Git hooks

Install `pre-commit` 4.6.1 in an isolated environment, then let it provision
the pinned hook tools:

```sh
pipx install pre-commit==4.6.1
pre-commit install --install-hooks
```

The installed hook types are `pre-commit`, `commit-msg`, and `pre-push`.
Commit-time hooks provide fast staged-file feedback; pre-push invokes the full
NUKE `Validate` target. CI repeats the required checks because local hooks can
be skipped.

## Development and governance

- [Contribution guide](CONTRIBUTING.md)
- [Complete development workflow](docs/development-workflow.md)
- [GitHub repository setup and rulesets](docs/github-governance.md)
- [Current implementation report](docs/baseline-implementation-report.md)
- [Security policy](SECURITY.md)
- [Documentation index](docs/README.md)

The repository is licensed under the [MIT License](LICENSE).
