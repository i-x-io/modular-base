# Analyzer taxonomy

## Purpose

This is the single diagnostic contract for `IX.Modularity.Analyzers`. The analyzer implementation, tests, package guide, and per-diagnostic pages must agree with this table. The `IXM100x` rules report semantic XML documentation omissions for designated public library API; malformed XML that the compiler itself rejects remains a compiler concern.

| ID | Title | Trigger and location | Default / repository severity | Remediation |
| --- | --- | --- | --- | --- |
| `IXM1001` | Public data object requires complete XML documentation | A user-authored public record, `Contracts` data type, or type whose name has an approved data suffix lacks complete XML documentation; report the type declaration identifier. | Warning / error | Add a non-empty summary plus applicable type-parameter and positional-record parameter documentation. |
| `IXM1002` | Public interface requires complete XML documentation | A user-authored public interface lacks complete XML documentation; report the interface declaration identifier. | Warning / error | Add a non-empty summary and applicable type-parameter documentation. |
| `IXM1003` | Public interface member requires complete XML documentation | A non-service public interface member lacks complete XML documentation; report the member declaration identifier. | Warning / error | Add a summary plus applicable type parameters, parameters, return value, and property value documentation. |
| `IXM1004` | Public service type requires complete XML documentation | A user-authored public service type lacks complete XML documentation; report the type declaration identifier. A service ends in `Service`, or is an interface derived from an `I*Service` interface. | Warning / error | Add a non-empty summary and applicable type-parameter documentation. |
| `IXM1005` | Public service member requires complete XML documentation | A member of a service interface lacks complete XML documentation; report the member declaration identifier. | Warning / error | Add a summary plus applicable type parameters, parameters, return value, and property value documentation. |
| `IXM2001` | Data objects should be records | A user-authored, externally visible data object is syntactically eligible when it is a non-abstract, non-static, non-record class with no base type other than `object`; report the type identifier. The analyzer does not infer immutability. | Info / suggestion | Consider a `record`; retain the class and suppress locally after review when a class-shaped contract is intentional. |

## Scope and exclusions

All `IXM100x` rules ignore compiler-generated, implicit, inherited, and non-user-authored symbols. They do not require documentation for private/internal implementation detail unless the repository's ordinary compiler/documentation policy does. A member is in scope only when its documented public interface or service contract is in scope; explicit interface implementations are governed by the exposed interface contract.

Complete member documentation means a non-empty `<summary>` and each applicable non-empty `<param>`, `<typeparam>`, `<returns>`, and property `<value>` element. It does not require documenting every transitive implementation exception. `IXM2001` applies a syntactic eligibility check to externally visible data objects: non-abstract, non-static, non-record classes with no base type other than `object`. It does not infer immutability or semantic eligibility. Mutable lifecycle, identity, EF/proxy/framework/interop requirements, and compatibility remain reviewed reasons to retain a class and suppress the suggestion locally.

## Configuration contract

`IXM1001`–`IXM1005` default to warning in the package and are set to error by repository policy. `IXM2001` defaults to info and is set to suggestion. A package consumer can configure the diagnostics according to its own compatibility policy; it should not claim this repository's enforcement level unless it adopts the same settings.

## Sources

- [Recommended XML tags for C# documentation comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags)
- [Records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- [Configure code analysis](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files)

Accessed 2026-07-27.
