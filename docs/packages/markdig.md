# Markdig

## Catalog entry

`Markdig` **1.3.2** — centrally pinned Markdown parser for repository documentation validation and other deliberate documentation-processing boundaries.

## Decision and scope

Use Markdig to parse Markdown structurally where regular expressions would be fragile, including local-link and documentation-schema validation. It is not a general-purpose renderer or an application content-management endorsement.

## Recommended registration and use

Reference it versionlessly only from a documentation-validation or other approved processing project. Parse Markdown into its documented model, resolve local paths beneath the repository root, and treat external URLs as external references rather than local files.

## Enterprise implementation guidance

Validate only governed documentation roots, reject paths that escape the repository, and provide failures naming the source document and target. Keep parsing deterministic and avoid executing embedded content or fetching remote links during tests.

## Integration with the catalog

`Directory.Packages.props` owns version `1.3.2`. The architecture documentation suite uses it to validate documentation links and catalog/guide ownership; no application example or public rendering contract is created by this catalog entry.

## Security, performance, AOT, trimming, and operations

Treat Markdown as untrusted input when a future consumer parses external content. Parsing repository documents is local, bounded work; do not convert parsed links into network requests. Evaluate rendering, HTML sanitization, trimming, and AOT needs separately if a future package exposes them.

## Avoid

Do not use regexes as a substitute for structural Markdown parsing, follow links outside the repository root, fetch external URLs in validation, or add it to a runtime package without an explicit content-processing boundary.

## Verification checklist

- [ ] The reference is versionless and centrally resolves `1.3.2`.
- [ ] Validation rejects a missing local Markdown target and a path escape.
- [ ] External URLs are not treated as local-file failures or fetched.

## Sources

- [Markdig documentation](https://github.com/xoofx/markdig) — Accessed 2026-07-27.
- [Markdig 1.3.2 on NuGet](https://www.nuget.org/packages/Markdig/1.3.2) — Accessed 2026-07-27.
