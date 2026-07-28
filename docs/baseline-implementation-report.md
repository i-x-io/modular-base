# C# baseline implementation report

Report date: 2026-07-28

## Executive result

The repository is now an executable vanilla C# library baseline rather than an
empty policy catalog. It contains a shipping `IX.Modularity` project, a
cross-platform 24-test suite, locked dependencies, deterministic packaging,
typed NUKE orchestration, managed Git hooks, hardened GitHub workflows,
templates, release automation, and a documented governance model. The release
contract has been reset to a repository-wide lockstep model so additional
packable projects can be added without changing the build host.

The selected platform is .NET 10 with stable C# 14. The selected workflow build
system is NUKE 10.1, while SDK-style MSBuild remains the compiler and project
evaluation engine underneath it. This gives contributors a typed C# target
graph without duplicating build behavior in GitHub Actions.

No mediator or dispatcher package was added. The package is reflection-free
and uses explicit generic module registration. No optional package from the
large central catalog becomes a runtime dependency merely because it is
documented or centrally versioned.

## What is covered

| Area | Implemented control |
| --- | --- |
| Platform | Exact SDK `10.0.302`, `net10.0`, stable C# `14.0`, no prerelease or roll-forward. |
| Compilation | Nullable enabled, warnings and analyzer findings as errors, deterministic portable symbols, documentation output. |
| Code policy | EditorConfig, banned APIs, private global analyzers, project-scoped public API analyzer. |
| Product | `IX.Modularity` contracts, validated identifiers/descriptors, ordered and duplicate-safe explicit DI registration. |
| Tests | xUnit v3 on Microsoft Testing Platform with 24 tests and a non-zero discovery guard. |
| Restore | Central package management, source mapping, NuGet audit, committed locks for product, test, and build graphs. |
| Packaging | MinVer versioning, README/LICENSE/XML docs, symbols package, repository metadata, SDK package validation. |
| Package inspection | Exact artifact count, required entries, repository URL, package version, and runtime dependency allowlist. |
| Supply chain | Low-severity NuGet audit, dependency review, Dependabot, full-history Gitleaks, CycloneDX SBOM. |
| Local workflow | Managed pre-commit, pre-push, and commit-message hooks with pinned revisions. |
| Workflow linting | GitHub schemas, actionlint, and strict zizmor collection. |
| CI | Focused Ubuntu pull-request and merge-group validation using the same NUKE `CI` target. |
| Pull requests | Issue forms, PR template, branch pattern, issue linkage, conventional or explicit stable-release title, one aggregate gate, and trusted auto-merge. |
| Releases | Typed multi-package NUKE release plan, MinVer prereleases on ordinary merges, `RELEASE:` stable promotion, protected repository tags, exact tag/package match, attestations, assets, and GitHub Packages publication. |

## Pre-commit research outcome

The repository installs tools through `pre-commit`; contributors do not need
separate global copies. Selected hooks are pinned to reviewed revisions:

| Hook | Version | Reason retained |
| --- | ---: | --- |
| `pre-commit-hooks` | 6.0.0 | Portable, well-scoped file hygiene and syntax checks with minimal overlap. |
| Markdownlint CLI2 | 0.23.2 | Native published hook, existing configuration compatibility, safe fixes. |
| Gitleaks | 8.30.1 | Staged scanning plus a separate full-history CI/manual alias with redaction. |
| Typos | 1.48.0 | Fast prose, filename, and identifier checking with reviewed fixes. |
| conventional-pre-commit | 4.4.0 | Focused Conventional Commits grammar at `commit-msg` without owning releases. |
| check-jsonschema | 0.37.4 | First-party schemas for workflows, Dependabot, issue forms, and issue config. |
| actionlint | 1.7.12 | GitHub expression, event, shell, and workflow semantic checking. |
| zizmor | 1.28.0 | Security-specific Actions analysis, including permissions, credential persistence, and ref pinning. |
| local NUKE hook | repository | Runs the authoritative `CI` graph at pre-push/manual stages. |

NuGet-generated `packages.lock.json` files remain under JSON syntax checking
but are excluded from generic JSON autoformatting, avoiding a permanent diff
between NuGet's serializer and the hook formatter.

Alternatives were not stacked when they duplicated a selected capability:
`markdownlint-cli` duplicates CLI2, codespell duplicates Typos, and
detect-secrets or TruffleHog duplicate the normal Gitleaks commit path. A
complementary scanner is justified only if it adds a distinct guarantee, such
as verified live-secret detection, and its network/service boundary is
accepted. ShellCheck is deferred because the only shell launcher is a trivial
forwarder; add it when maintained shell logic exists. A networked link checker
is intentionally outside the four build outcomes and the merge gate.

## Why NUKE is used fully

NUKE is not used as a decorative wrapper around a list of shell commands. The
four-target graph is isolated in `Build.cs`, injected values and the four
custom parameters live in `Build.Parameters.cs`, and `Pipeline/` is the single
composition boundary. Vertical `Repository/`, `Tooling/`, `Validation/`, and
`Release/` areas own cohesive behavior. Paths, repository-derived identity,
graph roles, immutable policy, and toolchain versions are typed objects, and
the build assembly has its own enterprise test project.

The solution is treated as a graph rather than generated hardcoded product and
test accessors. Every packable project is discovered through evaluated
`IsPackable` metadata, runtime dependencies come from evaluated
`PackageReference` items, and the solution plus build tests are managed build
inputs. Typed
`DotNet*` wrappers cover standard SDK commands; generic process execution
remains only for installed tools and CLI surfaces without a suitable NUKE 10.1
wrapper.

