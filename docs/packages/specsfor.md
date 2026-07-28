# SpecsFor

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `SpecsFor` |
| Pinned version | `8.0.0-rc2a` |
| Status | Catalog-only; prerelease dependency retained for controlled evaluation or migration only; do not adopt |
| Role | Legacy BDD-style specification framework retained for evaluation or migration research |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | `SpecsFor` prerelease/status, target-framework, transitive dependency, vulnerability, or migration-policy change |

## Decision and scope

Do not adopt SpecsFor for new tests. It is the catalog's sole prerelease dependency and remains available only for a controlled compatibility investigation or migration. Its legacy StructureMap lineage and transitive graph require explicit review before any `PackageReference`.

## Recommended registration and use

There is no recommended project setup or new-test code example because adding one would imply approval. If a migration experiment is explicitly approved, isolate it in one project using standard test metadata and a versionless reference:

```xml
<PropertyGroup>
  <IsTestProject>true</IsTestProject>
  <IsPackable>false</IsPackable>
</PropertyGroup>
<ItemGroup>
  <!-- Approved, time-bounded migration experiment only. -->
  <PackageReference Include="SpecsFor" />
</ItemGroup>
```

Before compiling any migrated specification, capture its exact direct/transitive graph and vulnerability report:

```bash
dotnet list path/to/Pilot.csproj package --include-transitive
dotnet list path/to/Pilot.csproj package --include-transitive --vulnerable
```

Record the owner, removal date, source suite, compatibility findings, and xUnit v3 replacement path. Do not copy SpecsFor composition conventions into production dependency injection.

## Enterprise implementation guidance

Prefer [xunit.v3](xunit-v3.md) for all new automated tests. For a sanctioned migration, translate one behavior at a time into ordinary arrange/act/assert tests and compare outcomes before removing the legacy specification. Freeze the pilot scope, store restore/audit evidence, and require an explicit project-level decision for any transitive dependency exception.

Treat the prerelease version as unsupported until restore, compilation, discovery, runtime, licensing, and dependency-health checks succeed on the repository's pinned .NET SDK. An experiment is evidence gathering, not permission for broader adoption.

### Upgrade and rollback

Do not advance this prerelease pin as routine maintenance. A version change requires a renewed, time-bounded migration decision plus exact direct/transitive dependency, vulnerability, license, restore, compile, and discovery evidence. Upgrade only the isolated pilot and continue translating behavior to xUnit v3. Rollback means restoring the previous central pin or, preferably, removing the pilot reference and migrated SpecsFor code; verify that no StructureMap-derived dependency remains in production or test outputs.

## Integration with the catalog

This entry is deliberately separate from the preferred xUnit v3/MTP stack and must not become a transitive production dependency. Central package management supplies the prerelease pin; project files must not override it. Any approved pilot must explicitly set `IsTestProject=true` and `IsPackable=false`. See [test-platform, runner, and coverage selection](../package-guidance/package-selection.md#test-platform-runners-and-coverage) and the [SpecsFor supply-chain entry](../package-guidance/supply-chain.md#specsfor).

## Security, performance, AOT, trimming, and operations

Prerelease and aging transitive dependencies increase supply-chain, maintenance, and compatibility risk. Restore only from approved feeds, review lock-file changes, and retain audit output for the pilot. SpecsFor has no approved production, NativeAOT, trimming, container, or operational role.

## Avoid

- Do not create new SpecsFor suites.
- Do not add StructureMap or SpecsFor conventions to production composition.
- Do not suppress prerelease or vulnerability findings to make a pilot pass.
- Do not reference the package outside an explicitly approved, time-bounded test project.

## Verification checklist

- [ ] The project-level adoption decision defines the pilot scope, owner, exit criteria, and removal date.
- [ ] The `PackageReference` is versionless and isolated in a project with `IsTestProject=true` and `IsPackable=false`.
- [ ] Direct/transitive package, vulnerability, and license evidence is recorded.
- [ ] No production project references SpecsFor or StructureMap-derived dependencies.
- [ ] The preferred xUnit v3 path remains the target for migrated behavior.

## Sources

- [SpecsFor upstream repository](https://github.com/MattHoneycutt/SpecsFor)
- [SpecsFor 8.0.0-rc2a on NuGet](https://www.nuget.org/packages/SpecsFor/8.0.0-rc2a)
- [StructureMap package history on NuGet](https://www.nuget.org/packages/StructureMap/)

Accessed 2026-07-27.
