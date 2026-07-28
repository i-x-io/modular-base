# C# baseline implementation report

Report date: 2026-07-28

## Executive result

The repository is now an executable vanilla C# library baseline rather than an
empty policy catalog. It contains a shipping `IX.Modularity` project, a
cross-platform 24-test suite, locked dependencies, deterministic packaging,
typed NUKE orchestration, managed Git hooks, hardened GitHub workflows,
templates, release automation, and a documented governance model.

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
| CI | Linux, Windows, and macOS validation using the same NUKE `Validate` target. |
| Pull requests | Issue forms, PR template, branch pattern, issue linkage, Conventional Commit title. |
| Classification | Native issue-form labels and official Labeler branch/path automation with a namespaced taxonomy. |
| PR feedback | One update-in-place comment with change metadata, required checks, dependency diff, and NUKE guarantees. |
| Releases | Release Please manifest flow, commit-generated changelog and release message, protected component tags, exact tag/package match, GitHub Packages publish target. |
| Maintenance | Weekly dependency freshness and documentation link checks. |

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
| local NUKE hook | repository | Runs the authoritative `Validate` graph at pre-push/manual stages. |

NuGet-generated `packages.lock.json` files remain under JSON syntax checking
but are excluded from generic JSON autoformatting, avoiding a permanent diff
between NuGet's serializer and the hook formatter.

Alternatives were not stacked when they duplicated a selected capability:
`markdownlint-cli` duplicates CLI2, codespell duplicates Typos, and
detect-secrets or TruffleHog duplicate the normal Gitleaks commit path. A
complementary scanner is justified only if it adds a distinct guarantee, such
as verified live-secret detection, and its network/service boundary is
accepted. ShellCheck is deferred because the only shell launcher is a trivial
forwarder; add it when maintained shell logic exists. A link checker belongs in
scheduled CI rather than every local commit, so Lychee runs weekly.

## Why NUKE is used fully

NUKE is not used as a decorative wrapper around a list of shell commands. The
build definition uses:

- typed `DotNetToolRestore`, `DotNetRestore`, `DotNetFormat`, `DotNetBuild`,
  `DotNetTest`, `DotNetPack`, and `DotNetNuGetPush` settings;
- target descriptions and an explicit dependency graph;
- output declarations for test, package, and SBOM artifacts;
- early target requirements for server-only publishing and secrets;
- strongly typed solution and project generation;
- injected Git repository metadata;
- typed build configuration values with generated schema completion;
- secret parameter handling;
- native build-root and build-project paths; and
- strongly typed GitHub Actions runtime context for repository validation.

Raw `dotnet` invocation remains only where NUKE 10.1 has no appropriate typed
wrapper: the installed CycloneDX and outdated tools and the .NET 10 noun-first
`dotnet package list` audit command.

The `[MinVer]` NUKE injection was deliberately not added. The package's
authoritative MinVer MSBuild configuration uses the component-specific
`IX.Modularity-v` tag prefix, while NUKE's generic injection does not preserve
that project setting. The build instead reads the produced package version and
requires it to equal the exact repository tag. The NUKE `[GitHubActions]`
workflow generator is also deliberately not used: the hand-authored workflows
need full-SHA action pins, least-privilege job permissions, pre-commit caches,
matrix artifacts, merge-queue events, dependency review, and Release Please.
NUKE still supplies the runtime GitHub context and owns all build behavior.

Build components are unnecessary for one small build class. Introduce them
only when multiple repositories or genuinely independent build concerns would
reuse the components; premature components would hide this baseline's target
graph without reducing duplication.

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

- CI on pushes, pull requests, merge groups, and manual invocation;
- pull-request title, branch, and linked-issue policy;
- four Dependabot ecosystems with grouped updates and cooldowns;
- release PR, tag, GitHub release, package validation, and package publish;
- a checked-in changelog and identical generated GitHub release message from
  Conventional Commits;
- weekly dependency freshness and Markdown link checks;
- structured bug/feature forms, private-security routing, and a PR template;
- native issue labels plus safe branch/path pull-request classification;
- a trusted base-branch result commenter that works for repository, fork, and
  Dependabot pull requests without executing their content;
- immutable action revisions, explicit token permissions, no credential
  persistence, and safe pull-request events.

External repository settings cannot be inferred from YAML. The exact Actions
policy, main and tag rulesets, release credential, package permissions, security
features, labels, and bootstrap order are documented in
`docs/github-governance.md`.

## Remaining work and deliberate deferrals

The baseline is usable, but these items remain:

| Priority | Item | Trigger or fix |
| --- | --- | --- |
| P0 before first publish | Exercise Release Please and restore the package from a clean consumer. | Use the controlled `0.1.0` release and verify GitHub Packages permissions. |
| P1 after first stable release | Package compatibility baseline. | Set `PackageValidationBaselineVersion` to an intentional released version. |
| P1 credential hardening | Replace the broad CLI bootstrap credential with a repository-scoped fine-grained token or organization-owned App. | Verify the new identity on a release, rotate the old credential, and update the tag-ruleset actor. |
| P1 with a second maintainer | Independent ownership enforcement. | The team and `CODEOWNERS` exist; require one code-owner review after a second active maintainer joins. |
| P1 when coverage has a decision use | MTP-native coverage and threshold. | Add an open-source MTP integration and ratchet a meaningful threshold; do not add a vanity percentage. |
| P1 security hardening | CodeQL default setup. | Confirm .NET 10 results and merge-queue check names, then make it required. |
| P2 release hardening | Package signing/provenance and artifact attestations. | Choose a supported identity, verification policy, and consumer process first. |
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

The final acceptance run for the complete uncommitted change set is
`dotnet nuke Validate --configuration Release` followed by all-file
`pre-commit` validation. Remote workflow and release acceptance necessarily
remain pending until this baseline is pushed and GitHub-side settings are
configured.
