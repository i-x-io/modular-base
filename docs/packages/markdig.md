# Markdig

## Catalog entry

`Markdig` **1.3.2** — centrally pinned Markdown parser for repository documentation validation and other deliberate documentation-processing boundaries.

- **Adoption:** Direct
- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** Review when the Markdig pin, enabled pipeline extensions, or the repository Markdown schema/link rules change.

## Decision and scope

Use Markdig to parse Markdown structurally where regular expressions would be fragile, including local-link and documentation-schema validation. It is not a general-purpose renderer or an application content-management endorsement.

## Recommended registration and use

Reference it versionlessly only from a documentation-validation or other approved processing project. Parse Markdown into its documented model, resolve local paths beneath the repository root, and treat external URLs as external references rather than local files.

| Pipeline choice | Catalog guidance |
| --- | --- |
| CommonMark/default pipeline | Prefer for validation that needs only standard Markdown structure. |
| Individual `Use...` extensions | Enable only syntax the governed documents use; keep `UseGenericAttributes()` last because it modifies other parsers. |
| `UseAdvancedExtensions()` | Treat as a compatibility bundle whose membership can change; cover every relied-on extension with fixtures. |
| `DisableHtml()` | Use when raw HTML is outside the accepted grammar, but do not treat it as a complete HTML-sanitization boundary. |

## Enterprise implementation guidance

Validate only governed documentation roots, reject paths that escape the repository, and provide failures naming the source document and target. Keep parsing deterministic and avoid executing embedded content or fetching remote links during tests.

### Upgrade and rollback

Before changing the pin, parse the repository fixture corpus with both versions and compare headings, links, trivia, source spans, and extension-specific nodes used by validators. Re-run malicious path and raw-HTML fixtures; parser success alone does not prove safe rendering. If the new version changes the syntax tree or accepted grammar unexpectedly, restore `1.3.2` and its lock-file resolution, then reduce the mismatch to a focused fixture before retrying. Do not preserve two parser paths or silently fall back to regex parsing.

## Integration with the catalog

`Directory.Packages.props` owns version `1.3.2`. The architecture documentation suite uses it to validate documentation links and catalog/guide ownership; no application example or public rendering contract is created by this catalog entry. Review its [supply-chain record](../package-guidance/supply-chain.md#markdig) before changing the pin.

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
