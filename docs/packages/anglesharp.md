# AngleSharp

## Catalog entry

`AngleSharp` **1.6.0** — direct catalog package; standards-oriented HTML, SVG, MathML, and CSS DOM parser. The catalog owns the version for `net10.0` projects using C# 14.

- **Owner:** IX
- **Last reviewed:** 2026-07-27
**Review trigger:** `AngleSharp` version changes, target-framework changes, or upstream parser/loader behavior changes.

## Decision and scope

Use for server-side parsing, standards-based CSS-selector queries, and controlled transformation of markup. It is a parser, not a browser security boundary, JavaScript runtime, or HTML sanitizer. Prefer `HtmlParser` for an in-memory HTML string; use an `IBrowsingContext` when the workflow needs a document address, navigation, cookies, or deliberately configured resource loading.

## Recommended registration and use

With Central Package Management already enabled, add a versionless reference to the consuming project:

```xml
<ItemGroup>
  <PackageReference Include="AngleSharp" />
</ItemGroup>
```

For the common parse-query-project workflow, reuse the parser, keep loading disabled, and return immutable application data rather than the DOM:

```csharp
using AngleSharp.Html.Parser;

var parser = new HtmlParser();
var document = await parser.ParseDocumentAsync(
    "<main><a class='product' href='/p/42'>Blue mug</a></main>");

var products = document.QuerySelectorAll("a.product")
    .Select(link => new ProductLink(
        link.TextContent.Trim(),
        link.GetAttribute("href") ?? string.Empty))
    .ToArray();

public sealed record ProductLink(string Name, string Href);
```

Create the parser or `IBrowsingContext` once per compatible configuration and reuse it where the application's concurrency tests permit. If remote navigation is required, construct a separate configuration with `WithDefaultLoader(...)`; resource loading is an explicit capability and should not be enabled for ordinary string parsing.

## Enterprise implementation guidance

Put parsing behind an application service that accepts a bounded input and returns a purpose-built projection. Set request/body limits before buffering, apply cancellation and an execution timeout at the service boundary, and decide how malformed documents, missing selectors, character encodings, and relative URLs are reported. When remote navigation is enabled, use an allowlist for schemes and destinations, block loopback/link-local/private network ranges as appropriate, cap redirects and response bytes, and emit fetch latency and failure metrics without recording document contents.

### Configuration reference

| Setting | Purpose | Default behavior | Production guidance | Reload | Sensitive | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| `HtmlParserOptions` | Controls parser compatibility and source tracking | Standards-oriented defaults | Create one reviewed profile per input contract | Rebuild parser/context | No | Changes DOM shape or diagnostics |
| `WithDefaultLoader(LoaderOptions)` | Enables navigation/resource loading | No loader in the ordinary parser path | Enable only in a separate SSRF-controlled fetch boundary | Rebuild browsing context | Destinations can be sensitive | Navigation/resource requests fail or are unavailable |
| `LoaderOptions.IsResourceLoadingEnabled` | Allows subresource requests | `false` unless explicitly enabled | Keep disabled unless the workflow requires subresources | Rebuild browsing context | Yes, requested URLs | Missing subresources; parsing still yields a document |

### Upgrade and rollback

Before upgrading, compare parser and loader migration notes, then regression-test malformed-markup, selector, encoding, and URL-resolution fixtures. If loaders are enabled, also verify redirect and SSRF controls. Roll back the central pin and redeploy together if DOM shape or projections change; no persistent migration is required.

## Integration with the catalog

Use `fluentresults.md` for expected parse or projection failures in application workflows and `polly.md` only around explicit, bounded remote fetches. Keep validation of the projected data separate from DOM parsing.

See the [`AngleSharp` supply-chain entry](../package-guidance/supply-chain.md#anglesharp).

## Security, performance, AOT, trimming, and operations

Treat markup and externally loaded content as untrusted; parsing does not make HTML safe to render, and selector results may contain dangerous URLs or text. Keep the default no-loader path for untrusted input to avoid SSRF. Bound input size, DOM complexity, and concurrent parses; malformed-but-valid HTML can still consume substantial CPU and memory. Disable source references when source locations are unnecessary. The package does not make a catalog-level NativeAOT/trimming guarantee, so publish and exercise parsing, selector, and serialization paths in the target mode before release.

Operational signals should include input-size buckets, parse latency, projection/missing-selector counts, and—only for enabled loaders—fetch duration, redirect count, response bytes, and destination class. Never record source markup, cookies, authorization headers, query strings, or full fetched URLs.

### Troubleshooting

| Symptom | Likely cause | Diagnostic | Correction | Retry? |
| --- | --- | --- | --- | --- |
| Expected nodes are absent | Malformed input, encoding, namespace, or selector mismatch | Capture redacted fixture identity and inspect parser errors/DOM shape in a safe test | Correct selector/encoding policy or tolerate the documented absence | No |
| Remote document/resource fails | Loader disabled, blocked destination, redirect/size/timeout limit | Inspect bounded fetch status and allowlist decision | Fix approved loader policy or dependency endpoint | Only transient fetch failures within budget |
| Parse CPU/memory spikes | Oversized or adversarial markup | Compare input-size, DOM-count, latency, and allocation metrics | Reject earlier and lower concurrency/size/complexity limits | No |

## Avoid

Do not enable arbitrary URL loading for attacker-controlled documents, use AngleSharp as a sanitizer, treat DOM-derived URLs as trusted, or parse unbounded request bodies synchronously in a request path.

## Verification checklist

- [ ] The consuming project has a versionless `PackageReference`, and the resolved version is `1.6.0` from the central catalog.
- [ ] Representative valid, malformed, differently encoded, and oversized inputs produce the expected projection or bounded failure.
- [ ] Missing attributes/selectors and relative URLs follow an explicit application policy.
- [ ] Any loader configuration enforces destination, redirect, byte, timeout, and cancellation limits, including SSRF regression cases.
- [ ] Publish-mode smoke tests cover parsing and selectors when trimming or NativeAOT is enabled.

## Sources

- [AngleSharp 1.6.0 on NuGet](https://www.nuget.org/packages/AngleSharp/1.6.0) (Accessed 2026-07-27)
- [AngleSharp official repository](https://github.com/AngleSharp/AngleSharp) (Accessed 2026-07-27)
- [AngleSharp parsing documentation](https://github.com/AngleSharp/AngleSharp/wiki/Documentation) (Accessed 2026-07-27)
- [AngleSharp configuration and loading documentation](https://github.com/AngleSharp/AngleSharp/blob/devel/docs/tutorials/06-Questions.md) (Accessed 2026-07-27)
