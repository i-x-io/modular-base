# Design principles

## Purpose

These principles help future libraries remain cohesive, understandable, and changeable. They guide review; the mandatory rules remain [architectural rules](architectural-rules.md).

## Canonical definitions

### Design principles

Single responsibility means a type, module, or package has one primary reason to change. Cohesion measures whether its contents change together. Separation of concerns puts independently changing concerns behind explicit boundaries. DRY means one authoritative representation of a rule; YAGNI rejects speculative capability.

## Related and contrasting terms

SRP is not “one method per class”; a cohesive service may coordinate several closely related operations. DRY does not require merging coincidentally similar code. KISS favours the smallest understandable design, while abstraction is justified only by an active consumer, dependency, or testability need.

## Normative rules

- A public library type and package must have one explainable responsibility.
- Do not introduce a public interface, generic hook, or project split without a current boundary need.
- Keep policy and mechanism separate: stable contracts own intent; adapters own technology details.
- Use named constants, typed options, or semantic value objects for repeated contract values; raw repeated magic strings are prohibited in `src/**` by `S1192`.

## Library-focused examples

`IClock` may describe time acquisition; a `TimeProvider` adapter supplies the mechanism. A library parser may own parsing policy while a `Markdig` adapter owns Markdown technology details.

## Anti-patterns

God services, “utility” packages with unrelated capabilities, interfaces that mirror one class without a boundary, and string literals duplicated across public validation, serialization, and logging are rejected.

## Review questions

- What single responsibility would change this type or package?
- Can a consumer use this capability without taking unrelated dependencies?
- Is the abstraction justified by a current consumer or dependency direction?

## Analyzer and build enforcement

`S1192` is error for `src/**/*.cs` and suggestion for `test/**/*.cs`. `IXM1004` and `IXM1005` require public service contracts to be documented, making an unclear responsibility visible in review.

## Authoritative references

- [SOLID principles](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)
- [Code quality policy](code-quality-policy.md)

## Last research/access date

2026-07-27.
