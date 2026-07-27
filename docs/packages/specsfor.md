# SpecsFor

## Catalog entry

`SpecsFor` **8.0.0-rc2a** — catalog-only, pre-release test package; BDD-style specification framework. The catalog identifies it as the sole pre-release dependency.

## Decision and scope

Do not adopt for new tests. Retain the catalog entry only for controlled evaluation or migration research. Its StructureMap lineage and transitive dependency risk require an explicit dependency review before any project reference.

## Recommended registration and use

No future project should reference it by default. If an approved migration experiment needs it, isolate it in one `IsTestProject=true` project, pin only through the central catalog, and document the exact transitive graph and exit plan.

## Enterprise implementation guidance

Prefer the xUnit v3 stack for new automated tests. Require architecture approval, restore/audit evidence, compatibility testing, and a removal date before a SpecsFor pilot. Do not allow its composition conventions to enter production DI boundaries.

## Integration with the catalog

This package is deliberately distinct from `xunit-v3.md`, the preferred runner stack. `Directory.Build.targets` recognizes it as test-only; `Directory.Packages.props` records its catalog-only/pre-release status.

## Security, performance, AOT, trimming, and operations

Pre-release and legacy/transitive dependencies increase supply-chain, support, and compatibility risk. It has no approved AOT/trimming or operational role. Review licenses, vulnerabilities, dependency age, and container usage before even an isolated test pilot.

## Avoid

Do not use it for new test suites, add StructureMap to production composition, or normalize a prerelease dependency by consuming it transitively without review.

## Verification checklist

- Obtain explicit approval and record the migration/evaluation scope.
- Inspect `dotnet list <project> package --include-transitive --vulnerable` for the isolated pilot.
- Confirm no production project references SpecsFor or StructureMap-derived dependencies.
- Confirm the preferred xUnit v3 test path remains available.

## Sources

- https://www.nuget.org/packages/SpecsFor/8.0.0-rc2a (Accessed 2026-07-27)
- https://github.com/MattHoneycutt/SpecsFor (Accessed 2026-07-27)
- https://www.nuget.org/packages/StructureMap/ (Accessed 2026-07-27)
