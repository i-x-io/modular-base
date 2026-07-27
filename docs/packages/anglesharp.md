# AngleSharp

## Catalog entry

`AngleSharp` **1.6.0** — direct catalog package; standards-oriented HTML, SVG, MathML, and CSS DOM parser.

## Decision and scope

Use for server-side parsing, querying, and controlled transformation of markup. It is a parser, not a browser security boundary or HTML sanitizer.

## Recommended registration and use

Create a parser or `IBrowsingContext` suitable for the required feature set, then query the resulting DOM with standard selectors. Reuse parser/configuration objects where the workload permits. Enable a loader only when the operation truly needs external resource fetching.

## Enterprise implementation guidance

Keep parsing behind an application service with input-size and execution limits. Define an explicit outbound-network policy before using a loader. Return a purpose-built projection, rather than handing a mutable DOM across layers.

## Integration with the catalog

Use `fluentresults.md` for expected parse failures in application workflows and `polly.md` only around explicit, bounded remote fetches.

## Security, performance, AOT, trimming, and operations

Treat input and any externally loaded content as untrusted; parsing does not make HTML safe to render. Disable source references when source locations are not needed to reduce retained data. The catalog makes no AOT/trimming compatibility claim: publish and exercise the intended parsing path in the target mode.

## Avoid

Do not enable arbitrary URL loading for attacker-controlled documents, use it as an HTML sanitizer, or parse unbounded request bodies synchronously in a request path.

## Verification checklist

- Parse representative valid and malformed markup and assert the selected DOM projection.
- Test the configured network policy and maximum input limit.
- Run the publish-mode smoke test when trimming or NativeAOT is enabled.

## Sources

- https://www.nuget.org/packages/AngleSharp/1.6.0 (Accessed 2026-07-27)
- https://github.com/AngleSharp/AngleSharp (Accessed 2026-07-27)
- https://github.com/AngleSharp/AngleSharp/wiki/Documentation (Accessed 2026-07-27)
