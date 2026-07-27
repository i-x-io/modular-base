# Type system and data modeling

## Purpose

Select .NET types that make a library’s data, nullability, ownership, and equality semantics explicit.

## Canonical definitions

### Type system and data model

A record is a type with compiler-provided value-oriented members; a class normally has reference identity. Immutable data cannot be changed after construction. Nullability annotations are part of a public contract. A value object expresses equality by contained values.

## Related and contrasting terms

Records are not inherently immutable, and classes can be immutable. `readonly struct` avoids allocation but has copying and boxing trade-offs. A DTO is a boundary representation, whereas a domain value object owns domain invariants.

## Normative rules

- Public data must state nullability and collection mutability deliberately.
- Prefer immutable data and `IReadOnly*` inputs where callers must not mutate owned state.
- Use a record for eligible value-like data; retain a class for identity, mutation, inheritance, framework materialization, or compatibility.
- Do not expose mutable arrays or `List<T>` when a read-only contract is intended.

## Library-focused examples

`public sealed record PageRequest(int Offset, int Limit);` conveys value semantics. A `StreamLease` class may retain identity and deterministic disposal, so converting it to a record would be wrong.

## Anti-patterns

Changing an existing public class to a record without compatibility review, accepting `IDictionary` as a public contract, and returning a mutable collection backed by internal state are prohibited.

## Review questions

- Is equality by identity or by value?
- Who owns and may mutate this data?
- Are nullability and collection semantics consumable without inspecting implementation?

## Analyzer and build enforcement

`IXM2001` is a nonblocking syntactic record suggestion for eligible class-shaped data objects; it does not infer immutability. Mutable lifecycle, identity, EF/proxy/framework/interop, and compatibility are valid reviewed reasons to retain a class and suppress it locally. `IXM1001` documents public data objects. The source policy prohibits `IDictionary` contracts; nullable is enabled repository-wide.

## Authoritative references

- [Records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- [Nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references)

## Last research/access date

2026-07-27.