Central MSBuild MinVer configuration remains authoritative for one lockstep
version and the `v` repository tag prefix. NUKE inspects every produced
package, creates a schema-v2 release plan, requires every final package to
equal the tagged version, generates versioned evidence, and reconciles the
GitHub release through Octokit. Focused workflows retain triggers, permissions,
caches, merge orchestration, Dependency Review, attestations, and retention.

Reusable NUKE components should be extracted when a second repository needs
the same stable contract. The current internal abstraction layer provides that
seam without publishing repository-specific components prematurely.

## C# and .NET decision

.NET 10 and C# 14 are the best fit for a greenfield `IX.Modularity.*` baseline
whose consumers can standardize on .NET 10:

- .NET 10 is the current LTS line for the report date;
- C# 14 is the stable language version paired with `net10.0`;
- an exact language version avoids machine-dependent `latest` behavior; and
- one target keeps tests, package validation, API compatibility, dependencies,
  and support policy simpler.

This decision must be revisited before `1.0.0` if a confirmed consumer cannot
run .NET 10. Do not add `net8.0` or `netstandard2.0` speculatively: each target
creates another supported API and dependency surface. A library TFM does not
upgrade the runtime of its consuming application.

## Library and dispatcher decision

The shipping dependency set is intentionally narrow. The module package needs
only Microsoft's DI abstractions. Logging providers, HTTP frameworks, data
access, validation, mapping, messaging, resilience, and observability are host
or adapter choices and do not belong in the reusable contract baseline.

For dispatch:

1. Prefer direct service or handler injection for one in-process use case.
2. If a host already adopts FastEndpoints, evaluate its built-in command/event
   support before adding a second abstraction.
3. If framework-neutral generic dispatch becomes a demonstrated requirement,
   compare the permissively licensed martinothamar Mediator and
   Immediate.Handlers with a proof of concept.
4. If work must survive failure or cross a process boundary, compare durable
   frameworks such as Wolverine or Brighter instead of calling an in-process
   notification durable.
5. MediatR and other reciprocal/commercial choices require explicit legal,
   budget, key-lifecycle, and exit-plan approval; they are not defaults.

## GitHub coverage

Checked-in automation covers:

- read-only NUKE `CI` on pull requests and merge groups;
- pull-request title, branch, and linked-issue policy inside the same target;
- one aggregate gate followed by an API-only trusted auto-merge controller;
- API-only branch and changed-path labeling from protected configuration;
- four Dependabot ecosystems with grouped updates and cooldowns;
- one NUKE `Publish` call for plan, protected tag, validation, evidence, and
  package publication after every merged pull request;
- generated release notes, prerelease/stable classification, release assets,
  provenance, and SBOM attestations;
- structured bug/feature forms, private-security routing, and a PR template;
- immutable action revisions, explicit token permissions, no credential
  persistence, and safe pull-request events.

External repository settings cannot be inferred from YAML. The exact Actions
policy, main and tag rulesets, release credential, package permissions, security
features, labels, and bootstrap order are documented in
`docs/github-governance.md`.

## Release acceptance evidence

The first release through the reconciled contract must demonstrate that the
pull-request gate passed, the `v<semver>` tag targets the merged commit, all
packable projects published at the same version, the GitHub release has the
correct prerelease/latest classification, every package and evidence file is
attached, attestations exist, and a clean consumer can restore the packages.
Historical component-tag releases and package versions are intentionally not
part of this new contract.

## Remaining work and deliberate deferrals

The baseline is usable, but these items remain:

| Priority | Item | Trigger or fix |
| --- | --- | --- |
| P1 after the first intentional stable release | Package compatibility baseline. | Set `PackageValidationBaselineVersion` to that release after deciding how CI retrieves the GitHub Packages baseline without exposing a broad credential. |
| P1 credential hardening | Replace the broad CLI bootstrap credential with a repository-scoped fine-grained token or organization-owned App. | Verify the new identity on a release, rotate the old credential, and update the tag-ruleset actor. |
| P1 with a second maintainer | Independent ownership enforcement. | The team and `CODEOWNERS` exist; require one code-owner review after a second active maintainer joins. |
| P1 when coverage has a decision use | MTP-native coverage and threshold. | Add an open-source MTP integration and ratchet a meaningful threshold; do not add a vanity percentage. |
| P1 security hardening | CodeQL default setup. | Confirm .NET 10 results and merge-queue check names, then make it required. |
| P2 release hardening | Package signing. | Choose a supported identity, verification policy, and consumer process; provenance and SBOM attestations are already emitted. |
| P2 ecosystem signal | OpenSSF Scorecard. | Add only after permissions/findings ownership is assigned. |
| As needed | `CODE_OF_CONDUCT.md`, discussions, project boards, wiki. | Add when community or planning ownership exists. |

No `eng/` directory is planned. Build orchestration belongs in the conventional
NUKE `build/` project and the thin root launchers.

## Acceptance status

Locally, the implemented baseline has demonstrated:

- build project compilation without warnings or errors;
- locked restore of product, tests, and build tooling;
- clean strict Release compilation;
- 24 discovered and passing MTP tests;
- package and symbols-package generation and semantic inspection;
- vulnerability audit of the solution and build graph;
- a non-empty CycloneDX JSON SBOM; and
- schema, actionlint, and strict zizmor validation of GitHub files.

Local acceptance is `dotnet nuke CI --configuration Release` followed by
all-file `pre-commit` validation. Remote release acceptance is performed only
after the refactor has passed the protected pull-request workflow.
