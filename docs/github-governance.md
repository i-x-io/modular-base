# GitHub governance and repository setup

Repository files define the automation, while GitHub settings, rulesets,
credentials, and security features remain external state. Reconcile this
checklist whenever a workflow name, required check, merge policy, or release
credential changes.

## Operating model

The automation has five focused workflows:

| Workflow | Trigger | Responsibility |
| --- | --- | --- |
| `Pull request` | `pull_request`, `merge_group` | Read-only NUKE `CI`, dependency review, and one aggregate gate. |
| `Auto-merge` | successful `Pull request` run or submitted approval | Trusted API-only transition into auto-merge or the merge queue after the exact head has both validation and approval. |
| `Release` | push to `main` | Prepare immutable inputs, attest them without a checkout, then publish through a protected environment. |
| `Pull request labels` | `pull_request_target`, manual dispatch | API-only branch/path classification without executing pull-request code. |
| `Scheduled assurance` | weekly, manual dispatch | Repeat the locked audit/repository gate and smoke-test build/test on Windows Server 2025. |

NUKE owns validation, merged-PR interpretation, release classification,
package inspection, version/tag agreement, package-specific SBOM generation,
and checksummed evidence. GitHub Actions owns triggers, permissions, caching,
Dependency Review, attestations, protected remote publication, auto-merge
orchestration, and artifact retention.

## Repository merge settings

Configure:

- default branch `main`;
- squash merge as the only merge method;
- pull-request title as the default squash subject;
- automatic deletion of head branches;
- auto-merge enabled only after the reviewed workflow is deployed;
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

`Release` runs only after code reaches protected `main`. Its `prepare` job has
read-only repository access. The `attest` matrix downloads checksum-verified
artifacts and never checks out repository code. Only `publish` enters a
protected environment and receives the release secret; it cannot run until
all package attestations succeed.

## Main branch ruleset

Create an active ruleset named `main` targeting the default branch. Configure:

- restrict deletion and block force pushes;
- require linear history and a pull request;
- allow only squash merge;
- require all conversations to be resolved;
- require one code-owner approval for every pull request;
- dismiss stale approvals and require approval of the latest push;
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

Create an active tag ruleset named `release tags` targeting:

```text
v*
```

Restrict creation, update, and deletion and block force updates. Publication
fails if a tag already exists; published releases are never reconciled or
mutated. GitHub immutable releases must remain enabled.

Store `RELEASE_TOKEN` in both `release-prerelease` and `release-stable`, not at
repository scope. Prefer a short-lived, organization-owned GitHub App
installation token. Until that exists, use a fine-grained personal access
token limited to this repository with Contents write permission and the
shortest practical expiration. Add only that App or token owner to the tag
ruleset bypass list. Record ownership and expiry privately and rotate before
expiry. There is no job-token fallback.

`release-prerelease` accepts only protected branches. `release-stable` also
requires approval by the maintainers team. Stable intent must originate in
this repository on `release/*`, use `RELEASE: <description>`, and carry the
`stable-release-approved` label. The job token publishes packages; the
environment secret is used only for the protected tag and GitHub release.

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
- one uniquely named CycloneDX JSON SBOM per package;
- `release-plan.json` as a release asset and retained Actions evidence;
- `release-manifest.json` and `SHA256SUMS` as release assets and retained
  evidence; and
- GitHub provenance and SBOM attestations.

Validation diagnostics are retained for 14 days. The complete package, SBOM,
plan, manifest, and checksum evidence produced by a release is retained as one
Actions artifact for 30 days in addition to the release assets.

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
CodeQL default setup is enabled for Actions and C# with the extended local and
remote threat model. The initial run completed successfully. Test-fixture path
alerts were dismissed as test-only and the build-owned SBOM directory finding
as a documented false positive; no open alert remains. Confirm default setup on
a merge-queue commit before adding its check names to the ruleset.

## Reconciliation procedure

1. Push the reviewed automation to `main` before enabling auto-merge or release.
2. Run a test pull request so GitHub records `Pull request gate`.
3. Enable auto-merge, squash-only merging, the merge queue, and the `main`
   ruleset.
4. Install `RELEASE_TOKEN` separately in both release environments, replace
   the maintainer-team tag bypass with the dedicated GitHub App, and remove the
   repository-scoped secret.
5. Merge an ordinary test pull request and verify prerelease package, tag,
   attestations, assets, and generated notes.
6. Merge a controlled `RELEASE:` pull request and verify promotion to a stable
   version and latest release.
7. Verify a direct push, a conflicting tag, a failed gate, and a draft pull
   request cannot publish or merge.

Rulesets remain configured in GitHub rather than synchronized from a
token-bearing repository script. Record material settings changes in an issue
or ADR so external policy remains auditable.

## Current reconciliation status

The enterprise-pipeline rollout was reconciled on 2026-07-28 through
[pull request #15](https://github.com/i-x-io/modular-base/pull/15). The external
state was verified as follows:

| Control | Verified state |
| --- | --- |
| Default branch | `main`; squash-only and automatic head-branch deletion. Auto-merge is temporarily disabled until the reviewed workflow reaches `main`. |
| Branch ruleset | Active `main` ruleset; one code-owner approval, stale-review dismissal, latest-push approval, resolved conversations, merge queue, and exact `Pull request gate` check are required; deletion, force pushes, bypass, and non-linear history are blocked. |
| Tag and release immutability | Active `release tags` ruleset targeting `v*`; GitHub immutable releases are enabled. The current team-wide tag bypass still needs replacement by a dedicated App. |
| Release environments | `release-prerelease` accepts protected branches; `release-stable` additionally requires maintainers-team approval. The existing repository-scoped `RELEASE_TOKEN` still needs to be re-created in both environments and then removed from repository scope. |
| Workflow set | `Auto-merge` and `Release` are intentionally disabled during rollout. `Pull request`, `Pull request labels`, and Dependabot remain active; `Scheduled assurance` becomes available after merge. |
| Code scanning | CodeQL default setup is configured for Actions and C# with `remote_and_local` threat modeling. The initial run passed and all 18 path findings were triaged with no open alert; merge-queue behavior remains to be confirmed before making checks required. |
| Stable release | [`v0.1.0`](https://github.com/i-x-io/modular-base/releases/tag/v0.1.0) targets merge commit `c0deee6f8ee7dcc78791c44c51ce5c5d9c1dea90` and is the latest stable release. |
| Published evidence | Package, symbols package, CycloneDX SBOM, schema-v2 plan and manifest, checksums, SLSA provenance, CycloneDX attestation, and retained Actions evidence were verified. |
| Labeling | The API-only labeler successfully synchronized branch/path labels without checking out pull-request content. |

The stable `RELEASE:` path, draft exclusion, ready-for-review validation, and
merge-group validation have production evidence. The ordinary-prerelease path
is covered by build tests but still needs a controlled remote smoke release.
Direct-push and conflicting-tag failures are policy invariants and should be
tested only in a disposable repository or other non-production fixture.
