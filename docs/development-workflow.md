# Development workflow

This is the authoritative contributor workflow for ModularBase. It covers
issues, branches, commits, local validation, pull requests, dependency changes,
releases, and exceptional maintenance.

## Operating model

The repository has three deliberately separate layers:

| Layer | Responsibility |
| --- | --- |
| `pre-commit` | Pinned repository-tool environment for local hooks and NUKE repository validation. |
| NUKE | The authoritative C# application for repository policy, restore, build, test, pack, audit, SBOM, release evidence, GitHub release reconciliation, and package publication. |
| GitHub Actions | Provision the supported Ubuntu runner and tools, invoke NUKE, perform Dependency Review and attestations, retain artifacts, and coordinate auto-merge. |

NUKE is the canonical build API. Workflow YAML must not grow a second sequence
of provider-neutral validation or release-classification commands. GitHub event
permissions, caches, attestations, Dependency Review, artifact retention, and
auto-merge remain workflow concerns. NUKE consumes GitHub event payloads and
uses the GitHub API through a tested C# boundary. Ubuntu is the sole supported
CI environment.

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
dotnet nuke CI --configuration Release
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
| `release` | Preparation for an explicit stable release. |
| `style` | Formatting-only changes. |
| `revert` | Revert of a prior change. |

## Branches

Create a short-lived branch from current `main`:

```sh
git fetch origin
git switch main
git pull --ff-only origin main
git switch -c feat/123-explicit-module-dependencies
```

The enforced form is:

```text
<type>/<issue>-<description>
```

The issue is a positive integer. The description is lowercase ASCII,
hyphen-separated, and concise. Examples are `fix/42-registration-order`,
`docs/87-release-setup`, `ci/106-harden-actions`, and
`release/107-stable-package`. Dependabot branches are bot-exempt.

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
runs `dotnet nuke CI`, which is intentionally slower and authoritative.
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
the generic JSON autoformatter; only `Restore --update-locks` should rewrite
them.

## NUKE target graph

Run `dotnet nuke --help` after adding a target or parameter. This regenerates
`.nuke/build.schema.json`, which gives IDE and shell tooling the target and
parameter choices. `.nuke/parameters.json` supplies the default `Release`
configuration.

The build uses NUKE's strongly typed integrations and typed configuration
objects rather than duplicating path strings or product project names:

- `[Solution]` loads the `.slnx` as a repository graph without generating
  hardcoded project accessors;
- `[GitRepository]` supplies the endpoint, owner, repository, URL, and commit;
- `[Parameter]` exposes configuration, intentional lock regeneration, and the
  two credentials required only by `Publish`;
- `[Secret]` redacts the short-lived GitHub/package and release credentials;
- `BuildPaths`, `RepositoryModel`, `RepositoryIdentity`, `BuildPolicy`, and
  `ToolchainVersions` centralize paths, graph roles, repository-derived values,
  immutable policy, and versions;
- `RootDirectory` and `IsServerBuild` come from `NukeBuild`; and
- `GitHubActions.Instance` supplies strongly typed server context during
  publishing.

The release-manifest and release-plan schema versions live on their models.
The stable-title prefix, tag prefix, minimum test count, and approved pull
request types are source-controlled policy objects rather than mutable per-run
parameters. Changing one modifies a reviewed build or release contract.

The build project remains outside `IX.Modularity.slnx`. This is intentional:
the solution represents product and test code, while NUKE restores and compiles
its conventional `build/_build.csproj` independently. NUKE treats the solution
as one graph and discovers every package through evaluated `IsPackable`
metadata; product and test project names are never hardcoded in the build.

| Target | Dependencies and guarantees |
| --- | --- |
| `Restore` | Validates topology and restores tools, solution, NUKE, and build-test graphs in locked mode. `--update-locks` switches the same operation into explicit lock regeneration. |
| `Test` | Depends on `Restore`, compiles all managed compilation inputs, runs all product and build-infrastructure tests, and enforces the configured nonzero minimum. |
| `CI` | Depends on `Test`; reads and validates pull-request events when present, checks formatting, packs and inspects every packable project, enforces one lockstep version, audits every dependency graph, generates and validates the SBOM, runs repository hooks, and scans complete Git history. |
| `Publish` | Depends on both `CI` and `Test`; requires a GitHub push to `main`, resolves exactly one merged pull request for the commit, classifies the release, creates the local tag, repacks and validates every package, emits schema-v2 evidence, creates or verifies the remote tag, publishes all packages, and reconciles the GitHub release and assets. |

NUKE 10.1 has typed wrappers for the standard SDK operations used here. The
structured process abstraction is retained only for installed tools, Git, and
the .NET 10 `dotnet package list` command, for which this NUKE version has no
suitable typed wrapper. It uses an argument list rather than shell-composed
command strings and applies timeouts, exit-code checks, and secret redaction.

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

