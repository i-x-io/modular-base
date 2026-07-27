# Architecture policy index

This directory is the normative design and delivery policy for future `IX.Modularity.*` libraries. It describes library boundaries and compiler-tooling packages; it does not prescribe application examples, deployment topology, or an application composition root.

## Documents

- [Analyzer index](analyzer-index.md) — diagnostic navigation.
- [Analyzer policy](analyzer-policy.md) — roles, distribution, severity, and suppression.
- [Analyzer taxonomy](analyzer-taxonomy.md) — authoritative diagnostic contract.
- [Architectural rules](architectural-rules.md) — mandatory boundaries, dependency direction, and compatibility.
- [Boundaries and dependencies](boundaries-and-dependencies.md) — ports, adapters, and dependency ownership.
- [Build and SBOM](build-and-sbom.md) — Makefile-only public build interface and artifact provenance.
- [Code quality policy](code-quality-policy.md) — compiler, analyzer, API, and source-quality rules.
- [Dependency policy](dependency-policy.md) — central package management and dependency approval.
- [Design principles](design-principles.md) — cohesion, SRP, and authoritative values.
- [Documentation, testing, and quality](documentation-testing-and-quality.md) — contract documentation and verification.
- [Domain modeling](domain-modeling.md) — optional semantic-model boundaries.
- [IXM1001 diagnostic](diagnostics/ixm1001.md) — data-object documentation.
- [IXM1002 diagnostic](diagnostics/ixm1002.md) — interface documentation.
- [IXM1003 diagnostic](diagnostics/ixm1003.md) — interface-member documentation.
- [IXM1004 diagnostic](diagnostics/ixm1004.md) — service-type documentation.
- [IXM1005 diagnostic](diagnostics/ixm1005.md) — service-member documentation.
- [IXM2001 diagnostic](diagnostics/ixm2001.md) — record suggestion.
- [IXM3001 diagnostic](diagnostics/ixm3001.md) — FluentResults service-operation contract.
- [IXM3002 diagnostic](diagnostics/ixm3002.md) — coded business failure.
- [IXM3003 diagnostic](diagnostics/ixm3003.md) — broad-catch rethrow.
- [Library public API and evolution](library-public-api-and-evolution.md) — compatibility and baselines.
- [Observability and operability](observability-and-operability.md) — structured signals and source-generated logging.
- [Package documentation schema](package-documentation-schema.md) — required guide structure for catalogued packages.
- [Performance and resource management](performance-and-resource-management.md) — spans, memory, and benchmarks.
- [Project structure](project-structure.md) — project roles, adaptive layout, and allowed references.
- [Terminology](terminology.md) — stable linked vocabulary.
- [Type system and data modeling](type-system-and-data-modeling.md) — records, nullability, and ownership.

All primary references in these documents were accessed on **2026-07-27**.
