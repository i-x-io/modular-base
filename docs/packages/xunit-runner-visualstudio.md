# xunit.runner.visualstudio

## Catalog entry

`xunit.runner.visualstudio` **3.1.5** — test-only catalog package; xUnit v3 adapter for Visual Studio and VSTest discovery/execution.

## Decision and scope

Use only when a future project deliberately selects the VSTest xUnit v3 path. The pinned `xunit.v3` package installs `xunit.v3.mtp-v1`, so the preferred catalog configuration is MTP; VSTest requires the xUnit VSTest-compatible `mtp-off` package variant before this adapter is usable.

## Recommended registration and use

In a project with `IXModularityProjectRole=Test` or `ArchitectureTest` and `IsTestProject=true`, pair this package with `Microsoft.NET.Test.Sdk` and the xUnit VSTest-compatible `mtp-off` package variant. Run `dotnet test` and use VSTest options such as the coverlet collector. That compatible package is not currently a catalog entry; add it centrally before authoring a VSTest test project.

## Enterprise implementation guidance

Upgrade the adapter, test SDK, and framework as a compatible set. Validate both CLI and IDE discovery, produce standard CI result artifacts, and retain runner diagnostics only when needed for a failing pipeline.

## Integration with the catalog

This is the VSTest alternative, not a companion to the preferred MTP `xunit-v3.md` package. Its SDK and coverage dependencies are documented in `microsoft-net-test-sdk.md` and `coverlet-collector.md`.

## Security, performance, AOT, trimming, and operations

The adapter loads test assemblies and extensions, so approved restore sources and lock files matter. It is build/test infrastructure only, with no production AOT/trimming scope.

## Avoid

Do not place it in production projects, use it without a test SDK and the `mtp-off` xUnit variant, or assume it is interchangeable with the Microsoft Testing Platform runner mode.

## Verification checklist

- Confirm `dotnet test` discovers a `[Fact]` only in a future project using the explicitly selected VSTest-compatible package set.
- Confirm the IDE displays the same tests.
- Run the coverlet collector and a test result logger through the selected VSTest path.

## Sources

- https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.5 (Accessed 2026-07-27)
- https://xunit.net/docs/getting-started/v3/getting-started (Accessed 2026-07-27; Context7 consulted first)
