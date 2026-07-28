# Enums.NET

## Catalog entry

`Enums.NET` **5.0.0** — direct catalog package; high-performance enum utilities, including parsing, formatting, cached metadata, validation, and flag operations. The catalog owns the version for `net10.0` projects using C# 14.

- **Adoption:** Direct
- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** `Enums.NET` version changes, target-framework changes, or enum metadata/formatting API changes.

## Decision and scope

Use for enum-specific operations where the standard library lacks the required API, attribute-backed formats are intentional, or measured throughput justifies its cached metadata. Keep public contracts expressed as the enum type or an explicitly documented wire string, not library-specific metadata wrappers.

## Recommended registration and use

With Central Package Management already enabled, add a versionless reference to the consuming project:

```xml
<ItemGroup>
  <PackageReference Include="Enums.NET" />
</ItemGroup>
```

For the common input-validation workflow, define the accepted format, use non-throwing parsing, and validate the result before entering the domain:

```csharp
using EnumsNET;

const string input = "in-progress";

if (!Enums.TryParse<JobState>(
        input, ignoreCase: true, out var state, EnumFormat.EnumMemberValue) ||
    !state.IsValid())
{
    throw new ArgumentException("Unsupported job state.", nameof(input));
}

var wireValue = state.AsString(EnumFormat.EnumMemberValue);

public enum JobState
{
    [System.Runtime.Serialization.EnumMember(Value = "queued")]
    Queued,
    [System.Runtime.Serialization.EnumMember(Value = "in-progress")]
    InProgress,
    [System.Runtime.Serialization.EnumMember(Value = "complete")]
    Complete
}
```

For `[Flags]` enums, use `IsValid()` to reject unknown bits and use `HasAllFlags` or `HasAnyFlags` to state the intended condition explicitly. Register custom `EnumFormat` instances once during startup and keep the same format precedence for parsing and formatting.

## Enterprise implementation guidance

Centralize external-string conversion and return a domain-level error rather than letting `Parse` exceptions escape normal validation paths. Decide whether names, numeric values, `EnumMemberAttribute`, descriptions, or another registered format are accepted; accepting multiple formats can create ambiguous contracts. During an enum change, test every supported wire value, aliases/duplicate values, the zero value, and all permitted flag combinations. Cache only derived presentation data whose culture and invalidation rules are explicit.

### Upgrade and rollback

Before upgrading, run contract tests for every accepted name, numeric representation, attribute format, alias, zero value, and flags combination. Review target-framework and reflection behavior in the published artifact. Rollback is a central-version re-pin and redeploy; stored enum values must remain readable by both versions during rollout.

## Integration with the catalog

[Humanizer.Core](humanizer-core.md) can produce presentation text after validation; it must not define wire-format names. Use [FluentValidation](fluentvalidation.md) to reject request values before domain conversion, and keep serializer configuration aligned with the same canonical names.

See the [`Enums.NET` supply-chain entry](../package-guidance/supply-chain.md#enums-net).

## Security, performance, AOT, trimming, and operations

Never trust a successful numeric conversion alone: the CLR can represent undefined enum values, and flags can contain unknown bits. Attribute-backed formats and metadata discovery rely on reflection and cached metadata. Enums.NET does not publish a package-level NativeAOT/trimming guarantee in its primary documentation; exercise `TryParse`, `AsString`, attributes, and flag validation in the published artifact. Benchmark against `System.Enum` on the actual hot path before adopting the dependency for performance alone.

## Avoid

Do not use descriptions or humanized text as stable API identifiers, accept numeric values accidentally, assume every numeric enum value is defined, treat `Enum.IsDefined` as flags-combination validation, or register process-wide custom formats per request.

## Verification checklist

- [ ] The consuming project has a versionless `PackageReference`, and the resolved version is `5.0.0` from the central catalog.
- [ ] Defined, undefined, numeric, duplicate, zero, case-sensitive, and case-insensitive inputs follow the documented contract.
- [ ] Flags tests cover valid composites, unknown bits, `HasAllFlags`, and `HasAnyFlags` semantics.
- [ ] Serialized names and accepted parse formats remain stable for every supported member during enum changes.
- [ ] Trimmed/NativeAOT smoke tests cover metadata, attributes, parsing, formatting, and flags where those publish modes are used.

## Sources

- [Enums.NET 5.0.0 on NuGet](https://www.nuget.org/packages/Enums.NET/5.0.0) (Accessed 2026-07-27)
- [Enums.NET official repository and v5 API examples](https://github.com/TylerBrinkley/Enums.NET) (Accessed 2026-07-27)
- [Enums.NET v5 changes and API examples](https://github.com/TylerBrinkley/Enums.NET/blob/master/README.md#v50-changes) (Accessed 2026-07-27)
- [Microsoft guidance for C# enum types](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/enum) (Accessed 2026-07-27)