Squash-merge pull requests, so the validated pull-request title becomes the
final commit subject on `main`. Ordinary pull-request titles use Conventional
Commits. A stable release is the one intentional exception and must use:

```text
RELEASE: <non-empty description>
```

The corresponding branch still follows policy, for example
`release/123-publish-stable-package`, and the body still closes an issue.

Git-generated merge commits and `fixup!` commits remain permitted locally for
rebasing and autosquash, but should not survive the squash merge.

## Pull requests and automatic merge

Use the pull-request template and include:

- a concise behavior and motivation summary;
- a closing issue reference such as `Closes #123`;
- risks, compatibility effects, and release impact;
- the exact validation performed; and
- package/license notes for dependency changes.

Draft pull requests are encouraged for early design feedback, but automation
does not merge them. Resolve every review conversation. Do not approve your own
change when another maintainer is available.

The focused `Pull request` workflow has a `Validate` job and a
pull-request-only `Dependency review` job, followed by one required
`Pull request gate`. Pull-request metadata policy is part of the authoritative
NUKE `CI` target rather than a parallel YAML job. The workflow also listens to
`merge_group`, so the merge queue validates the combined commit that would
reach `main`. Dependency Review is skipped for merge groups; the complete NUKE
validation reruns and the aggregate gate accounts for the event-specific skip.

After the workflow succeeds, the trusted `Auto-merge` workflow resolves the
same pull request and invokes GitHub's merge operation with the exact checked
head SHA. It enables auto-merge while reviews or other requirements are
pending, or enters the merge queue when one is required. It does not check out,
download, cache, or execute pull-request-controlled code or artifacts.

The separate `Pull request labels` workflow classifies branch type and changed
areas through the GitHub API. It intentionally uses `pull_request_target` but
never checks out or executes pull-request content. Labels are synchronized on
open, reopen, update, and ready-for-review events. A maintainer can rerun it for
an existing pull request through `workflow_dispatch` with the pull-request
number.

## Dependency updates

Dependabot checks NuGet, GitHub Actions, pre-commit hooks, and the .NET SDK each
week. Its groups reduce compatible updates into reviewable batches and a
seven-day cooldown avoids adopting a brand-new release immediately.

For any dependency change:

1. Establish a demonstrated capability or maintenance need.
2. Confirm the canonical upstream identity and current license.
3. Review release notes, ownership changes, transitives, advisories, and
   external-service requirements.
4. Update the central pin or manifest and regenerate locks with
   `Restore --update-locks`.
5. Run `CI`, inspect the package, and review the SBOM diff.
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

MinVer calculates one lockstep package version from immutable repository tags
matching `v<semver>`. Every push to `main` must resolve to exactly one
merged pull request; a direct push is a release failure and should already be
prevented by the branch ruleset.

Release classification occurs inside `Publish`:

- an ordinary pull-request title keeps MinVer's exact candidate, such as
  `0.1.1-preview.0.3`, and creates a GitHub prerelease;
- a title beginning with the exact uppercase marker `RELEASE:` removes the
  prerelease portion from that candidate, such as `0.1.1`, and creates the
  latest stable GitHub release.

The release workflow invokes `Publish` once. `Publish` reruns authoritative
validation, resolves the merged pull request through GitHub, writes the typed
multi-package plan, creates the local tag, repacks under it, validates
tag/package identity, generates checksummed evidence, creates or verifies the
remote tag, derives the GitHub Packages endpoint from repository identity,
publishes every package, and reconciles release notes and assets. The workflow
then performs provenance and SBOM attestations and retains the same evidence as
an Actions artifact.

Do not create a release tag or call `Publish` from a workstation. Publishing
requires GitHub Actions server context and configured credentials. The release
credential creates protected tags and releases; the separate job-scoped token
publishes the package. Provisioning, rotation, minimum permissions, and the tag
ruleset are documented in [GitHub governance](github-governance.md).

The initial stable package `v0.1.0` is now available as a potential SDK package
validation baseline. Before setting `PackageValidationBaselineVersion`, define
how CI retrieves that GitHub Packages version with a least-privilege credential
and how an intentional breaking release advances the baseline.

## Exceptional and maintenance operations

- Use `dotnet nuke Restore --update-locks` only with an intentional dependency
  change.
- Use `pre-commit autoupdate` only in a dedicated tooling pull request; verify
  each revision and license before accepting it.
- A hook bypass does not waive CI. Explain it and fix the underlying portability
  or false-positive issue in the same pull request when possible.
- Reverts use a `revert:` pull request and still pass the complete merge-queue
  validation.
- Security fixes follow the private process in `SECURITY.md`; public history
  should not disclose an unpatched vulnerability.
