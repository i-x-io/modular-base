# Contributing

Thank you for improving ModularBase. The repository follows an issue-first,
short-lived-branch workflow and uses one NUKE validation graph locally and in
GitHub Actions.

## Before starting

1. For defects, features, dependency changes, and policy changes, open or
   select a GitHub issue. Do not put vulnerability details in a public issue;
   follow [SECURITY.md](SECURITY.md).
2. Create a branch from an up-to-date `main` using
   `<type>/<issue>-<description>`, for example `fix/123-module-ordering`.
3. Keep the change focused. Separate unrelated refactoring, dependency
   upgrades, and behavior changes.

Allowed branch and ordinary commit types are `build`, `chore`, `ci`, `docs`,
`feat`, `fix`, `perf`, `refactor`, `release`, `revert`, `style`, and `test`.
Descriptions use lowercase ASCII words separated by hyphens.

## Set up the repository

The required prerequisites are Git, the exact .NET SDK selected by
`global.json`, Python 3, and `pipx` or another isolated Python environment.

```sh
dotnet tool restore
pipx install pre-commit==4.6.1
pre-commit install --install-hooks
dotnet nuke CI --configuration Release
```

Do not install Markdownlint CLI2, Gitleaks, Typos, actionlint, or zizmor
globally for this repository. `pre-commit` creates isolated environments at the
revisions pinned in `.pre-commit-config.yaml`.

## Make a change

- Preserve nullable correctness and the public API contract.
- Add or update tests with behavior changes. A test run that discovers zero
  tests is a failure.
- Use the platform and existing dependencies before proposing another package.
- Keep runtime dependencies out of reusable contracts unless consumers
  genuinely need them.
- Do not add runtime scanning or broad reflection to module discovery. The
  current package deliberately uses explicit generic registration.
- Add a public API entry when a public symbol is introduced. The analyzer will
  identify the required declaration.
- Update package and workflow documentation in the same pull request when a
  behavior or operating requirement changes.

For a dependency change, update the central version or project reference, then
regenerate all affected lock files through the build:

```sh
dotnet nuke Restore --update-locks
dotnet nuke CI --configuration Release
```

Review the package license, repository ownership, transitives, advisories, and
generated lock diff. A central version is a catalog entry; it is not adopted
until a project adds a `PackageReference`.

## Commit and validate

Commit messages follow Conventional Commits 1.0.0:

```text
<type>[optional scope][!]: <description>
```

Examples:

```text
fix(registration): reject duplicate module dependencies
feat(api)!: replace the module descriptor contract
docs(workflow): explain release credentials
```

Use `feat` and `fix` only for release-relevant package changes. Add `!` or a
`BREAKING CHANGE:` footer for an incompatible change. The pull-request title
uses the same grammar because squash merge makes that title the commit on
`main`. A stable-release PR is the explicit exception and uses
`RELEASE: <description>` from a conforming
`release/<issue>-<description>` branch. Every ordinary merged PR publishes a
prerelease.

Before opening a pull request, run:

```sh
pre-commit run --all-files --show-diff-on-failure
dotnet nuke CI --configuration Release
```

The pre-push hook runs the second command automatically. If an auto-fixing hook
changes a file, inspect the diff, stage the intended fix, and rerun the hook.
`SKIP=<hook-id>` is an exceptional local bypass that must be explained in the
pull request; `--no-verify` is not a normal workflow.

## Open a pull request

The pull request must:

- target `main` from a conforming branch;
- have a Conventional Commit title or the exact stable marker
  `RELEASE: <description>`;
- include a closing reference such as `Closes #123`;
- describe behavior, risk, and validation evidence;
- contain no unresolved review conversations; and
- pass all required checks, including the merge-queue commit when the queue is
  enabled.

Use squash merge. Do not push directly to `main`, force-push protected refs, or
manually create release tags. NUKE derives the release plan from MinVer and the
merged pull-request title; GitHub automation owns the protected tag and the
reconciled release.

The API-only labeler applies branch-type and changed-area labels from protected
default-branch configuration. Do not add workflow steps that check out or run
pull-request code under `pull_request_target`.

The complete process, target graph, package policy, and exceptional workflows
are documented in the [development workflow](docs/development-workflow.md).
