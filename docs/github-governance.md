# GitHub governance and repository setup

The repository files define workflows and templates, but GitHub repository
settings, rulesets, Apps, and security features live outside Git. Apply this
checklist after the initial `main` branch is pushed. Revisit it whenever a
required job name or release credential changes.

## Repository settings

Use these general settings:

- visibility: public;
- default branch: `main`;
- issues and private vulnerability reporting: enabled;
- wiki and projects: disabled unless an owner and use case exist;
- squash merge: enabled and selected as the only merge method;
- merge commits and rebase merge: disabled;
- automatically delete head branches: enabled;
- always suggest updating pull-request branches: enabled; and
- Actions workflow permissions: read-only by default.

Use the pull-request title as the default squash commit subject. The title is
already checked as a Conventional Commit and becomes the release signal on
`main`.

## Actions policy

Enable GitHub Actions and require every action to be pinned to a full-length
commit SHA. The checked-in workflows already satisfy that rule. If the
organization uses an action allowlist, permit only:

- `actions/*`;
- `amannn/action-semantic-pull-request`;
- `googleapis/release-please-action`; and
- `lycheeverse/lychee-action`.

Keep the repository default `GITHUB_TOKEN` permission at read-only and allow
workflows to request narrower write scopes explicitly. Do not allow Actions to
create or approve pull requests with the default token; release automation uses
a dedicated GitHub App so its pull request triggers normal CI.

Fork pull requests must never receive repository secrets. Current pull-request
workflows require only read permissions and do not use `pull_request_target`.
Do not introduce `pull_request_target` plus checkout or execution of untrusted
pull-request code.

## Main branch ruleset

Create an active branch ruleset named `main` targeting the default branch.
Configure:

- restrict deletion;
- block force pushes;
- require linear history;
- require a pull request before merging;
- require zero approvals initially, raising this to one when at least two
  maintainers can review without blocking the repository;
- dismiss stale approvals when approval requirements are enabled;
- require all review conversations to be resolved;
- allow only squash as the merge type;
- require the merge queue, using squash and only non-failing pull requests; and
- require status checks, without separately requiring the branch to be current
  because the merge queue validates the combined head.

After each required workflow has run at least once, select these exact check
names with GitHub Actions as the expected source:

- `Pre-commit`;
- `Validate (ubuntu-latest)`;
- `Validate (windows-latest)`;
- `Validate (macos-latest)`;
- `Dependency review`;
- `Conventional pull request`; and
- `Branch and issue policy`.

Both CI and pull-request policy workflows listen for `merge_group`. Jobs that
need pull-request metadata are emitted as successful skipped checks for merge
groups, while the complete NUKE validation reruns against the merge-queue
commit. Do not add a required workflow that uses only path filters or lacks a
`merge_group` trigger; its absent status can block the queue indefinitely.

Do not grant administrators a blanket bypass. If emergency bypass is retained,
limit it to repository administrators in pull-request-only mode and require a
follow-up issue. Normal release automation does not need to bypass `main`.

## Release tag ruleset

Create an active tag ruleset named `IX.Modularity release tags` targeting:

```text
IX.Modularity-v*
```

Restrict creation, update, and deletion, and block force updates. Add only the
dedicated release GitHub App as an always-allowed bypass actor. People,
administrator roles, the default Actions token, and Dependabot should not
create or rewrite release tags.

## Release GitHub App

Create or reuse an organization-owned GitHub App dedicated to release
automation. Install it only on `i-x-io/modular-base` and grant repository
permissions:

- Contents: read and write;
- Issues: read and write; and
- Pull requests: read and write.

It does not need administration, Actions, checks, members, secrets, or package
permissions. Add the App as the release-tag ruleset bypass actor.

Configure repository Actions values:

| Kind | Name | Value |
| --- | --- | --- |
| Variable | `RELEASE_APP_ID` | The numeric GitHub App ID. |
| Secret | `RELEASE_APP_PRIVATE_KEY` | The current PEM private key. |

The workflow exchanges these credentials for a short-lived installation token
and explicitly limits the token to this repository and the three permissions
above. Rotate the private key according to the organization's credential
policy and remove the old key after a successful release run.

The built-in job token publishes the package because the release job requests
`packages: write`. It is passed to NUKE only for the publish step and is masked
as a secret parameter.

## GitHub Packages

Packages are published to the organization NuGet endpoint:

```text
https://nuget.pkg.github.com/i-x-io/index.json
```

The package links back to this repository. Grant package access to consuming
repositories explicitly rather than making a broad organization token. GitHub
Actions consumers should request `packages: read`; developer and external
NuGet clients normally use a least-privilege personal access token with
`read:packages`. Never commit a token to `NuGet.Config`.

After the first publish, verify package visibility, repository linkage,
download instructions, symbols artifact retention, and that a clean consumer
can restore only with the documented permissions.

## Security features

Enable:

- dependency graph;
- Dependabot alerts and security updates;
- secret scanning and push protection;
- private vulnerability reporting; and
- automatic security advisories for supported ecosystems.

The repository already adds dependency review, package audit, Gitleaks, action
SHA pinning, actionlint, zizmor, and an SBOM. Add CodeQL default setup only
after confirming the generated job name and merge-queue behavior, then make it
required. Coverage gates, OpenSSF Scorecard, artifact attestations, and package
signing are useful later hardening steps, but are intentionally not silently
treated as complete in this baseline.

## Teams, ownership, and labels

The `i-x-io/maintainers` team owns the repository-wide policy, build,
workflow, and public API surfaces through `.github/CODEOWNERS`:

```text
* @i-x-io/maintainers
/.github/ @i-x-io/maintainers
/build/ @i-x-io/maintainers
/src/IX.Modularity/PublicAPI.*.txt @i-x-io/maintainers
```

Keep mandatory code-owner review disabled while the team has only one active
human member, because self-approval cannot provide independent review. Enable
one required code-owner approval as soon as a second active maintainer joins,
without making release automation unable to maintain its pull request.

Create and consistently use at least these labels: `bug`, `enhancement`,
`triage`, `security`, `dependencies`, `dotnet`, `github-actions`, `pre-commit`,
`breaking-change`, `release`, `documentation`, `autorelease: pending`, and
`autorelease: tagged`. Dependabot references the ecosystem labels in
`.github/dependabot.yml`; absent labels are simply not applied, so create them
before relying on label-based triage.

## Bootstrap order

The new remote has no default branch until the first push. Bootstrap in this
order:

1. Review and push the complete baseline to `main`; the release job is safely
   skipped until `RELEASE_APP_ID` exists.
2. Let every workflow run once so GitHub records its check names.
3. Configure repository merge settings and Actions SHA enforcement.
4. Create the release App, variables, secret, and tag ruleset.
5. Manually dispatch `Release` to create or update the first release pull
   request.
6. Create and activate the `main` ruleset with recorded check names.
7. Enable the security features and private reporting.
8. Verify labels, team membership, and `CODEOWNERS` resolution.
9. Open a non-release test pull request and exercise the merge queue.
10. Merge a controlled release pull request and verify package consumption.

Rulesets are intentionally configured in GitHub rather than synchronized from
a checked-in token-bearing script. Record material settings changes in an
issue or ADR so GitHub-side policy remains auditable.
