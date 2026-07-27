# YamlDotNet

## Catalog entry

`YamlDotNet` **18.1.0** — direct catalog package; YAML parser/emitter and object serialization/deserialization library.

## Decision and scope

Use for controlled YAML configuration, documents, and interchange where YAML is required. YAML parsed from untrusted parties needs a dedicated input, schema, and resource policy.

## Recommended registration and use

Build and reuse immutable serializer/deserializer instances from `SerializerBuilder` and `DeserializerBuilder`. Configure naming conventions and type conversion explicitly. Enable nullability enforcement when deserializing into non-nullable models and reject values outside the application schema.

## Enterprise implementation guidance

Deserialize into narrow DTOs, validate them with `fluentvalidation.md`, then map them to domain objects. Set document-size/depth/resource limits outside the parser where necessary, choose duplicate-key handling deliberately, and keep custom type converters scoped to trusted types.

## Integration with the catalog

Use `fluentvalidation.md` for semantic validation and `fluentresults.md` to represent expected configuration/document errors. Do not use `scrutor.md` scanning to discover arbitrary YAML target types.

## Security, performance, AOT, trimming, and operations

YAML aliases, deep nesting, and large documents require workload-specific controls. Avoid polymorphic deserialization of untrusted data. Reflection-based object mapping can be trim/AOT-sensitive; build a published-target test covering every model/converter used in production.

## Avoid

Do not deserialize untrusted YAML into broad object graphs, infer types from attacker-controlled tags, or assume serialization preserves a security policy by itself.

## Verification checklist

- Round-trip supported DTOs and validate naming/nullability behavior.
- Test duplicate keys, aliases, oversized/deep documents, and unknown fields against policy.
- Run a trimmed/NativeAOT serialization/deserialization smoke test where applicable.

## Sources

- https://www.nuget.org/packages/YamlDotNet/18.1.0 (Accessed 2026-07-27)
- https://github.com/aaubry/YamlDotNet (Accessed 2026-07-27)
- https://github.com/aaubry/YamlDotNet/wiki/Serialization.Deserializer (Accessed 2026-07-27)
- https://github.com/aaubry/YamlDotNet/wiki/Serialization.Serializer (Accessed 2026-07-27)
