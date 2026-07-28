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
- a real package, a 24-test Microsoft Testing Platform product suite, and 37
  tests for the enterprise build infrastructure;
- package, symbols-package, metadata, vulnerability, and CycloneDX SBOM
  validation;
- pinned `pre-commit`, Markdownlint CLI2, Gitleaks, Typos, JSON-schema,
  actionlint, zizmor, and Conventional Commits hooks;
- focused pull-request validation, API-only labeling, trusted auto-merge,
  Dependabot, issue forms, and release automation; and
- MinVer-derived prereleases for every merged pull request plus explicit stable
  releases selected by a `RELEASE:` pull-request title.

Optional packages documented in the package catalog are not runtime
dependencies. The shipping package currently exposes only
`Microsoft.Extensions.DependencyInjection.Abstractions`.

## Get started

Install the exact SDK selected by `global.json`, then restore the repository
tools and run the authoritative validation:

```sh
dotnet tool restore
dotnet nuke CI --configuration Release
```

The checked-in launchers are equivalent entry points:

```sh
./build.sh CI
```

```powershell
./build.ps1 CI
```

Run `dotnet nuke --help` for parameter completion and the complete target
graph. The build intentionally exposes only outcome-oriented targets:

| Target | Purpose |
| --- | --- |
| `Restore` | Restore tools and every managed project graph; pass `--update-locks` only for an intentional lock refresh. |
| `Test` | Compile the repository, run every MTP suite, and reject missing tests. |
| `CI` | Run `Test`, PR policy, formatting, package inspection, dependency audit, SBOM generation, repository checks, and history secret scanning. |
| `Publish` | Run `CI` and `Test`, resolve the merged PR, plan and create the exact repository tag, repack all packages, publish them, and reconcile release evidence and assets. |

Generated product output is written below `artifacts/`. The running NUKE host
uses `build/bin` and `build/obj`, keeping it outside cleanable product output.

The production workflow set is intentionally limited to `Pull request`,
`Pull request labels`, `Auto-merge`, and `Release`. Provider-neutral build and
release logic belongs in NUKE rather than additional workflow YAML.

## Install the Git hooks

Install `pre-commit` 4.6.1 in an isolated environment, then let it provision
the pinned hook tools:

```sh
pipx install pre-commit==4.6.1
pre-commit install --install-hooks
```

The installed hook types are `pre-commit`, `commit-msg`, and `pre-push`.
Commit-time hooks provide fast staged-file feedback; pre-push invokes the full
NUKE `CI` target. GitHub repeats the required checks because local hooks can
be skipped.

## Development and governance

- [Contribution guide](CONTRIBUTING.md)
- [Complete development workflow](docs/development-workflow.md)
- [GitHub repository setup and rulesets](docs/github-governance.md)
- [Current implementation report](docs/baseline-implementation-report.md)
- [Security policy](SECURITY.md)
- [Documentation index](docs/README.md)

The repository is licensed under the [MIT License](LICENSE).
