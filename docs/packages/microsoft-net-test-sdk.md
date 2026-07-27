# Microsoft.NET.Test.Sdk

## Catalog entry

`Microsoft.NET.Test.Sdk` **18.8.1** — test-only catalog package; SDK integration that lets VSTest discover and run .NET test projects.

## Decision and scope

Use only in a future project that deliberately selects the cataloged VSTest alternative. The pinned `xunit.v3` package installs the MTP runner, so it is not the preferred xUnit v3 configuration as currently cataloged. This package is runner infrastructure, not a test framework and not a production dependency.

## Recommended registration and use

Use only a project with `IXModularityProjectRole=Test` or `ArchitectureTest`, set `<IsTestProject>true</IsTestProject>`, and, after selecting a VSTest-compatible xUnit package variant, reference this package with `xunit.runner.visualstudio`. Run the project with `dotnet test`; use VSTest-compatible collector and logger options only.

## Enterprise implementation guidance

Standardize test results, filtering, and diagnostic-log collection in CI. Keep the test SDK version catalog-managed with the adapter; upgrade and validate that pair together across local IDE and CI agents.

## Integration with the catalog

Required by the VSTest alternative in `xunit-runner-visualstudio.md`; coverage guidance is in `coverlet-collector.md`. The preferred MTP package is `xunit-v3.md`. Test-only package enforcement is owned by `Directory.Build.targets`.

## Security, performance, AOT, trimming, and operations

It executes test code and adapters, so restore only from approved feeds and retain diagnostic logs as potentially sensitive build artifacts. It has no production runtime, trimming, or NativeAOT role.

## Avoid

Do not add it to libraries, combine it with the default MTP `xunit.v3` package, combine it with a different test adapter for the same framework, or treat a test SDK upgrade as isolated from its runner adapter.

## Verification checklist

- Confirm `dotnet test` discovers the expected tests only after the VSTest-compatible framework/adapter combination is selected.
- Run the selected VSTest collector and result logger in CI.
- Verify the project role is `Test` or `ArchitectureTest`, it is marked `IsTestProject=true`, and it has no package-local version.

## Sources

- https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.8.1 (Accessed 2026-07-27)
- https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest (Accessed 2026-07-27)
