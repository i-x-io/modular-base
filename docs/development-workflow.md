# Development workflow

This is the authoritative contributor workflow for ModularBase. It covers
issues, branches, commits, local validation, pull requests, dependency changes,
releases, and exceptional maintenance.

## Operating model

The repository has three deliberately separate layers:

| Layer | Responsibility |
| --- | --- |
| `pre-commit` | Fast, pinned feedback for staged files, commit messages, and pre-push validation. |
| NUKE | The provider-neutral dependency graph for restore, build, test, pack, audit, SBOM, and publish. |
| GitHub Actions | Provision runners, run repository prechecks, invoke NUKE, retain artifacts, and apply GitHub policy. |

NUKE is the canonical build API. Workflow YAML must not grow a second sequence
of restore/build/test/pack commands. A CI-only check may remain in YAML when it
depends on GitHub event metadata or is naturally a GitHub Action, such as
dependency review or pull-request title validation.

## One-time setup

Prerequisites:

- Git;
- .NET SDK `10.0.302` exactly, as selected by `global.json`;
- Python 3 and preferably `pipx`; and
- a shell on Unix or PowerShell on Windows.

From the repository root:

```sh
dotnet tool restore
pipx install pre-commit==4.6.1
pre-commit install --install-hooks
dotnet nuke Validate --configuration Release
```

`pre-commit install` installs the `pre-commit`, `commit-msg`, and `pre-push`
hook scripts. The pinned hook repositories then provision their own Python,
Node, Go, or binary environments. Run `pre-commit clean` only when rebuilding
all hook environments is intentional; use `pre-commit gc` for unused caches.

## Issue-first changes

Create an issue before starting work except for a trivial typo or an emergency
security fix being coordinated privately. The issue records intent and gives
the branch and pull request a stable identifier.

Choose one change type:

| Type | Use |
| --- | --- |
| `feat` | New release-relevant package behavior. |
| `fix` | Release-relevant defect correction. |
| `perf` | Measurable performance improvement. |
| `refactor` | Behavior-preserving production-code restructuring. |
| `test` | Test-only work. |
| `docs` | Documentation-only work. |
| `build` | NUKE, MSBuild, packaging, or build-tool changes. |
| `ci` | GitHub Actions and repository automation. |
| `chore` | Maintenance that does not fit another type. |
| `style` | Formatting-only changes. |
| `revert` | Revert of a prior change. |

## Branches

Create a short-lived branch from current `main`:

```sh
git fetch upstream
git switch main
git pull --ff-only upstream main
git switch -c feat/123-explicit-module-dependencies
```

The enforced form is:

```text
<type>/<issue>-<description>
```

The issue is a positive integer. The description is lowercase ASCII,
hyphen-separated, and concise. Examples are `fix/42-registration-order`,
`docs/87-release-setup`, and `ci/106-harden-actions`. Release Please and
Dependabot branches are bot-exempt.

Do not use long-lived `develop`, release, environment, or personal branches.
`main` is the integration branch and must remain releasable. Use feature flags
or keep incomplete work out of `main`; do not create a parallel integration
branch to hide incomplete work.

## Local feedback and hooks

The commit stage includes:

- structured JSON, TOML, YAML, XML, `.slnx`, and project-file checks;
- filename, symlink, executable, merge-marker, submodule, large-file, byte
  order mark, whitespace, and line-ending checks;
- Markdownlint CLI2 safe fixes;
- Typos documentation, filename, and identifier fixes;
- Gitleaks staged-secret detection with redacted output;
- GitHub workflow, Dependabot, issue-form, and issue-config schema checks;
- actionlint semantic workflow checks; and
- zizmor GitHub Actions security analysis.

The `commit-msg` stage validates Conventional Commits. The `pre-push` stage
runs `dotnet nuke Validate`, which is intentionally slower and authoritative.
CI also runs a manual full-history Gitleaks hook because staged scanning cannot
detect secrets introduced in older commits.

