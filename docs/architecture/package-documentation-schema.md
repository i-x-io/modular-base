# Package documentation schema

Every future package page in `docs/packages/` uses the following exact heading order. Replace angle-bracket fields with the catalog entry’s actual values. Do not add sections that duplicate this schema; record package-specific detail in the corresponding section.

```md
# <PackageId>

## Catalog entry

## Decision and scope

## Recommended registration and use

## Enterprise implementation guidance

## Integration with the catalog

## Security, performance, AOT, trimming, and operations

## Avoid

## Verification checklist

## Sources
```

## Required content

| Section | Required content |
| --- | --- |
| `Catalog entry` | Exact pinned version and category/role. State whether the package is direct, companion, catalog-only, or a global analyzer. |
| `Decision and scope` | Why it is approved, the intended boundary, and explicit non-goals. |
| `Recommended registration and use` | Correct DI registration and minimal use pattern. If the package has no DI integration, retain the heading and explain direct/API use. |
| `Enterprise implementation guidance` | Configuration, lifecycle, error handling, data ownership, and deployment guidance relevant to this package. |
| `Integration with the catalog` | Cross-reference related pages in `docs/packages/` and describe the integration boundary. |
| `Security, performance, AOT, trimming, and operations` | Relevant constraints; say “Not applicable” only after checking the package. |
| `Avoid` | Unsupported, risky, or out-of-scope usage. |
| `Verification checklist` | Concrete package-level checks, including tests or commands where applicable. Do not present unrun commands as verified. |
| `Sources` | Direct primary links and the literal date `Accessed 2026-07-27`. |

## Writing rules

- Document the exact version in `Directory.Packages.props`; do not substitute a newer version from vendor documentation.
- Keep examples compatible with the repository’s `net10.0` and C# `14.0` baseline and its central-package policy: project `PackageReference` entries omit `Version`.
- Mark catalog-only entries as not approved for automatic project consumption.
- Identify analyzer entries as build-time only and note `PrivateAssets="all"` where relevant.
- Test every command and code example against a representative project before calling it verified. The repository's architecture-test project validates repository governance only; it is not evidence that package integration examples compile or behave correctly.
