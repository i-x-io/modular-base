# Package documentation expansion design

## Purpose

Expand the repository's package documentation beyond package-level setup into a
maintainable operating reference. The result must help readers choose compatible
packages, configure and operate runtime dependencies, troubleshoot meaningful
failures, upgrade individual packages, understand supply-chain properties, and
compose approved packages through illustrated workflows.

The work applies to the package catalog present in the working tree when
implementation begins. The current uncommitted catalog and analyzer changes are
an intended part of that baseline.

## Decisions

- Keep the exact nine-section schema for every page in `docs/packages/`.
- Add package-specific information inside the existing sections; do not add new
  top-level headings to package pages.
- Put cross-package decisions and complete workflows in separate documents.
- Create all ten agreed recipes as Markdown documentation with focused code
  examples and explanations. Do not add permanent sample applications.
- Keep verification reporting simple. State that an example compiled or ran
  only when that check was actually performed; otherwise leave the verification
  checklist as the required work for a consumer.
- Add troubleshooting only for meaningful runtime or external-service failure
  modes. Do not manufacture troubleshooting content for abstractions, analyzers,
  or build-only packages.
- Store objective supply-chain metadata in one catalog-wide reference rather
  than repeating it in all package guides.
- Record package-specific upgrade guidance in every package guide.
- Record `IX` as the documentation owner.
- Do not implement a permanent documentation validator.
- Commit the completed work, including the pre-existing uncommitted baseline
  changes if they still exist at final integration.

## Information architecture

### Package pages

The implementation baseline contains 89 external catalog packages (84
`PackageVersion` entries and five `GlobalPackageReference` entries) plus the
repository-produced `IX.Modularity.Analyzers` package, for 90 package pages in
total. These pages remain the authoritative package-specific references. Each
page continues to use this exact H2 order:

1. `Catalog entry`
2. `Decision and scope`
3. `Recommended registration and use`
4. `Enterprise implementation guidance`
5. `Integration with the catalog`
6. `Security, performance, AOT, trimming, and operations`
7. `Avoid`
8. `Verification checklist`
9. `Sources`

New information is placed as paragraphs, tables, or H3 subsections inside the
appropriate existing section.

Every package page will include lightweight freshness metadata:

- Owner: `IX`.
- Last reviewed: `2026-07-27`.
- Review trigger: package-version change, target-framework change, or a relevant
  upstream/platform change.

Every package page will also include package-specific upgrade guidance. Relevant
guidance includes changed defaults, deprecated APIs, target-framework
requirements, companion packages that should move together, data or migration
effects, deployment sequencing, and rollback considerations. When a concern is
not applicable, the guide should say so directly rather than inventing steps.

Configuration-heavy packages will receive compact reference tables covering the
setting, purpose, default or default behavior, production guidance, reload
behavior, sensitivity, and failure behavior. Only settings that materially
affect supported workflows belong in these tables; the guides are not intended
to duplicate full vendor API references.

Runtime packages will document operational signals where those signals are
supported by primary sources or established instrumentation. This may include
log categories, metrics, spans, health behavior, saturation signals, retry or
circuit state, and fields that must not be recorded. Packages without meaningful
runtime signals will not receive boilerplate signal tables.

Troubleshooting content is limited to meaningful runtime and external-service
failure modes. Each entry should connect a symptom to likely causes, useful
diagnostics, safe corrective action, and retry suitability. The guidance must
not recommend disabling TLS, suppressing validation, logging secrets, broadly
retrying unsafe operations, or hiding failures.

### Package-selection guide

A cross-package decision guide will explain boundaries and valid combinations
for overlapping families:

- `Microsoft.Extensions.Http.Resilience` versus direct Polly pipelines.
- Microsoft Testing Platform versus VSTest, runners, and coverage tooling.
- EF Core InMemory versus PostgreSQL Testcontainers.
- Direct Npgsql access versus `Npgsql.EntityFrameworkCore.PostgreSQL`.
- FastEndpoints security conveniences versus ASP.NET Core JWT bearer.
- OpenTelemetry API, SDK, hosting, instrumentation, and exporter packages.
- FluentStorage abstractions and providers versus provider-native SDKs.
- Microsoft abstraction packages versus concrete runtime implementations.

Each decision table identifies the package that owns registration or runtime
behavior, appropriate direct references, valid combinations, selection criteria,
and common misuse. The guide links back to package pages rather than duplicating
their full setup instructions.

### Illustrated recipes

Create ten recipe documents:

1. FastEndpoints, JWT bearer authentication, OpenAPI, and Scalar.
2. FluentValidation, FluentResults, and FastEndpoints request processing.
3. EF Core, Npgsql, naming conventions, and PostgreSQL exception mapping.
4. Pgvector similarity search and hybrid ranking.
5. OpenTelemetry traces, metrics, runtime instrumentation, Npgsql, and OTLP.
6. A resilient typed `HttpClient` with one retry owner.
7. PostgreSQL and Redis integration tests with Testcontainers and xUnit v3.
8. A durable mail-outbox worker using MimeKit and MailKit.
9. Portable FluentStorage upload and download workflows with provider selection.
10. Options binding, startup validation, reload, and health reporting.

