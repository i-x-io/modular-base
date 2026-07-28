# Documentation index

This documentation governs the future `IX.Modularity.*` library repository. It includes illustrative package-composition examples, but it is not an application tutorial and does not add permanent sample applications or projects.

## Documentation branches

- [Vanilla C# baseline assessment](vanilla-csharp-baseline-assessment.md) — current-state audit, missing engineering controls, build-system recommendation, and open-source dispatcher/library research.
- [Package catalog](packages/README.md) — central package entries and package-specific decision guides.
- [Package guidance](package-guidance/README.md) — package selection, ownership boundaries, and objective supply-chain facts.
- [Composition recipes](recipes/README.md) — explained, multi-package workflows using the centrally pinned catalog.

## Common tasks

- **Select a package:** start with the [package-selection guide](package-guidance/package-selection.md), then read the package-specific guide from the [catalog](packages/README.md).
- **Add or upgrade a package:** update the pin in [`Directory.Packages.props`](../Directory.Packages.props), then keep the catalog entry, package guide, and supply-chain record synchronized.
- **Review a package:** use the [supply-chain reference](package-guidance/supply-chain.md) alongside the package-specific guide to assess identity, dependencies, lifecycle, advisories, and provenance.
