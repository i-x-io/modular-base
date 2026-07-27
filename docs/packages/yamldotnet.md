# YamlDotNet

## Catalog entry

`YamlDotNet` **18.1.0** — direct catalog package; YAML parser/emitter and object serialization/deserialization library with a native `net10.0` asset. The catalog owns the version for C# 14 projects.

## Decision and scope

Use for controlled YAML configuration, documents, and interchange where YAML is required. YAML parsed from untrusted parties needs a dedicated input, schema, and resource policy. Deserialization creates typed objects; it does not provide semantic validation or make a document trustworthy.

## Recommended registration and use

With Central Package Management already enabled, add a versionless reference to the consuming project:

```xml
<ItemGroup>
  <PackageReference Include="YamlDotNet" />
</ItemGroup>
```

For a strict configuration workflow, build and reuse a deserializer, reject duplicate and unmatched keys, enforce C# required/nullability metadata, then perform semantic validation:

```csharp
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

var deserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .WithDuplicateKeyChecking()
    .WithEnforceRequiredMembers()
    .WithEnforceNullability()
    .Build();

const string yaml = "name: billing-api\nreplicas: 3\n";
var service = deserializer.Deserialize<ServiceConfig>(yaml);

if (service.Replicas is < 1 or > 20)
    throw new InvalidDataException("replicas must be between 1 and 20.");

public sealed class ServiceConfig
{
    public required string Name { get; init; }
    public int Replicas { get; init; }
}
```

Use a separately configured `SerializerBuilder` for output, usually with the same naming convention. Keep strict input and tolerant migration/import profiles as distinct instances; call `IgnoreUnmatchedProperties()` only when forward compatibility is an explicit contract and unknown keys are surfaced through another mechanism.

## Enterprise implementation guidance

Deserialize into narrow DTOs, run structural checks, validate with `fluentvalidation.md`, and only then map to domain objects. Bound bytes before parsing and configure `WithMaximumRecursion(...)` for the workload; define policies for aliases, duplicate keys, multiple documents, unknown properties, tags, and type converters. Include file/line context in operator-facing errors while redacting document values and secrets. For configuration, parse and validate a candidate fully before atomically replacing the last known good snapshot.

## Integration with the catalog

Use `fluentvalidation.md` for semantic validation and `fluentresults.md` to represent expected configuration/document errors. Do not use `scrutor.md` scanning to discover arbitrary YAML target types. Keep secret resolution outside YAML deserialization so configuration files contain references rather than secret values where possible.

## Security, performance, AOT, trimming, and operations

YAML aliases, deep nesting, numerous collection items, and large scalars require workload-specific limits. Avoid broad `object` graphs, attacker-controlled tags, and polymorphic type selection; register only narrowly scoped converters for known types. Treat emitted YAML as data requiring context-appropriate encoding if embedded elsewhere. Reflection-based object mapping can be trim/AOT-sensitive; publish-test every DTO, naming convention, attribute, and converter used in production. Monitor parse latency, rejection reasons, reload success, and retained configuration version without logging document bodies.

## Avoid

Do not deserialize untrusted YAML into broad object graphs, ignore unknown keys silently in security-sensitive configuration, infer CLR types from attacker-controlled tags, mutate live configuration before full validation, or assume a serialization round trip preserves comments, formatting, or a security policy.

## Verification checklist

- [ ] The consuming project has a versionless `PackageReference`, and the resolved version is `18.1.0` from the central catalog.
- [ ] Supported DTOs round-trip as required, and naming, required-member, nullability, unknown-property, and duplicate-key behavior is asserted.
- [ ] Tests cover aliases, deep nesting, maximum recursion, oversized scalars/documents, multiple documents, tags, and malformed syntax against policy.
- [ ] Invalid reloads preserve the last known good configuration and emit redacted, actionable diagnostics.
- [ ] Trimmed/NativeAOT publish tests cover every production DTO and custom converter when those modes are used.

## Sources

- [YamlDotNet 18.1.0 on NuGet](https://www.nuget.org/packages/YamlDotNet/18.1.0) (Accessed 2026-07-27)
- [YamlDotNet official repository and quick start](https://github.com/aaubry/YamlDotNet) (Accessed 2026-07-27)
- [YamlDotNet deserializer configuration](https://github.com/aaubry/YamlDotNet/wiki/Serialization.Deserializer) (Accessed 2026-07-27)
- [YamlDotNet serializer configuration](https://github.com/aaubry/YamlDotNet/wiki/Serialization.Serializer) (Accessed 2026-07-27)
- [YamlDotNet 18.1.0 release](https://github.com/aaubry/YamlDotNet/releases/tag/v18.1.0) (Accessed 2026-07-27)
