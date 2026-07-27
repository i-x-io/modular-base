# Humanizer.Core

## Catalog entry

`Humanizer.Core` **3.0.10** — direct catalog package; human-readable transformations for strings, dates, quantities, enums, and numbers.

## Decision and scope

Use for user-facing display text only. It is not a formatting standard for logs, database values, URLs, API payloads, or other machine contracts.

## Recommended registration and use

Use the extension method closest to the presentation boundary. Rely on the current culture only where the request culture is correctly established; otherwise pass the intended `CultureInfo` explicitly.

## Enterprise implementation guidance

Keep canonical values separate from localized display text. Treat global `Configurator` changes as process-wide state and set them once during startup if needed. Define supported cultures and verify resource availability as part of localization testing.

## Integration with the catalog

Use with `enums-net.md` only after enum values have been validated and converted. Keep FluentValidation error codes stable; humanize a separately localized client message if desired.

## Security, performance, AOT, trimming, and operations

Do not let culture-sensitive display strings become identifiers. Formatting hot paths may allocate; measure before applying it to batch processing. No package-level AOT/trimming guarantee is documented, so test supported cultures in the published target.

## Avoid

Do not persist humanized output, parse it back into data, or configure global behavior per request.

## Verification checklist

- Assert output for every supported culture and fallback behavior.
- Verify invariant machine formats remain separate.
- Benchmark any high-volume transformation path.

## Sources

- https://www.nuget.org/packages/Humanizer.Core/3.0.10 (Accessed 2026-07-27)
- https://github.com/Humanizr/Humanizer/blob/main/docs/localization.md (Accessed 2026-07-27)
- https://github.com/Humanizr/Humanizer/blob/main/docs/extensibility.md (Accessed 2026-07-27)
