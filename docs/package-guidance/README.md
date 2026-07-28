# Cross-cutting package guidance

These references complement the one-package-at-a-time guides in the
[`docs/packages`](../packages/README.md) catalog:

- [Package selection](package-selection.md) identifies the package that owns a
  registration or runtime concern and the combinations the catalog supports.
- [Supply-chain reference](supply-chain.md) records exact-version, source-backed
  identity, dependency, lifecycle, advisory, and provenance facts for every
  external catalog entry.

`Directory.Packages.props` remains the source of truth for package IDs and pinned
versions. A central version does not add or approve a dependency: each consuming
project decides what it needs and adds a versionless `PackageReference`. These
pages help with that decision but do not replace the package-specific guidance.

Research was last accessed on **2026-07-27**. Context7 was consulted first for
each package family, but its service returned `Monthly quota exceeded` before any
library could be resolved. The references therefore use exact-version NuGet
metadata and official Microsoft, vendor, or upstream project sources. A fact that
could not be established from those sources is labeled **not officially
documented** rather than inferred.