Every recipe contains:

- The problem and intended boundary.
- Required catalog packages using versionless `PackageReference` entries.
- A focused composition or workflow example compatible with `net10.0` and C# 14.
- A direct explanation after each meaningful code block describing control flow,
  package ownership, ordering, error handling, security, and production changes.
- Failure modes and operational observations.
- A verification checklist that distinguishes checks run during authoring from
  checks the consuming application must perform.
- Links to the relevant package pages and primary sources.

Examples remain illustrative documentation. Temporary projects may be used to
compile or exercise them, but no permanent sample projects are added to the
repository.

### Supply-chain reference

Create one catalog-wide supply-chain reference containing objective,
source-backed metadata for all 89 external catalog packages plus a separate
repository-controlled entry for the produced `IX.Modularity.Analyzers` package:

- Package ID and pinned version.
- License.
- Publisher or maintainer.
- Upstream repository.
- Approved NuGet source.
- External services and runtime dependencies.
- Native dependencies, where applicable.
- Officially established status such as active, archived, deprecated, or
  prerelease.
- Security or advisory references.
- Signing or provenance information when officially available.

Do not invent numeric health scores or infer maintenance status from subjective
signals. If a fact cannot be established from a primary source, record it as not
officially documented rather than guessing. Package pages link to this reference
where it materially helps selection or operations.

### Navigation

Update the repository's documentation navigation so readers can discover the
package-selection guide, recipe collection, and supply-chain reference from the
package index and the root documentation entry points. Preserve the existing
one-to-one package index and do not make cross-cutting pages look like additional
catalog package entries.

## Research policy

Research each package family with Context7 first. Resolve the official library
identifier before querying its documentation. Verify material claims using
official documentation, exact-version NuGet metadata, upstream repositories or
tagged source, and vendor operational documentation.

`Directory.Packages.props` in the implementation working tree remains the sole
authority for package IDs and pinned versions. Newer vendor documentation must
not silently change the documented API surface. Version-sensitive examples must
be checked against the pinned package or clearly qualified.

Supply-chain, compatibility, security, and breaking-change claims require
primary sources. Current web research is required because these facts can change
over time.

## Verification strategy

Verification is proportional to the documentation change and does not add a
permanent validator.

- Confirm catalog, index, and guide parity against the implementation baseline.
- Confirm the exact nine-section schema for every package page.
- Confirm pinned versions and versionless project package references.
- Check balanced code fences, freshness metadata, descriptive sources, and local
  Markdown links.
- Compile recipes and materially changed examples in temporary `net10.0`
  projects where practical.
- Exercise runtime behavior only when it can be done deterministically without
  external credentials or persistent infrastructure.
- Do not call an example runtime-tested, integration-tested, or AOT-tested unless
  that exact check was observed.
- Run `git diff --check`, `make validate`, `make build`, and `make test`.
- Run an independent complete-diff review and a final acceptance verification.

External-service behavior such as SMTP delivery, cloud storage, PostgreSQL query
plans, Redis failover, and production telemetry export remains a consumer
integration responsibility unless a deterministic local test is explicitly run.

## Change safety

- Preserve the existing uncommitted `.editorconfig`, central catalog,
  global-analyzer configuration, package index, `.omx`, and analyzer-test work.
- Treat those changes as the intended baseline and reconcile documentation with
  them rather than reverting them.
- Avoid unrelated source, dependency, formatting, and architecture changes.
- Keep parallel file ownership disjoint during implementation.
- Do not expose credentials or use real secrets in examples.
- Do not push, publish, deploy, or open a pull request.
- At final integration, include the pre-existing baseline changes in the commit
  if they still exist, as explicitly requested.

## Acceptance criteria

- All catalog package pages retain the required nine-section schema.
- Every package page contains the agreed owner, review date, review trigger, and
  package-specific upgrade guidance.
- Relevant package pages contain useful configuration tables, operational
  signals, and runtime troubleshooting without boilerplate on inapplicable pages.
- The cross-package decision guide covers all eight agreed comparison areas.
- All ten recipes exist, include code, explain the code, and link to package pages.
- The supply-chain reference covers every package in the implementation catalog
  using objective primary-source facts and no invented health score.
- Documentation navigation exposes all new cross-cutting material without
  disturbing package-index parity.
- Research uses Context7 first and current primary web sources afterward.
- Repository and documentation checks pass, independent review has no unresolved
  blocking finding, and limitations are reported honestly.
- The final work is committed locally and not pushed.

## Non-goals

- A permanent documentation-validation script or CI target.
- Permanent sample applications or new production projects.
- Exhaustive duplication of vendor configuration or API references.
- Subjective package scoring.
- Package upgrades that are not already part of the intended working-tree
  baseline.
- Publishing, deployment, pushing, or pull-request creation.