Run individual checks during editing:

```sh
pre-commit run markdownlint-cli2 --all-files
pre-commit run typos --all-files
pre-commit run actionlint --all-files
pre-commit run zizmor --all-files
pre-commit run gitleaks-repository --all-files --hook-stage manual
```

Markdownlint, Typos, JSON formatting, and basic hygiene hooks can edit files.
Gitleaks, schema validation, actionlint, and zizmor only report findings.
NuGet-generated `packages.lock.json` files are syntax-checked but excluded from
the generic JSON autoformatter; only `UpdateLocks` should rewrite them.

## NUKE target graph

Run `dotnet nuke --help` after adding a target or parameter. This regenerates
`.nuke/build.schema.json`, which gives IDE and shell tooling the target and
parameter choices. `.nuke/parameters.json` supplies the default `Release`
configuration.

The build uses NUKE's strongly typed integrations rather than duplicating path
strings:

- `[Solution(GenerateProjects = true)]` loads the `.slnx` file and generates
  typed product and test project accessors;
- `[GitRepository]` supplies exact current tags for release verification;
- `[Parameter]` exposes a typed `Debug` or `Release` configuration;
- `[Secret]` protects the short-lived package token from normal logging;
- `RootDirectory`, `BuildProjectFile`, and `IsServerBuild` come from
  `NukeBuild`; and
- `GitHubActions.Instance` supplies strongly typed server context during
  publishing.

The build project remains outside `IX.Modularity.slnx`. This is intentional:
the solution represents product and test code, while NUKE restores and compiles
its conventional `build/_build.csproj` independently. Adding `_build.csproj`
under a `/build/` solution folder also creates a name collision in NUKE's
strongly typed solution generator.

| Target | Dependencies and guarantees |
| --- | --- |
| `Clean` | Recreates `artifacts/`. |
| `UpdateLocks` | Restores tools, solution, and build project with force evaluation and unlocked mode. |
| `Restore` | Asserts repository shape and restores both project graphs in locked mode. |
| `Format` | Depends on `Restore`; verifies solution formatting without edits. The build project enforces style while it compiles. |
| `Compile` | Depends on `Restore`; uses NUKE's typed `DotNetBuild` settings. |
| `Test` | Depends on `Compile`; uses typed `DotNetTest`, MTP reporting, and `--minimum-expected-tests 1`. |
| `Pack` | Depends on `Compile`; creates clean package output with typed `DotNetPack`. |
| `InspectPackages` | Depends on `Pack`; checks one package and symbols package, required contents, repository URL, and exact runtime dependency set. |
| `Audit` | Depends on `Restore`; audits both the solution and the private build graph. |
| `Sbom` | Depends on `Restore`; invokes the local CycloneDX tool and rejects an empty component list. |
| `Validate` | Aggregates `Format`, `Test`, `InspectPackages`, `Audit`, and `Sbom`. |
| `Outdated` | Scheduled maintenance target; fails when updates exist so GitHub opens a visible failed run. |
| `Publish` | Requires server context and a secret token, reruns `Validate`, checks the exact SemVer tag against the package, then publishes once. |

NUKE 10.1 has typed wrappers for the standard SDK operations used here. The
generic `DotNet` runner is retained only for installed tools and the .NET 10
`dotnet package list` command, for which this NUKE version has no suitable
typed wrapper.

## Commit messages

Use Conventional Commits:

```text
<type>[optional scope][!]: <description>

[optional body]

[optional footer(s)]
```

Keep the subject imperative, lowercase after the colon, and free of a trailing
period. A scope names a stable area such as `api`, `build`, `hooks`, `packages`,
or `workflow`; it is not mandatory. Examples:

```text
feat(api): add explicit module dependencies
fix(build): verify symbols package contents
chore(deps): update Microsoft.Extensions packages
```

