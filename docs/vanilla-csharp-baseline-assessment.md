# Vanilla C# baseline assessment

Assessment date: 2026-07-28

Repository state: `main` at `7c1f50c`

Scope: current repository configuration, developer workflow, build and test orchestration, package posture, C#/.NET choice, and mediator/dispatcher alternatives.

## Executive conclusion

The repository is a strong, carefully documented **policy and package catalog**, but it is not yet an executable or validated C# baseline. The empty solution is intentional, yet it means restore, format, build, pack, audit, and SBOM commands either do no useful work or produce false-positive success. The documented test command currently fails, and the outdated-package tool crashes while examining the empty solution.

The recommended baseline is:

| Decision | Recommendation |
| --- | --- |
| Runtime and language | Keep .NET 10 and stable C# 14 if all intended consumers can require .NET 10. Do not use `latest` or preview language versions. |
| Build system | Use the .NET CLI and MSBuild as the canonical build system. Add a small provider-neutral MSBuild orchestration project only when the first real project is added. |
| Solution format | Keep `.slnx`; it is the .NET 10 default solution format. |
| CI | Add an authoritative CI workflow that invokes the same local `Validate` target and fails when the expected project or test count is zero. |
| Git hooks | Use `pre-commit` with its managed Markdownlint CLI2, Gitleaks, Typos, Conventional Commits, and standard hygiene hooks. Keep the SDK-wide formatting check in pre-push and repeat required checks in CI. |
| Package catalog | Separate the minimal baseline from optional application choices. Do not add packages merely because they are popular, and preferably pin a package only when a project actually adopts it. |
| Dispatcher | Do not put a mediator or dispatcher dependency in the reusable library baseline. Prefer direct handler/service injection. If a FastEndpoints application already exists, use its built-in command/event support. If a framework-neutral in-process dispatcher is genuinely needed, shortlist the MIT-licensed `Mediator.SourceGenerator`/`Mediator.Abstractions`. |
| Durable messaging | Evaluate Wolverine or Brighter at the application boundary only when inbox/outbox, retries, transports, or background delivery are actual requirements. They are not drop-in in-process mediator choices. |
| Commercial packages | Exclude them from the default catalog. Permit them only through an explicit architecture and licensing decision with ownership of keys, renewals, and exit strategy. |

The most important next step is not another package. It is a conformance project plus one authoritative validation path that proves the baseline settings are actually imported and enforced.

## Scope and assumptions

This recommendation assumes:

- the future `IX.Modularity.*` code is greenfield;
- Windows, Linux, and macOS development should remain viable;
- libraries are primarily consumed by other modern IX applications rather than .NET Framework applications;
- NuGet packages will eventually be built and published;
- permissive open-source dependencies are preferred, with reciprocal or commercial licensing requiring explicit review; and
- runtime reflection should remain exceptional, consistent with the repository's current policy.

If a real consumer matrix requires .NET Framework, Unity, Xamarin, or applications that cannot move to .NET 10, the target-framework recommendation must be revisited before the first public package is released.

## Follow-up implementation status

This assessment records the repository at `7c1f50c` before remediation. A follow-up change implements the root-level hook recommendation without adding `eng/` orchestration or C# projects:

- `pre-commit` 4.6.1 manages pinned standard hygiene, Markdownlint CLI2 0.23.2, Gitleaks 8.30.1, Typos 1.48.0, and conventional-pre-commit 4.4.0 environments;
- fast staged-file checks run at commit time and the SDK-provided `dotnet format` verification runs at pre-push;
- the only initial spelling exception is the package-author surname `Yoh`;
- Gitleaks extends its default rules and narrowly allows one reviewed documentation false positive; and
- no runtime, mediator, test, coverage, versioning, or other NuGet package is adopted.

These hooks improve local feedback but do not change the central conclusion: the empty solution still cannot prove meaningful restore, build, test, pack, lock-file, package-validation, or SBOM behavior.

## What exists today

The repository contains 120 tracked files, of which 106 are Markdown documentation. It has no `.csproj`, source code, test code, CI definition, hook configuration, build entry point, or release workflow.

The principal baseline files are:

- [`global.json`](../global.json), which selects SDK `10.0.302` exactly and selects Microsoft.Testing.Platform;
- [`Directory.Build.props`](../Directory.Build.props), which supplies .NET 10/C# 14, analysis, deterministic-build, lock-file, audit, artifacts, and packaging settings;
- [`Directory.Packages.props`](../Directory.Packages.props), which centrally pins 84 packages and installs five global analyzer packages;
- [`ModularBase.globalconfig`](../ModularBase.globalconfig) and [`.editorconfig`](../.editorconfig), which make a broad set of analyzer and style findings build-blocking;
- [`BannedSymbols.txt`](../BannedSymbols.txt), which bans mutable dictionary interfaces, ambient time, and broad reflection APIs;
- [`NuGet.Config`](../NuGet.Config), which clears inherited feeds, uses NuGet.org only, maps all packages to that source, and uses it as the audit source;
- [`.config/dotnet-tools.json`](../.config/dotnet-tools.json), which pins CycloneDX and `dotnet-outdated`; and
- [`IX.Modularity.slnx`](../IX.Modularity.slnx), which contains no projects.

