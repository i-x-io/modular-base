# Documentation index

This documentation governs the executable `IX.Modularity.*` library baseline.
It includes illustrative package-composition examples, but it is not an
application tutorial.

## Documentation branches

- [Current baseline implementation report](baseline-implementation-report.md) — implemented controls, NUKE/C# decisions, hook and library research, and remaining work.
- [Development workflow](development-workflow.md) — branches, commits, hooks, NUKE targets, pull requests, dependencies, and releases.
- [GitHub governance](github-governance.md) — workflows, rulesets, App credentials, packages, settings, reconciliation procedure, and current verified state.
- [Original vanilla C# assessment](vanilla-csharp-baseline-assessment.md) — historical pre-implementation audit and package/dispatcher research; superseded where the project explicitly selected NUKE.
- [Package catalog](packages/README.md) — central package entries and package-specific decision guides.
- [Package guidance](package-guidance/README.md) — package selection, ownership boundaries, and objective supply-chain facts.
- [Composition recipes](recipes/README.md) — explained, multi-package workflows using the centrally pinned catalog.

## Common tasks

- **Select a package:** start with the [package-selection guide](package-guidance/package-selection.md), then read the package-specific guide from the [catalog](packages/README.md).
- **Add or upgrade a package:** update the pin in [`Directory.Packages.props`](../Directory.Packages.props), then keep the catalog entry, package guide, and supply-chain record synchronized.
- **Review a package:** use the [supply-chain reference](package-guidance/supply-chain.md) alongside the package-specific guide to assess identity, dependencies, lifecycle, advisories, and provenance.
