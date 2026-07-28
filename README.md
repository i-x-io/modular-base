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

## Developer checks

The repository uses [`pre-commit`](https://pre-commit.com/) **4.6.1** to install and run pinned checks in isolated environments. Install only the framework; `pre-commit` manages the Node, Go, and Python hook environments for Markdownlint CLI2, Gitleaks, Typos, conventional-pre-commit, and the standard file checks.

Prefer an isolated [`pipx`](https://pipx.pypa.io/) installation:

```sh
pipx install pre-commit==4.6.1
pre-commit --version
pre-commit install --install-hooks
```

If `pipx` is unavailable, install the same version in a dedicated Python virtual environment rather than the system Python environment. The repository config installs the `pre-commit`, `pre-push`, and `commit-msg` Git hook types.

Initialize or verify the complete repository with:

```sh
pre-commit validate-config
pre-commit run --all-files
pre-commit run --all-files --hook-stage pre-push
```

The commit stage performs structured-file syntax and formatting checks, portable filename and symlink checks, a submodule ban, secret detection, spelling, Markdown linting, and safe file cleanup. Markdownlint, Typos, JSON formatting, and basic cleanup hooks may modify files. When they do, review the diff, stage the accepted changes, and rerun the command. Gitleaks reports redacted findings without changing files.

Commit messages must follow [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/):

```text
<type>[optional scope][!]: <description>
```

Allowed types are `build`, `chore`, `ci`, `docs`, `feat`, `fix`, `perf`, `refactor`, `revert`, `style`, and `test`. Scopes are optional and are not centrally restricted; use a short stable area such as `packages`, `hooks`, or `analyzers`. Use `feat` for a release-relevant feature, `fix` for a release-relevant defect correction, and either `!` or a `BREAKING CHANGE:` footer for an incompatible change. For example:

```text
chore(hooks): enforce conventional commits
feat(dispatch)!: replace the handler contract
```

The validation intentionally permits Git-generated merge messages and `fixup!` commits for local autosquash workflows. It applies to new commits only; existing history is not rewritten.

The pre-push stage runs `dotnet format IX.Modularity.slnx --verify-no-changes --no-restore`. It is currently a no-op because the solution has no projects; it becomes meaningful after a project and its restored assets exist. It is not a substitute for build or test validation.

Run one check explicitly with `pre-commit run <hook-id> --all-files`. `SKIP=<hook-id>` is an exceptional local bypass and should be explained during review; do not use `--no-verify` as a normal workflow.

For a reviewed tooling upgrade, update the framework pin, run `pre-commit autoupdate`, inspect each upstream version and license change, rerun the commit and pre-push stages over all files, validate a representative commit message, and commit the tooling update separately. `pre-commit clean` removes all cached hook environments, while `pre-commit gc` removes only unused cached repositories.

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
