# Humanizer.Core

## Catalog entry

`Humanizer.Core` **3.0.10** — direct catalog package; human-readable transformations for strings, dates, quantities, enums, and numbers. The catalog owns the version for `net10.0` projects using C# 14.

## Decision and scope

Use for user-facing display text close to the presentation boundary. It is not a formatting standard for logs, database values, URLs, cache keys, API payloads, or other machine contracts. `Humanizer.Core` provides the library and neutral English resources; verify package/resource choices before promising other locales.

## Recommended registration and use

With Central Package Management already enabled, add a versionless reference to the consuming project:

```xml
<ItemGroup>
  <PackageReference Include="Humanizer.Core" />
</ItemGroup>
```

For a deterministic presentation workflow, preserve canonical values, pass the request culture explicitly, and pass a fixed comparison time when testing relative dates:

```csharp
using System.Globalization;
using Humanizer;

var culture = CultureInfo.GetCultureInfo("en-US");
var now = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

var fieldLabel = nameof(CustomerRecord.LastLoginAt).Humanize();
var lastSeen = now.AddHours(-2).Humanize(
    utcDate: true,
    dateToCompareAgainst: now,
    culture: culture);

public sealed record CustomerRecord(DateTime LastLoginAt);
```

Establish the request's UI culture before formatting, or pass `CultureInfo` to culture-sensitive overloads. Apply any process-wide `Configurator` changes exactly once during startup. Snapshot or assertion-test visible output where wording is a product contract, because dependency or locale updates can change phrasing.

## Enterprise implementation guidance

Keep canonical data and localization keys separate from rendered text. Define supported cultures, fallback behavior, time-zone conversion, and whether relative time compares UTC or local values. Prefer explicit culture at background-job, queue-consumer, and batch boundaries where ambient request culture is absent. Inventory the exact locale resources included in deployment, and have product/localization review translated output rather than assuming grammatical equivalence.

## Integration with the catalog

Use with `enums-net.md` only after enum values have been validated and converted. Keep FluentValidation error codes and localization keys stable; humanize a separately localized client message if desired. Preserve structured values in telemetry and humanize only in the UI or final report renderer.

## Security, performance, AOT, trimming, and operations

Do not let culture-sensitive display strings become identifiers or authorization inputs. Humanized text can contain user-controlled source text, so retain normal output encoding and avoid composing it into HTML. Formatting allocates and relative-time output depends on the comparison clock; benchmark batch/reporting hot paths and inject a stable clock into tests. No package-level AOT/trimming guarantee is documented, so publish-test every supported culture and transformation surface, including resource loading and fallback.

## Avoid

Do not persist humanized output, parse it back into data, use it as a stable log/event field, rely on ambient culture in background work, compare localized strings in business logic, or mutate global `Configurator` behavior per request.

## Verification checklist

- [ ] The consuming project has a versionless `PackageReference`, and the resolved version is `3.0.10` from the central catalog.
- [ ] Every supported culture and fallback path has representative output tests using an explicit comparison clock.
- [ ] Canonical API, persistence, cache, and telemetry values remain separate from rendered text.
- [ ] Deployed artifacts contain the intended locale resources, and unsupported cultures follow the documented fallback policy.
- [ ] High-volume transformations are benchmarked, and published trimming/NativeAOT artifacts smoke-test culture/resource loading when applicable.

## Sources

- [Humanizer.Core 3.0.10 on NuGet](https://www.nuget.org/packages/Humanizer.Core/3.0.10) (Accessed 2026-07-27)
- [Humanizer official documentation](https://github.com/Humanizr/Humanizer/tree/main/docs) (Accessed 2026-07-27)
- [Humanizer localization guidance](https://github.com/Humanizr/Humanizer/blob/main/docs/localization.md) (Accessed 2026-07-27)
- [Humanizer extensibility and global configuration](https://github.com/Humanizr/Humanizer/blob/main/docs/extensibility.md) (Accessed 2026-07-27)
- [Humanizer v3 migration notes](https://github.com/Humanizr/Humanizer/blob/main/docs/migration-v3.md) (Accessed 2026-07-27)