The package documentation correctly distinguishes a central pin from adoption: a package does not enter a project's dependency graph until a project adds a `PackageReference`. This is how NuGet [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) works. Nevertheless, every speculative pin still has review, update, documentation, and license-governance cost.

### Open-source posture of the current catalog

The repository's own supply-chain records classify the 89 centrally managed entries as follows:

| Recorded license | Count | Baseline implication |
| --- | ---: | --- |
| MIT | 65 | Permissive; normally acceptable under an OSS-first policy. |
| Apache-2.0 | 17 | Permissive with an express patent grant; normally acceptable. |
| PostgreSQL | 3 | Permissive; normally acceptable. |
| BSD-3-Clause | 2 | Permissive; normally acceptable. |
| BSD-2-Clause | 1 | Permissive; normally acceptable. |
| LGPL-3.0-only | 1 | Open source but reciprocal; the Sonar analyzer should have an explicit legal/policy disposition even though it is build-only. |

No currently pinned package is documented as requiring a commercial runtime license. That is encouraging, but package metadata is not a legal opinion and can change between versions. A package upgrade should re-check the license, not inherit approval from the previous version. A package's open-source license also says nothing about the cost or terms of a cloud service it connects to.

## Validation performed

The following checks were run on macOS arm64 with the repository-selected SDK `10.0.302`, runtime `10.0.10`, and MSBuild `18.6.11`.

| Command or check | Observed result | Interpretation |
| --- | --- | --- |
| `dotnet tool restore` | Succeeded. | The two local tools restore reproducibly. |
| `dotnet restore IX.Modularity.slnx --locked-mode` | Exit 0 with “No project to restore.” | Successful exit is not dependency validation. |
| `dotnet format ... --verify-no-changes --no-restore` | Exit 0 with no work. | Formatting policy is not being exercised. |
| `dotnet build ... -c Release --no-restore` | Exit 0, zero warnings and zero errors. | This is a false green because no project imports the baseline. |
| `dotnet test ... -c Release --no-build` | Exit 1: solution configuration `Release\|` is invalid. | The README's validation sequence does not currently complete. |
| `dotnet pack ... -c Release --no-build` | Exit 0 with no package. | Packaging settings and metadata are untested. |
| `dotnet outdated ... --fail-on-updates` | Exit 134 with a `FileNotFoundException` in its temporary analysis path. | The tool does not handle this empty solution usefully. |
| `dotnet CycloneDX ...` | Exit 0, reports zero projects and zero packages, and writes an empty SBOM. | File existence must not be treated as evidence of a valid SBOM. |
| `dotnet sln IX.Modularity.slnx list` | Reports no projects. | Confirms the root cause of the false greens. |
| Markdown lint | 106 files, zero findings. | Content is clean, but the runner was an unpinned machine installation rather than repository tooling. |
| XML and JSON parsing | All tracked configuration parsed successfully. | Static syntax is sound. |

The baseline therefore cannot currently prove that analyzers load, banned APIs fail, package locks are generated, MTP discovers tests, package validation runs, or the produced NuGet metadata is correct.

## Strengths worth retaining

### Reproducible SDK and restore policy

The exact SDK selection is deliberate and defensible. Microsoft's current [`global.json` guidance](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) specifically notes that `rollForward: disable` keeps the SDK and dependency graph in lockstep when package lock files are used. The trade-off is maintenance: CI must install that exact SDK, and an automated update must keep the SDK, runtime-package family, locks, and validation together.