Release Please maps `fix` to a SemVer patch and `feat` to a SemVer minor. Any
allowed type marked with `!`, or carrying a `BREAKING CHANGE:` footer, produces
a breaking release signal. Squash-merge pull requests, so the validated pull
request title becomes the final conventional commit on `main`.

Git-generated merge commits and `fixup!` commits remain permitted locally for
rebasing and autosquash, but should not survive the squash merge.

## Pull requests and merge queue

Use the pull-request template and include:

- a concise behavior and motivation summary;
- a closing issue reference such as `Closes #123`;
- risks, compatibility effects, and release impact;
- the exact validation performed; and
- package/license notes for dependency changes.

Draft pull requests are encouraged for early design feedback, but they cannot
enter the merge queue. Resolve every review conversation. Do not approve your
own change when another maintainer is available.

The CI workflow listens to `pull_request` and `merge_group`. A merge queue
therefore validates the combined commit that would reach `main`, not only the
individual pull-request head. Use squash as the queue merge method and delete
the head branch after merge.

## Dependency updates

Dependabot checks NuGet, GitHub Actions, pre-commit hooks, and the .NET SDK each
week. Its groups reduce compatible updates into reviewable batches and a
seven-day cooldown avoids adopting a brand-new release immediately.

For any dependency change:

1. Establish a demonstrated capability or maintenance need.
2. Confirm the canonical upstream identity and current license.
3. Review release notes, ownership changes, transitives, advisories, and
   external-service requirements.
4. Update the central pin or manifest and regenerate locks with `UpdateLocks`.
5. Run `Validate`, inspect the package, and review the SBOM diff.
6. Update the package catalog and supply-chain record when a cataloged package
   changes.

Prefer the BCL and shared framework. Default acceptable licenses are MIT,
Apache-2.0, BSD-2-Clause, BSD-3-Clause, ISC, and PostgreSQL-style permissive
terms. Reciprocal, source-available, or commercial terms require an ADR, legal
review where appropriate, a budget/renewal owner, key management, and an exit
plan. Popularity alone is not an adoption reason.

No mediator or dispatcher package is part of the baseline. Use direct service
or handler injection first. A host that already adopts FastEndpoints should
evaluate its built-in command/event support before adding another abstraction.
Framework-neutral mediator or durable messaging choices require a separate ADR
and proof of the actual delivery guarantees.

## Release workflow

MinVer calculates package versions from tags matching
`IX.Modularity-v<semver>`. The project config is authoritative because NUKE's
generic MinVer injection cannot express this package-specific tag prefix.

Release Please reads conventional commits on `main` and maintains a release
pull request that updates `src/IX.Modularity/version.txt` and
`src/IX.Modularity/CHANGELOG.md`. Merging that pull request creates the GitHub
release and exact component tag. The release workflow then checks out that tag,
runs `Publish`, compares the tag version with the packed package version, and
publishes to `https://nuget.pkg.github.com/i-x-io/index.json`.

Do not manually edit the manifest version, create a release tag, or call
`Publish` from a workstation. Publishing requires GitHub Actions server context
and a short-lived token. The GitHub App setup and ruleset bypass are documented
in [GitHub governance](github-governance.md).

After the first stable package is published, set and maintain
`PackageValidationBaselineVersion` so SDK package validation checks binary and
API compatibility against an intentional released baseline.

## Exceptional and maintenance operations

- Use `dotnet nuke UpdateLocks` only with an intentional dependency change.
- Use `pre-commit autoupdate` only in a dedicated tooling pull request; verify
  each revision and license before accepting it.
- Run `dotnet nuke Outdated` locally when reviewing scheduled freshness
  failures; the target is expected to fail while updates exist.
- A hook bypass does not waive CI. Explain it and fix the underlying portability
  or false-positive issue in the same pull request when possible.
- Reverts use a `revert:` pull request and still pass the complete merge-queue
  validation.
- Security fixes follow the private process in `SECURITY.md`; public history
  should not disclose an unpatched vulnerability.
