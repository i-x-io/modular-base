# Enums.NET

## Catalog entry

`Enums.NET` **5.0.0** — direct catalog package; high-performance enum utilities, including parsing, formatting, metadata, and flag operations.

## Decision and scope

Use for enum-specific operations where the standard library lacks the required API or measured throughput matters. Keep public contracts expressed as the enum type, not library-specific metadata wrappers.

## Recommended registration and use

Use the package's typed extension APIs at domain boundaries and keep parsing culture/format requirements explicit. Cache application-level derived display values when they are repeatedly used.

## Enterprise implementation guidance

Centralize external-string-to-enum conversion, reject undefined values unless the contract allows them, and test every serialized value during enum changes. Document flags combinations independently from display text.

## Integration with the catalog

`humanizer-core.md` can provide presentation text; it must not define wire-format names. Use `fluentvalidation.md` to validate request values before domain conversion.

## Security, performance, AOT, trimming, and operations

Metadata and enum discovery can depend on runtime reflection. Enums.NET does not publish a package-level NativeAOT/trimming guarantee in its primary documentation; treat NativeAOT as a release gate and exercise metadata, parsing, and formatting paths in the published artifact.

## Avoid

Do not use display descriptions as stable API identifiers. Do not assume every numeric value of an enum is defined or valid for a flags contract.

## Verification checklist

- Test defined, undefined, composite-flags, and case/culture-specific parsing cases.
- Assert serialized names remain stable for all supported enum members.
- Run a trimmed/NativeAOT smoke test when the application uses enum metadata.

## Sources

- https://www.nuget.org/packages/Enums.NET/5.0.0 (Accessed 2026-07-27)
- https://github.com/TylerBrinkley/Enums.NET (Accessed 2026-07-27)
