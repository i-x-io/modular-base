# DeviceDetector.NET

## Catalog entry

`DeviceDetector.NET` **6.5.0** — direct catalog package; user-agent parser for client, device, operating system, brand, and model classification. The catalog owns the version for `net10.0` projects using C# 14.

## Decision and scope

Use only where coarse client classification has a concrete product or operational purpose, such as analytics segmentation or optional UX hints. User-agent output is incomplete and spoofable: it is advisory metadata, never an identity, authorization, anti-fraud, or security signal.

## Recommended registration and use

With Central Package Management already enabled, add a versionless reference to the consuming project:

```xml
<ItemGroup>
  <PackageReference Include="DeviceDetector.NET" />
</ItemGroup>
```

For the common edge-enrichment workflow, normalize the header, parse once, and pass only the small classification downstream:

```csharp
using DeviceDetectorNET;

const string userAgent =
    "Mozilla/5.0 (Linux; Android 13; Mobile) AppleWebKit/537.36 Chrome/120.0";
var detector = new DeviceDetector(userAgent);
detector.Parse();

var client = detector.GetClient().Match;
var os = detector.GetOs().Match;

var classification = new ClientClassification(
    detector.GetDeviceName(),
    detector.GetBrandName(),
    detector.GetModel(),
    client?.Name,
    os?.Name,
    detector.IsBot());

public sealed record ClientClassification(
    string Device, string Brand, string Model,
    string? Client, string? OperatingSystem, bool IsBot);
```

Treat an absent or malformed header as an unknown classification. For repeated user agents, use `LRUCachedDeviceDetector.GetDeviceDetector(userAgent)` and configure `DeviceDetectorSettings.LRUCacheMaxSize`, `LRUCacheCleanPercentage`, and `LRUCacheMaxDuration` once during startup, before the first parse. Include Client Hints only when the application already collects them under an approved privacy policy.

## Enterprise implementation guidance

Centralize parsing in middleware or an edge adapter so requests are classified once. Define a stable internal vocabulary that includes `unknown`; do not expose package-specific result objects to domain code. Bound the header length before parsing, bound cache size and TTL, and record aggregate parse latency, unknown rate, bot rate, and cache behavior. Review retention, consent, and data-subject requirements before storing raw user agents or Client Hints, because combinations can contribute to fingerprinting.

## Integration with the catalog

Use `microsoft-extensions-resilience.md` only if device data comes from an explicit remote dependency; local parsing needs no retry policy. Use `fluentresults.md` when classification is optional enrichment whose failure should not fail the request.

## Security, performance, AOT, trimming, and operations

High-cardinality attacker-controlled headers can create CPU pressure and cache churn. Enforce the web server's header limits, prefer a bounded LRU cache for long-lived services, and never use a persistent cache without approved retention, access, and cleanup controls. Pin package upgrades and regression-test the classification corpus because regex data changes can alter results without application code changes. AOT/trimming support is not documented as a package guarantee; validate parsing and embedded regex resources in the production publish artifact.

## Avoid

Do not authorize, fingerprint, permanently personalize, or make accessibility-critical decisions from detected values. Do not log raw headers by default, create a detector multiple times per request, configure global cache settings after traffic begins, or assume an unfamiliar agent is malicious.

## Verification checklist

- [ ] The consuming project has a versionless `PackageReference`, and the resolved version is `6.5.0` from the central catalog.
- [ ] Desktop, mobile, tablet, bot, malformed, oversized, spoofed, and absent-header fixtures map to the intended internal vocabulary.
- [ ] Unique-header load tests verify parse latency, cache maximum size, TTL, eviction, and memory behavior.
- [ ] Logs, traces, analytics, and persistent stores do not retain raw user-agent or Client Hint data without approval.
- [ ] A published `net10.0` artifact successfully loads regex resources and runs representative classifications.

## Sources

- [DeviceDetector.NET 6.5.0 on NuGet](https://www.nuget.org/packages/DeviceDetector.NET/6.5.0) (Accessed 2026-07-27)
- [DeviceDetector.NET official usage guide](https://github.com/totpero/DeviceDetector.NET#usage) (Accessed 2026-07-27)
- [DeviceDetector.NET official documentation](https://totpero.github.io/DeviceDetector.NET/) (Accessed 2026-07-27)
- [DeviceDetector.NET cache implementation](https://github.com/totpero/DeviceDetector.NET/tree/master/src/DeviceDetector.NET/Cache) (Accessed 2026-07-27)
- [DeviceDetector.NET global settings](https://github.com/totpero/DeviceDetector.NET/blob/master/src/DeviceDetector.NET/DeviceDetectorSettings.cs) (Accessed 2026-07-27)
