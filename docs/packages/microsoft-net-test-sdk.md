# Microsoft.NET.Test.Sdk

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `Microsoft.NET.Test.Sdk` |
| Pinned version | `18.8.1` |
| Status | Approved test-only dependency for the VSTest alternative |
| Role | VSTest host integration for test discovery and execution |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | Test SDK, .NET SDK/VSTest runner, xUnit adapter, target-framework, or CI test-host change |

## Decision and scope

Use only when a future configuration deliberately selects the VSTest alternative. This package supplies VSTest runner infrastructure; it is not a test framework, assertion library, or production dependency. xUnit v3's native MTP support and VSTest adapter support are separate and do not inherently interfere; the active platform selected for `dotnet test` determines which path runs.

## Recommended registration and use

Configure a `Test` or `ArchitectureTest` project with `IsTestProject=true` and versionless references:

```xml
<PropertyGroup>
  <IsTestProject>true</IsTestProject>
  <IXModularityProjectRole>Test</IXModularityProjectRole>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" />
  <PackageReference Include="xunit.v3" />
  <PackageReference Include="xunit.runner.visualstudio" />
</ItemGroup>
```

The root `global.json` currently selects MTP. Before using this alternative, obtain architecture approval for a coherent VSTest repository/run configuration and select `VSTest` through .NET 10's `test.runner` setting (or remove the MTP selection). Then use VSTest syntax consistently:

```bash
dotnet test --logger "trx;LogFileName=tests.trx" \
  --results-directory artifacts/test-results
```

## Enterprise implementation guidance

Keep the test SDK, xUnit framework variant, adapter, and collectors on a tested compatibility set. Standardize TRX output, filters, result paths, and diagnostic-log retention in CI. Verify discovery both from `dotnet test` and supported IDEs after upgrades; a successful compile does not prove adapter discovery.

Use repository-relative result locations, avoid machine-specific paths, and separate VSTest jobs from MTP jobs because their options and extension models differ.

### Upgrade and rollback

Treat `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, the xUnit framework, and any VSTest collector as a compatibility set. After changing the central pins, compare discovered and executed test counts from CLI and supported IDEs, exercise TRX publication, and run a bounded diagnostic invocation for a known discovery failure. Roll back the complete validated set if the host fails to start, tests disappear, or result contracts change; this test-only package requires no application migration.

## Integration with the catalog

This SDK is required by the VSTest alternative described in [xunit.runner.visualstudio](xunit-runner-visualstudio.md), with optional VSTest coverage from [coverlet.collector](coverlet-collector.md). It is not needed by the preferred MTP run in [xunit.v3](xunit-v3.md). The repository-wide .NET 10 `global.json` selects MTP, so a VSTest alternative requires an intentional runner-selection change and a separately validated run configuration rather than package references alone. See [test-platform, runner, and coverage selection](../package-guidance/package-selection.md#test-platform-runners-and-coverage) and the [Microsoft.NET.Test.Sdk supply-chain entry](../package-guidance/supply-chain.md#microsoft-net-test-sdk).

## Security, performance, AOT, trimming, and operations

The SDK starts test hosts and loads test assemblies and adapters. Restore only from approved feeds; treat TRX and diagnostic logs as potentially sensitive because they can include paths, test names, arguments, and failure output. Keep detailed diagnostics off by default. This package has no production trimming or NativeAOT role.

## Avoid

- Do not reference it from production projects.
- Do not assume package references override the repository's active MTP runner selection.
- Do not mix VSTest and MTP options in one invocation.
- Do not upgrade the SDK independently of framework, adapter, and collector validation.

## Verification checklist

- [ ] The project is marked as a test project, has an allowed test role, and uses only versionless package references.
- [ ] The xUnit v3 framework and adapter are present, and the repository/run configuration explicitly selects VSTest.
- [ ] CLI and supported IDEs discover the same focused `[Fact]` test.
- [ ] CI produces bounded TRX output and optional diagnostics without sensitive values.

## Sources

- [`dotnet test` runner selection and MTP/VSTest behavior](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)
- [`dotnet test` with VSTest](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest)
- [.NET test platform comparison](https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview)
- [Microsoft.NET.Test.Sdk 18.8.1 on NuGet](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.8.1)

Accessed 2026-07-27.
