# Dependency policy

## Scope

This repository is the dependency catalog for future .NET packages. It is not an application and intentionally has no `src/`, `tests/`, solution, or project files. Package entries in [`Directory.Packages.props`](../../Directory.Packages.props) are approved catalog entries, not proof that every package is used.

## SDK and language baseline

`global.json` requires SDK `10.0.302`, `rollForward: disable`, and `allowPrerelease: false`. Future projects consume `net10.0` and C# `14.0` from [`Directory.Build.props`](../../Directory.Build.props), unless a consciously reviewed project-level override is needed. The repository policy itself does not compile without a project.

## Central Package Management

`Directory.Packages.props` is the sole version authority:

- `ManagePackageVersionsCentrally` is enabled.
- `CentralPackageTransitivePinningEnabled` is `false`; do not elevate transitive packages into the catalog unless the package becomes a deliberate direct dependency.
- `CentralPackageVersionOverrideEnabled` is `false`; do not use `VersionOverride`.
- Future project files must reference packages without a `Version` attribute. `Directory.Build.targets` rejects both per-project `Version` and `VersionOverride` before package references are collected.
- Universal analyzers use `GlobalPackageReference` with `PrivateAssets="all"`; they apply to every future project without flowing to consumers. `Microsoft.CodeAnalysis.PublicApiAnalyzers` is a centrally-versioned `PackageVersion`, referenced only by packable projects through the shared targets.

For a future package project, use this form:

```xml
<ItemGroup>
  <PackageReference Include="FluentValidation" />
</ItemGroup>
```

The snippet follows CPM and the repository target policy; it has not been compiled because this catalog deliberately contains no project.

Test-only package IDs are explicitly allow-listed in `Directory.Build.targets` and may be used only when `IsTestProject` is `true`.

## Sources, restore, and audit

[`NuGet.Config`](../../NuGet.Config) clears inherited package and audit sources, then permits only `nuget.org`. Its package-source mapping maps `*` to that sole source. Keeping a single mapped source removes source ambiguity and satisfies NuGet's CPM guidance for repositories with more than one configured source.

NuGet audit is enabled in both shared props files with `NuGetAuditMode=all` and `NuGetAuditLevel=low`. `NU1901` through `NU1904` are warnings-as-errors, so known low-or-higher severity advisory findings fail a future project build.

Lock files are enabled with `RestorePackagesWithLockFile=true`. Shared props set `RestoreLockedMode=true` in CI and Release builds, and the repository restore target passes `--locked-mode` for those builds. Commit each future project's generated `packages.lock.json`; locked restore then detects graph drift instead of silently rewriting the lock file.

## Approval workflow for future projects

1. Add or update the exact `PackageVersion` in `Directory.Packages.props`; retain one entry per package ID.
2. Keep the project `PackageReference` versionless and choose only catalog entries appropriate for its role.
3. Restore locally, review the generated lock file, and commit it with the project.
4. Run the repository build targets once the project exists; address audit findings rather than suppressing them.
5. Update the matching package documentation using the [common schema](package-documentation-schema.md).

## Sources

- [NuGet Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) — Accessed 2026-07-27.
- [NuGet PackageReference and lock files](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files) — Accessed 2026-07-27.
- [NuGet package source mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping) — Accessed 2026-07-27.
- [NuGet auditing packages](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages) — Accessed 2026-07-27.
