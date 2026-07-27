# DeviceDetector.NET

## Catalog entry

`DeviceDetector.NET` **6.5.0** — direct catalog package; user-agent parser for client, device, operating system, brand, and model classification.

## Decision and scope

Use only where coarse client classification has a concrete product or operational purpose. User-agent output is advisory metadata, never an identity, authorization, or security signal.

## Recommended registration and use

Parse the request's user-agent once at the edge and pass a small classified result downstream. For repeated values, share the package cache or use its bounded LRU cache; configure cache limits during startup before use.

## Enterprise implementation guidance

Normalize the absence of a user-agent and keep unknown results first-class. Bound cache cardinality and TTL, record cache hit/miss and parse latency, and treat retained user-agent strings as potentially personal data under the service's privacy policy.

## Integration with the catalog

Use `microsoft-extensions-resilience.md` only if device data comes from an explicit remote dependency; do not add resilience to local parsing. Use `fluentresults.md` when parsing is part of an optional enrichment workflow.

## Security, performance, AOT, trimming, and operations

The shared regex/parse cache prevents repeated work but can grow with a high-cardinality header stream. Do not persist the cache without an approved retention and storage policy. AOT/trimming support is not documented by the package; validate the production publish artifact.

## Avoid

Do not authorize, fingerprint, or permanently personalize users from this value. Do not create a detector per request with an unbounded cache configuration.

## Verification checklist

- Test representative desktop, mobile, bot, malformed, and absent headers.
- Load-test unique-header traffic and verify cache size/eviction.
- Verify privacy logging does not emit raw user-agent strings unnecessarily.

## Sources

- https://www.nuget.org/packages/DeviceDetector.NET/6.5.0 (Accessed 2026-07-27)
- https://github.com/totpero/DeviceDetector.NET (Accessed 2026-07-27)
