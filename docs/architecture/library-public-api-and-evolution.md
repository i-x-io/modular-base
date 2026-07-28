# Library public API and evolution

## Purpose

Protect consumers from accidental source, binary, behavioral, and package-contract breaks.

## Canonical definitions

### Library public API

Public API includes public/protected symbols and documented behavior consumers reasonably rely on. Source compatibility preserves compilation; binary compatibility preserves execution of already compiled consumers; behavioral compatibility preserves observable contract. A public API baseline records shipped and pending surface.

## Related and contrasting terms

A NuGet package version, assembly identity, and API surface are related but distinct contracts. XML documentation explains an API; it does not prove compatibility. Package validation complements but does not replace API baselines and tests.

## Normative rules

- Treat every public/protected type, member, nullability annotation, default, constraint, dependency, and documented behavior as a release decision.
- Packable libraries maintain `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` with `#nullable enable`.
- Additions require tests, complete documentation, and a reviewed baseline update; removals and signature changes require an explicit compatibility and versioning decision.
- Avoid public vendor types unless the package deliberately owns that integration boundary.
- Changing a public service operation from a raw return type to `Result` or `Result<T>` is a breaking API change. Plan it through the normal compatibility and semantic-versioning process.
- A published business-error code is a compatibility contract. Do not repurpose it for a different meaning; callers must not branch on explanatory messages.

## Library-focused examples

Adding an overload can alter overload resolution; review it as a compatibility change. Replacing `IReadOnlyList<T>` with `IEnumerable<T>` can reduce a consumer guarantee even when source still compiles.

## Anti-patterns

Rewriting shipped baseline history to conceal a break, relying on a major version as documentation, and exposing an ORM model through a neutral contracts package are forbidden.

## Review questions

- What already-compiled and source consumers observe differently?
- Does a transitive package dependency become part of the consumer contract?
- Has documentation changed with behavior and the API baseline?

## Analyzer and build enforcement

Public API analyzers run for packable projects and pack requires baseline files. `IXM1001`–`IXM1005` enforce complete public data/interface/service documentation. `IXM3001` enforces service result shapes; it does not make a migration compatible. `CS1591` remains disabled because these focused rules define the repository contract.

## Authoritative references

- [Public API analyzer guide](../packages/microsoft-codeanalysis-publicapianalyzers.md)
- [.NET API compatibility tools](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/overview)
- [Versioning .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/versioning)

## Last research/access date

2026-07-27.