Lock files should be committed for repository reproducibility. NuGet notes that an application lock file controls the complete dependency graph, whereas a library's lock file does not constrain the graph selected by its eventual consumer. Document the repository-CI rationale so nobody mistakes library lock files for consumer dependency control. See NuGet's [PackageReference lock-file guidance](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#locking-dependencies).

### Strict compile-time feedback

Nullable reference types, stable language selection, warnings as errors, deterministic output, portable symbols, Source Link-related properties, package validation, and analyzer configuration are all sound baseline capabilities. They shift errors earlier and make library packaging more reviewable.

### Supply-chain controls

Cleared package sources, package source mapping, transitive vulnerability audit mode, low-severity audit threshold, centrally disabled version overrides, SBOM tooling, and detailed per-package documentation form an unusually good starting point. NuGet's [audit guidance](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages) should be used to add an expected audited-project count so an empty or accidentally excluded project set cannot pass silently.

### Documentation quality

The package guides distinguish ownership boundaries and warn against blindly composing overlapping packages. That decision-oriented documentation is more valuable than a long undifferentiated package list and should remain the model for future additions.

## Priority findings and missing pieces

### P0: make validation meaningful

Add at least one non-shipping conformance class library and one MTP test project. They should import the root settings and deliberately exercise:

- nullable and analyzer configuration;
- generated documentation and deterministic build properties;
- one allowed time abstraction (`TimeProvider`);
- a project lock file and locked restore;
- MTP discovery and execution;
- package creation, symbols, repository metadata, and package validation; and
- an expected failure fixture for a banned API, if practical in a separate compile-test harness.

The canonical build must also assert minimum counts: at least one evaluated project, at least one discovered test module, and at least one package when the pack/SBOM targets claim success. This prevents the exact false greens observed above.

### P0: add one authoritative build and CI path

There is no provider-neutral build entry point and no CI adapter. Direct commands in the README are useful documentation, but they can drift and cannot express invariants such as expected project count.

When the first project is added, create a small `eng/Build.proj` with targets such as:

| Target | Responsibility |
| --- | --- |
| `Bootstrap` | Restore local tools and validate the expected repository/project shape. |
| `Restore` | Restore with locks and NuGet audit enabled. |
| `Format` | Run `dotnet format --verify-no-changes`. |
| `Build` | Compile Release with analyzers and warnings as errors. |
| `Test` | Run MTP tests and assert that test modules were discovered. |
| `Pack` | Produce and inspect expected `.nupkg` and `.snupkg` files. |
| `DependencyReport` | Run the outdated-package check only when projects exist. |
| `Sbom` | Generate an SBOM and assert non-zero project/package expectations appropriate to the repository. |
| `Validate` | Aggregate restore, format, build, test, pack, and structural checks. |
| `Release` | A separately protected target for signing/publishing; never part of a local hook. |

CI should call the same `Validate` target as developers. Provider-specific YAML should only provision the SDK/cache and invoke the build; it should not contain a second implementation of the build logic.

### P0: align the MTP test and coverage packages

[`global.json`](../global.json) selects Microsoft.Testing.Platform, but the catalog pins the generic `xunit.v3` package, which in the current 3.x line defaults to MTP v1. xUnit now provides `xunit.v3.mtp-v2` to select MTP v2 explicitly and uses it in its current template. See xUnit's [MTP version-selection guidance](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform) and [current v3 setup](https://xunit.net/docs/getting-started/v3/getting-started).

The catalog also pins `coverlet.collector`, which is the VSTest collector integration. For this MTP-first baseline, use the MIT-licensed [`coverlet.MTP`](https://github.com/coverlet-coverage/coverlet) package and its `--coverlet` switch. Microsoft's [MTP coverage documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-code-coverage) confirms that `coverlet.MTP` is the native Coverlet MTP extension. The Microsoft coverage extension is free to use but closed source, so it is a poorer fit for the stated OSS preference.

Keep `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk` only if the supported IDE/runner matrix still needs VSTest compatibility. The xUnit documentation explicitly treats that as a compatibility choice, not a requirement for MTP-native execution.

### P0: define package versioning before the first release

No repository versioning policy exists. Without one, packages can accidentally use SDK defaults and package compatibility has no stable baseline version.

For this simple, tag-driven library repository, adopt [`MinVer`](https://github.com/adamralph/minver) as a private build dependency when packing starts. It is Apache-2.0, cross-platform, and derives SDK package/assembly versions from SemVer tags without a separate server. Nerdbank.GitVersioning remains a good alternative if the team needs a checked-in version file, commit-unique versions, or a more elaborate nightly/release flow.

Also define:

- tag format and ownership;
- prerelease semantics;
- shallow-clone/fetch-depth requirements;
- `PackageValidationBaselineVersion` selection for released packages;
- public API compatibility policy; and
- who may publish and how credentials/provenance are managed.

### P1: add fast hooks, but keep CI authoritative

Git hooks are developer feedback, not enforcement: they can be skipped, may not run in GUI clients, and do not validate the merge result. CI must repeat every required check.

For the current repository, use the open-source [`pre-commit`](https://pre-commit.com/) framework because it manages pinned, isolated hook environments across multiple ecosystems. Developers install only `pre-commit` 4.6.1; `pre-commit install --install-hooks` provisions the selected tools without requiring separate global Node, Go, Rust, Markdownlint, Gitleaks, or Typos installations.

| Tool | Managed hook and version | Role | License |
| --- | --- | --- | --- |
| [`pre-commit-hooks`](https://github.com/pre-commit/pre-commit-hooks/tree/v6.0.0) | Standard hook IDs at 6.0.0 | Structured-file syntax, portability, whitespace, line endings, private keys, and accidental large files | MIT |
| [`markdownlint-cli2`](https://github.com/DavidAnson/markdownlint-cli2/blob/v0.23.2/.pre-commit-hooks.yaml) | `markdownlint-cli2` at 0.23.2 | Reuse `.markdownlint.json` and apply reviewed Markdown fixes in a managed Node environment | MIT |
| [Gitleaks](https://github.com/gitleaks/gitleaks/blob/v8.30.1/.pre-commit-hooks.yaml) | `gitleaks` at 8.30.1 | Scan staged Git changes with redacted output in a managed Go environment | MIT |
| [Typos](https://github.com/crate-ci/typos/blob/v1.48.0/.pre-commit-hooks.yaml) | `typos` at 1.48.0 | Check and fix prose, filenames, and identifiers through the published binary hook | MIT OR Apache-2.0 |
| [conventional-pre-commit](https://github.com/compilerla/conventional-pre-commit/blob/v4.4.0/.pre-commit-hooks.yaml) | `conventional-pre-commit` at 4.4.0 | Validate Conventional Commits 1.0.0 at the `commit-msg` stage in managed Python | Apache-2.0 |
| .NET SDK | Local `dotnet-format` hook from SDK 10.0.302 | Verify the complete solution at pre-push without another package | .NET SDK component |

Use the managed `markdownlint-cli2`, `gitleaks`, and `typos` hook IDs rather than their Docker, system, or source-compilation variants. This keeps installation reproducible through the pinned hook revisions and avoids Docker, global Node, global Gitleaks, and Cargo prerequisites.

Alternatives were considered but do not improve this baseline:

- [`markdownlint-cli`](https://github.com/igorshubovych/markdownlint-cli/blob/v0.49.1/.pre-commit-hooks.yaml) has native check and fix hooks, but CLI2 already matches the repository configuration and prioritizes configuration-based repeated use.
- [`detect-secrets`](https://github.com/Yelp/detect-secrets) is useful when a legacy repository needs an audited secret baseline. This repository can start clean with Gitleaks defaults and one path-and-line-specific false-positive exception.
- [TruffleHog](https://github.com/trufflesecurity/trufflehog/blob/v3.96.0/.pre-commit-hooks.yaml) is a useful complementary history or CI scanner, but its default pre-commit hook focuses on verified secrets and is unnecessary alongside Gitleaks at commit time.
- GGShield's client is open source, but its strongest behavior adds GitGuardian authentication and a hosted-service boundary that the offline baseline does not need.
- [`codespell`](https://github.com/codespell-project/codespell/blob/v2.4.3/.pre-commit-hooks.yaml) is mature, but Typos is fast, also checks identifiers and filenames, and found only one wording problem plus one valid surname in the current repository.

Do not install overlapping Markdown, secret, or spelling hooks. Add schema, CI-workflow, shell, or link linters only after corresponding files and an authoritative CI stage exist.

For commit messages, `conventional-pre-commit` is preferable to broader alternatives here. It validates the [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/) grammar without introducing version bumping or release behavior. [Commitizen](https://github.com/commitizen-tools/commitizen) is appropriate when interactive commit creation, changelog generation, and versioning are desired as one workflow, while Commitlint adds a Node configuration stack and Gitlint is a more general commit-policy engine. The baseline needs only a focused validator.

The allowed types are `build`, `chore`, `ci`, `docs`, `feat`, `fix`, `perf`, `refactor`, `revert`, `style`, and `test`. Scopes are optional and unrestricted. Do not enable strict mode: Git-generated merge messages and `fixup!` commits remain useful for normal Git and autosquash workflows. Breaking changes use `!` after the type/scope or a `BREAKING CHANGE:` footer.

Additional hook research produced three immediate decisions:

- add `pretty-format-json` from the already pinned standard hook repository with two-space indentation, Unicode preservation, and original key order;
- add `forbid-submodules` because this baseline has no approved Git submodule supply-chain path; and
- reject EditorConfig Checker for now because a current-tree trial incorrectly treated indentation inside Markdown code examples as file-level indentation violations.

Lychee is a strong future link checker for this documentation-heavy repository, but its official non-Docker pre-commit hook requires a system installation and external requests can be slow or rate-limited. Run it in scheduled CI after a provider is selected. Also defer check-jsonschema, yamllint, actionlint, zizmor, and ShellCheck until schema-governed configuration, CI workflows, or shell scripts exist. Do not add `no-commit-to-branch`: server-side branch protection is authoritative, while this repository currently has no configured hosting provider.

Recommended split:

| Stage | Checks | Target duration |
| --- | --- | ---: |
| Pre-commit | Merge-conflict markers, whitespace, EOF/line endings, JSON/YAML/TOML/XML syntax, portable paths/symlinks, Markdown fixes, spelling fixes, large-file checks, and redacted staged-secret scanning. | Preferably under 10 seconds after initial environment installation. |
| Commit message | Conventional Commits grammar and the repository's allowed type set; optional scopes and Git-generated merge/fixup messages remain valid. | Under one second after initial environment installation. |
| Pre-push | SDK-wide `dotnet format --verify-no-changes`; replace or extend this with the future canonical `Validate` target when executable projects exist. | A few minutes at most. |
| CI | Clean locked restore, format, Release build, all tests/coverage, pack inspection, audit, license policy, SBOM, and structural count assertions. | Authoritative. |
| Scheduled CI | Dependency freshness, full advisory/license re-evaluation, link checking, and slower integration/compatibility matrices. | Not developer-blocking. |

[`Husky.Net`](https://github.com/alirezanet/Husky.Net) remains a possible .NET-local-tool-only alternative, but it would still need separate pinned Markdown, secret, and spelling execution. Do not install both hook managers.

### P1: reduce policy breadth until it is proven

The baseline globally installs five third-party analyzer packages and promotes essentially all analyzer warnings to build errors. That may be the desired end state, but without representative code it is impossible to measure overlap, contradictory advice, build-time cost, generated-code behavior, or upgrade churn.

Start a conformance project with Microsoft analyzers plus the banned API analyzer. Add or keep Meziantou, Roslynator, Visual Studio Threading, and Sonar rules only where their signal is demonstrated. In particular, record acceptance of SonarAnalyzer's LGPL-3.0-only license even though analyzers do not flow to package consumers.

The banned-symbol file is more opinionated than a “vanilla” baseline:

- banning all `IDictionary<TKey,TValue>` uses rejects valid owned-mutation APIs as well as weak public contracts;
- banning `System.Type`, `GetType`, `Activator`, and the entire reflection namespace can block ordinary DI registration, serializers, test infrastructure, and framework integration;
- the reflection ban fits source-generated dispatchers and mappers, but should be scoped by project role rather than assumed universal; and
- the ambient-time ban is good, but the platform-native default should be `TimeProvider`, not a mandatory custom `IClock`. A domain may still define its own clock port when domain language or portability requires it.

Prefer a narrow baseline that produces high-confidence errors. Project-role-specific analyzer configs can make application code strict without preventing compiler tooling, generators, tests, or infrastructure adapters from doing legitimate work.

### P1: separate baseline dependencies from the capability catalog

`Directory.Packages.props` contains 89 entries spanning API frameworks, databases, every major object-storage provider, telemetry, mail, parsing, testing, and compiler development. They are not runtime dependencies today, but this breadth makes “vanilla baseline” harder to understand and creates permanent update work.

Recommended rule:

1. The central props file contains versions for packages actually referenced by repository projects plus truly universal build analyzers.
2. The documentation catalog may record evaluated candidates without making each candidate a central pin.
3. A project-level adoption decision adds the central pin, the versionless project reference, tests, and an ownership note together.
4. Provider packages remain application/infrastructure choices and never become implicit library dependencies.

If the broad approved catalog is an explicit product requirement, keep it in a separately named imported props file and label it “optional catalog” so its purpose cannot be confused with the minimal build baseline.

### P1: add repository and release governance

Before accepting external or cross-team contributions, add:

- `CONTRIBUTING.md` with bootstrap, build, hook, test, and release instructions;
- `SECURITY.md` with a private vulnerability-reporting route and supported-version policy;
- ownership/review rules for build, package, and security-sensitive files;
- dependency update automation with grouped .NET-family updates and lock-file regeneration;
- a changelog or generated release-note policy;
- a license allowlist and exception process;
- package provenance/signing policy; and
- protected publishing separated from ordinary validation.

### P2: documentation and package hardening

After executable validation exists, add a pinned scheduled link checker, inspect `.nupkg` contents in CI, verify repository/commit metadata and symbols, validate the SBOM semantically, and test at least one consumer project against the packed artifact rather than only using project references.

## Best .NET and C# version for this case

### Recommendation: .NET 10 with C# 14

.NET 10 is the right default for a greenfield baseline in July 2026. Microsoft supports it through 2028-11-14, while .NET 8 support ends on 2026-11-10. See the official [.NET lifecycle table](https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core). C# 14 is the stable language version paired with .NET 10; Microsoft's [C# 14 documentation](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14) confirms that pairing.

Keeping `<LangVersion>14.0</LangVersion>` is acceptable and explicit. Omitting it would also select the language version matching the target framework. Do not select `latest`: Microsoft warns that it makes builds machine-dependent and can enable language features that require runtime/library support absent from the target. See [C# language/version guidance](https://learn.microsoft.com/en-us/dotnet/csharp/versioning).

### Consumer compatibility is the deciding constraint

`net10.0` is correct only if consumers can run on .NET 10. A reusable library cannot make that decision solely from what the authors have installed.

| Consumer requirement | Target recommendation |
| --- | --- |
| New IX services and applications can standardize on .NET 10 | `net10.0` only; simplest and recommended. |
| A known supported product remains on .NET 8 during its final support months | Temporarily multi-target `net8.0;net10.0`, with a dated removal plan. |
| A real .NET Framework or broad third-party library market exists | Evaluate `netstandard2.0` or an additional target from actual API/consumer requirements; expect reduced API surface and more compatibility work. |
| No confirmed older consumer exists | Do not multi-target speculatively. Every target multiplies compilation, test, API-compatibility, and dependency work. |

Make the root `TargetFramework` a conditional default so a source generator, build tool, test fixture, or deliberately multi-targeted library can choose its appropriate TFM without fighting the root property.

## Best C# build system for this case

### Recommendation: .NET CLI plus MSBuild

There are two separate concerns:

- **Compilation/project evaluation:** SDK-style C# projects are MSBuild projects. `dotnet build` is a thin cross-platform wrapper over MSBuild, as Microsoft's [MSBuild documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild) explains.
- **Workflow orchestration:** restore, format, build, test, pack, audit, and SBOM need one dependency graph and a few repository invariants.

The native stack solves both without another build framework. Keep commands visible and debuggable, use `dotnet` consistently, and introduce a small `eng/Build.proj` only for aggregation and invariants. Thin `build.sh` and `build.cmd` wrappers may improve discoverability, but they should only forward arguments to the MSBuild project.

Keep `.slnx`. The [.NET 10 `dotnet sln` documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln) identifies `.slnx` as the default created by `dotnet new sln`.

### Alternatives considered

| Option | Strengths | Costs and risks here | Verdict |
| --- | --- | --- | --- |
| .NET CLI + MSBuild | Already installed with the pinned SDK; cross-platform; IDE-native; no extra runtime package; props/targets are already MSBuild. | XML becomes awkward for a large release/deployment graph. | **Use now.** |
| Bullseye + SimpleExec | Small Apache-2.0 C# console target graph; transparent subprocess execution; easy debugging. Its [official repository](https://github.com/adamralph/bullseye) documents `.slnx` and .NET 8+ support. | Adds a build project and at least two dependencies to orchestrate commands MSBuild can currently express. | Best escalation option if XML orchestration becomes genuinely cumbersome. |
| Cake | Mature, cross-platform C# DSL, .NET tool/Frosting/SDK choices, and a broad add-in ecosystem. See [Cake documentation](https://cakebuild.net/docs/). | Adds a DSL/tool and a larger extension supply chain; excessive for the present graph. | Do not add now. |
| NUKE | Plain C#, typed targets, IDE debugging, and CI generation. See the [NUKE repository](https://github.com/nuke-build/nuke). | Larger scaffold and abstraction; its maintainer has publicly discussed ending or changing the project's direction, creating a poor new-baseline maintenance signal. See the [maintainer discussion](https://github.com/nuke-build/nuke/discussions/1564). | Do not choose for a new baseline now. Reassess only if stewardship becomes clear. |
| FAKE | Capable open-source target DSL. | Build definitions are F#, adding a language/tooling boundary to a vanilla C# repository. See [FAKE](https://fake.build/). | Not a fit. |
| Make/just/Task | Familiar shell-oriented orchestration. | Adds a non-.NET prerequisite and usually creates Windows/shell portability work while still invoking MSBuild underneath. | Do not make canonical. |

Escalation rule: stay on native MSBuild until build orchestration has substantial conditional workflows, parallel targets, or non-.NET deployment operations that are demonstrably hard to maintain. At that point, prototype Bullseye and compare the resulting code and dependency surface against the existing MSBuild project.

## Mediator and dispatcher research

### First decide which problem is being solved

“Dispatcher,” “mediator,” and “message bus” are often grouped together, but the operational guarantees differ:

| Need | Appropriate mechanism |
| --- | --- |
| One endpoint calls one known use-case handler in the same process | Direct constructor injection of that handler/service. |
| Generic in-process request/response with pipelines | A source-generated mediator/dispatcher, selected by the application. |
| In-process fan-out notification | A mediator event mechanism only if best-effort, same-process execution is acceptable. |
| Work must survive process failure or transaction boundaries | Durable queue plus inbox/outbox and idempotency; not an in-process mediator notification. |
| Cross-service command/event delivery | A real message bus/transport framework and versioned contracts. |

The reusable library baseline should define useful APIs and domain contracts, not require all consumers to share a dispatcher. Direct handler injection is the most vanilla, compile-time-safe, testable option and has no package or runtime discovery cost.

### Options as of 2026-07-28

Versions and adoption figures below are a point-in-time NuGet snapshot. Download counts are only a maturity/discovery signal, not an architecture decision.

| Option | License and current package | Model and fit | Recommendation |
| --- | --- | --- | --- |
| No dispatcher | No dependency | Inject a concrete application service or a small `ICommandHandler<TCommand,TResult>` contract directly. No service locator, scanning, reflection, or hidden control flow. | **Default for the baseline and small applications.** |
| FastEndpoints command/event bus | MIT; `FastEndpoints` 8.2.0 is already cataloged | In-process commands, results, streaming, events, and command middleware are built into the selected HTTP framework. See its [command bus documentation](https://fast-endpoints.com/docs/command-bus). | **Use when an application already adopts FastEndpoints.** Do not add a second mediator abstraction without a demonstrated gap. |
| martinothamar Mediator | MIT; `Mediator.SourceGenerator` and `Mediator.Abstractions` 3.0.2; roughly 7.2M/9.4M downloads | Source-generated, AOT-oriented, interface-based requests/commands/queries, notifications, streams, diagnostics, and pipelines. It avoids runtime reflection and is intentionally MediatR-like. See the [official repository](https://github.com/martinothamar/Mediator) and [NuGet package](https://www.nuget.org/packages/Mediator.SourceGenerator/3.0.2). | **Best standalone permissive-OSS shortlist** if generic in-process dispatch is truly needed. Keep the generator private/build-only in the composition project. |
| Immediate.Handlers | MIT; 3.11.1; roughly 193K downloads | Source-generated concrete/nested handlers, compile-time pipeline construction, DI registration, and streaming. See [Immediate.Handlers](https://github.com/ImmediatePlatform/Immediate.Handlers). | Strong alternative for teams that prefer direct generated handler injection and accept its coding model. Pilot before cataloging because it is newer and less widely adopted. |
| Foundatio.Mediator | The [repository](https://github.com/FoundatioFx/Foundatio.Mediator) and package declare Apache-2.0; 1.3.3; roughly 14K downloads | Convention-based handlers, source generation, interceptors, middleware, results, and generated endpoints. The generator enables `InterceptorsPreviewNamespaces` in its [build target](https://github.com/FoundatioFx/Foundatio.Mediator/blob/main/src/Foundatio.Mediator/Foundatio.Mediator.targets). The official [landing-page](https://mediator.foundatio.dev/) footer currently says MIT while the repository/package say Apache-2.0. | Promising but **do not baseline now**: it is young, relies on a preview compiler mechanism, and has license-metadata inconsistency to resolve. |
| Wolverine | MIT; `WolverineFx` 6.23.1; roughly 6.6M downloads; optional paid support | Unified mediator and durable messaging with transports, middleware, retries, persistence, inbox/outbox, and local queues. It has a lighter [mediator-only mode](https://wolverinefx.net/tutorials/mediator.html), but its real value is the broader message-processing model. | Application-only candidate when durable workflows are expected. Too broad for a vanilla in-process dispatcher. |
| Paramore Brighter | MIT; 10.6.0; roughly 3.3M downloads | Explicit command dispatcher/processor, middleware, transports, service activator, and multiple inbox/outbox stores. See the [Brighter repository](https://github.com/BrighterCommand/Brighter). | Application-only candidate for command-centric ports/adapters plus durable messaging. Pairing with its Darker query library increases the abstraction surface. |
| MediatR | RPL-1.5 reciprocal terms or a commercial license; 14.2.0 | Mature request/response, notification, stream, and pipeline API; runtime registration and license-key lifecycle. Its [official FAQ](https://luckypennysoftware.com/faq) describes reciprocal versus commercial use, and the [repository](https://github.com/LuckyPennySoftware/MediatR) documents license-key configuration. | Do not include under the current OSS-first preference. Reconsider only if compatibility/support value justifies legal/procurement approval and an owned exit plan. |

### Dispatcher decision for this repository

1. Do not add any mediator package to the root baseline or to domain/library contracts.
2. For simple use cases, inject a handler or service directly.
3. If the future application uses FastEndpoints, first use its existing command/event bus and generator path.
4. If HTTP-framework independence is required, run a small proof of concept with martinothamar Mediator and Immediate.Handlers. Test generator diagnostics, multi-assembly discovery, scoped dependencies, pipeline ordering, trimming/AOT if relevant, incremental-build time, and debugging.
5. If delivery must be durable, stop comparing in-process mediators and write a separate ADR comparing Wolverine and Brighter against the required broker, database, outbox, retry, observability, and operations model.
6. Never describe an in-process notification as a domain-event delivery guarantee. Persistence and retries must be explicit.

## Other common library candidates

The catalog is already broad. The following commonly encountered packages are worth knowing about, but only a few close actual gaps in the current baseline.

| Capability | Candidate | Decision |
| --- | --- | --- |
| Structured logging | [Serilog](https://github.com/serilog/serilog) | Do not add to reusable libraries. Continue to depend on `Microsoft.Extensions.Logging.Abstractions`; each host chooses Serilog or another provider/sink. |
| JSON | [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) | Do not add by default. Use platform `System.Text.Json`; add Newtonsoft only for a demonstrated compatibility or dynamic-JSON requirement. |
| Direct SQL | [Dapper](https://github.com/DapperLib/Dapper) | Project-only candidate when a module deliberately selects SQL mapping instead of or alongside EF Core. Do not make every library depend on both data-access styles. |
| Object mapping | [`Riok.Mapperly`](https://github.com/riok/mapperly) | Good optional catalog candidate after a real need appears. It is Apache-2.0, source-generated, produces inspectable code, and aligns with the no-runtime-reflection policy better than AutoMapper. |
| Object mapping | [AutoMapper](https://docs.automapper.io/en/latest/License-configuration.html) | Do not add under the default policy: current versions use RPL/commercial licensing as described by the [official licensing FAQ](https://luckypennysoftware.com/faq), and runtime mapping is less aligned with the compile-time baseline. |
| HTTP clients | [Refit](https://github.com/reactiveui/refit) | Project-only. Typed `HttpClient` plus source-generated `System.Text.Json` is sufficient for a vanilla baseline; adopt Refit only when declarative client generation materially reduces adapter code. |
| Test doubles | [NSubstitute](https://github.com/nsubstitute/NSubstitute) | Reasonable BSD-licensed test-only option if the team standardizes on interface substitutes; include its analyzer and prefer simple fakes for domain code. Not universal enough for the baseline. |
| HTTP integration tests | [WireMock.Net](https://github.com/wiremock/WireMock.Net) | Project-only candidate for adapters with significant external HTTP behavior. It has no value in libraries without an HTTP boundary. |
| Database reset | [Respawn](https://github.com/jbogard/Respawn) | Project-only candidate for high-volume integration suites using real relational databases/Testcontainers. Not needed for unit tests or all modules. |
| Test data | [Bogus](https://github.com/bchavez/Bogus) | Project-only. If adopted, seed it explicitly so tests remain reproducible. Builders/mothers are often clearer for small domain models. |
| Snapshot tests | Verify | Useful, but do not add automatically. Its [official repository](https://github.com/VerifyTests/Verify) announces an open-source maintenance fee for official binaries used by commercial/government organizations from August 2026. Review that distribution policy first. |
| Package versioning | [`MinVer`](https://github.com/adamralph/minver) | Add when the first package is built; this closes a real release gap and remains a private build dependency. |
| MTP coverage | [`coverlet.MTP`](https://www.nuget.org/packages/coverlet.MTP/10.0.1) | Add to test projects and replace the VSTest collector as the MTP-first default. MIT. |
| MTP xUnit selection | [`xunit.v3.mtp-v2`](https://www.nuget.org/packages/xunit.v3.mtp-v2/3.2.2) | Use instead of implicit MTP v1 selection in new test projects. Keep legacy runner packages only for a documented compatibility matrix. |

Popularity is not a reason to centralize a dependency. A candidate should close a real capability gap, have an acceptable current license, work with the target/AOT/reflection policy, have a named owner, and include a removal or migration path.

## Recommended dependency policy

Adopt a short written policy before adding more packages:

1. Prefer the BCL and shared framework when they meet the requirement.
2. Prefer direct, explicit code for small abstractions over a framework dependency.
3. Default allowlist: MIT, Apache-2.0, BSD-2/3-Clause, ISC, and PostgreSQL-style permissive licenses.
4. Reciprocal/copyleft packages require legal review and a recorded distribution analysis.
5. Source-available, dual-license, or commercial packages require an ADR, budget owner, license-key/renewal plan, and tested exit strategy.
6. Source generators and analyzers are private assets unless a consumer explicitly needs their compile-time behavior.
7. Runtime library packages expose only the narrowest necessary dependencies to consumers.
8. Every addition records why platform or existing packages were insufficient, its license, maintainer/repository, transitive dependencies, external services, security posture, tests, and upgrade owner.
9. Every upgrade re-evaluates license and ownership changes as well as API compatibility and vulnerabilities.
10. Remove speculative pins that no project has adopted within a defined review window.

## Proposed implementation sequence

### Phase 1: turn policy into an executable baseline

1. Confirm that .NET 10-only consumers are acceptable.
2. Add a conformance library and MTP v2 xUnit test project to the solution.
3. Add a provider-neutral MSBuild `Validate` target with project/test/package-count assertions.
4. Add CI that invokes only that target after provisioning SDK `10.0.302`.
5. Replace the MTP-misaligned xUnit/coverage defaults.
6. Add pinned, managed pre-commit hooks for repository hygiene, Markdown, secrets, spelling, and commit messages. **Completed in the follow-up implementation.**

### Phase 2: make packages releasable

1. Add MinVer and document tagging/release ownership.
2. Generate and inspect real lock files, packages, symbol packages, and SBOMs.
3. Add package content, Source Link/repository metadata, public API, and previous-version compatibility checks.
4. Add contribution, security, license, dependency-update, and publishing policies.

### Phase 3: shrink and validate the optional catalog

1. Move speculative application/provider packages out of the minimal baseline.
2. Benchmark analyzer build cost and keep only demonstrated signal.
3. Scope banned APIs by project role and use `TimeProvider` as the platform clock default.
4. Add Mapperly, WireMock.Net, Respawn, or a dispatcher only when a consuming project and tests justify each choice.

## Acceptance criteria for a finished baseline

The baseline should not be called ready until a clean clone can demonstrate all of the following:

- the exact SDK is provisioned or fails with a useful message;
- local tools and hooks install from pinned definitions;
- locked restore audits a known, non-zero project count;
- format and Release build evaluate representative C# with every intended analyzer;
- MTP discovers and passes a known, non-zero test count;
- coverage uses an OSS MTP-native integration;
- pack creates the expected package and symbol files with correct metadata;
- public API/package validation compares against an intentional baseline where applicable;
- the SBOM contains the expected project and dependency inventory;
- CI invokes the same target developers can run locally;
- commercial/source-available dependencies are absent unless explicitly approved; and
- the optional package catalog is clearly separated from dependencies actually adopted by the repository.

## Final recommendation

Keep the exact .NET 10/C# 14 foundation, `.slnx`, CPM, audit, deterministic packaging settings, and the decision-oriented documentation. Build the repository with native `dotnet`/MSBuild, not a third-party build framework. Add a conformance project, MTP v2-aligned testing, OSS coverage, pre-commit hooks, and authoritative CI before adding runtime libraries.

For dispatch, the baseline answer is **none**. Use direct injection first; use FastEndpoints' built-in bus in a FastEndpoints host; shortlist martinothamar Mediator only for a demonstrated framework-neutral in-process need; and evaluate Wolverine or Brighter only when the requirement is durable messaging. This preserves the user's open-source preference and keeps future libraries free of an application-level architectural dependency.
