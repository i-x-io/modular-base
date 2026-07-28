# GitHub governance and repository setup

Repository files define the automation, while GitHub settings, rulesets,
credentials, and security features remain external state. Reconcile this
checklist whenever a workflow name, required check, merge policy, or release
credential changes.

## Operating model

The automation has four focused workflows:

| Workflow | Trigger | Responsibility |
| --- | --- | --- |
| `Pull request` | `pull_request`, `merge_group` | Read-only NUKE `CI`, dependency review, and one aggregate gate. |
| `Auto-merge` | successful `Pull request` workflow run | Trusted API-only transition into auto-merge or the merge queue. |
| `Release` | push to `main` | Invoke NUKE `Publish`, attest the released artifacts, and retain evidence. |
| `Pull request labels` | `pull_request_target`, manual dispatch | API-only branch/path classification without executing pull-request code. |

NUKE owns validation, GitHub event/merged-PR interpretation, release
classification, protected-tag and GitHub-release reconciliation, package
inspection, version/tag agreement, SBOM generation, release assets, and
evidence. GitHub Actions owns triggers, permissions, caching, Dependency
Review, attestations, auto-merge orchestration, and artifact retention.

## Repository merge settings

Configure:

- default branch `main`;
- squash merge as the only merge method;
- pull-request title as the default squash subject;
- automatic deletion of head branches;
- auto-merge enabled;
- merge commits and rebase merge disabled; and
- default Actions workflow permissions set to read-only.

Ordinary titles are Conventional Commits. An explicit stable release uses
`RELEASE: <description>` and a `release/<issue>-<description>` branch. Every
other merged pull request becomes a prerelease.

## Actions trust boundaries

Require third-party actions to be pinned to full commit SHAs. The allowlist
needs only `actions/*`; Dependency Review is an official GitHub-maintained
action under that namespace. The trusted auto-merge workflow also uses the
GitHub-hosted `gh` CLI for its documented merge-queue operation.

The `Pull request` workflow executes candidate code only with read permissions
and receives no repository release credential. NUKE `CI` reads the trusted
GitHub event payload and enforces title, branch, and issue policy alongside the
code and package checks.

`Auto-merge` runs through `workflow_run`, which receives a privileged token
even when the initiating pull request did not. It must remain API-only: never
check out pull-request code, restore a pull-request cache, download an artifact,
or execute a script supplied by the triggering run. It binds the merge request
to the exact successful head SHA.

`Pull request labels` has the same API-only trust boundary. It uses the label
configuration from the protected default branch, reads changed paths through
GitHub, and must never check out or execute pull-request-controlled content.

`Release` runs only after code reaches protected `main`. Treat that branch as
the trust boundary for code that receives release permissions.

## Main branch ruleset

Create an active ruleset named `main` targeting the default branch. Configure:

- restrict deletion and block force pushes;
- require linear history and a pull request;
- allow only squash merge;
- require all conversations to be resolved;
- require one approval when at least two independent maintainers are active;
- dismiss stale approvals when approvals are required;
- disallow ruleset bypass for normal operation; and
- require the exact check `Pull request gate` from GitHub Actions.

For an active repository, require the merge queue. The `Pull request` workflow
listens to `merge_group`, and the aggregate gate intentionally accepts skipped
PR-metadata and Dependency Review jobs only for that event while rerunning the
full NUKE validation against the combined queue commit. GitHub requires the
`merge_group` event for required Actions checks used by a merge queue.

Do not require the individual `Validate` or `Dependency review` job separately.
`Pull request gate` is the stable public contract and fails unless all
applicable jobs succeed.

## Release tag ruleset and credential

Create an active tag ruleset named `repository release tags` targeting:

```text
v*
```

Restrict creation, update, and deletion and block force updates. NUKE
`Publish` is idempotent: an existing tag is accepted only when it already
points to the pushed `main` commit. A conflicting remote tag fails without
mutation.

Store `RELEASE_TOKEN` as a repository Actions secret. Prefer a short-lived,
organization-owned GitHub App installation token. If that is not yet
available, use a fine-grained personal access token limited to this repository
with Contents write permission and the shortest practical expiration. Add only
that App or token owner to the tag-ruleset bypass list. Record ownership and
expiry privately and rotate before expiry.

The workflow falls back to its job token where repository policy permits it,
but a protected tag ruleset should require the dedicated credential. The job
token separately receives pull-request read and package write permissions and
is passed as `GitHubToken`; NUKE derives owner, repository, and package source
from `[GitRepository]`.

## GitHub Packages and release evidence

Packages publish to:

```text
https://nuget.pkg.github.com/i-x-io/index.json
```

Grant package read access to consuming repositories explicitly. Actions
consumers should request `packages: read`; developer and external clients use a
least-privilege credential with `read:packages`. Never commit a token to
`NuGet.Config`.

Each release must contain:

- the `.nupkg` and `.snupkg` assets;
- the CycloneDX JSON SBOM;
- `release-plan.json` as a release asset and retained Actions evidence;
- `release-manifest.json` and `SHA256SUMS` as release assets and retained
  evidence; and
- GitHub provenance and SBOM attestations.

Ordinary merges must be marked prerelease and must not become the latest
release. Only `RELEASE:` merges are stable and update the latest-release
pointer.

## Security features

Enable:

- dependency graph and Dependency Review;
- Dependabot alerts and security updates;
- secret scanning and push protection;
- private vulnerability reporting; and
- automatic security advisories for supported ecosystems.

The repository additionally enforces locked restore, NuGet audit, Gitleaks,
action SHA pinning, actionlint, zizmor, package inspection, and SBOM generation.
Add CodeQL default setup after verifying its merge-queue check name, then make
that exact check required if desired.

## Bootstrap and reconciliation

1. Push the reviewed automation to `main` before enabling automatic release.
2. Run a test pull request so GitHub records `Pull request gate`.
3. Enable auto-merge, squash-only merging, the merge queue, and the `main`
   ruleset.
4. Install `RELEASE_TOKEN` and the immutable release-tag ruleset.
5. Merge an ordinary test pull request and verify prerelease package, tag,
   attestations, assets, and generated notes.
6. Merge a controlled `RELEASE:` pull request and verify promotion to a stable
   version and latest release.
7. Verify a direct push, a conflicting tag, a failed gate, and a draft pull
   request cannot publish or merge.

Rulesets remain configured in GitHub rather than synchronized from a
token-bearing repository script. Record material settings changes in an issue
or ADR so external policy remains auditable.
