# xunit.runner.visualstudio

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `xunit.runner.visualstudio` |
| Pinned version | `3.1.5` |
| Status | Approved test-only dependency for the VSTest alternative |
| Role | Visual Studio/VSTest adapter for xUnit v3 discovery and execution |

## Decision and scope

Use only when a future configuration deliberately selects the VSTest xUnit v3 path. The cataloged `xunit.v3` package installs native MTP support, but xUnit documents VSTest adapter support as a separate capability that does not interfere with MTP. The active platform selected for `dotnet test` determines which runner path is used; the adapter does not override that selection.

## Recommended registration and use

Configure only an allowed test-role project and keep all references versionless:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit.v3" />
<PackageReference Include="xunit.runner.visualstudio">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

The adapter is build/test tooling and should not flow transitively to consumers. The repository's current `global.json` selects MTP, so using this alternative first requires an architecture-approved VSTest repository/run configuration (`test.runner` set to `VSTest`, or the MTP selection removed). Then validate CLI and IDE discovery with the same minimal test:

```csharp
public sealed class DiscoveryTests
{
    [Fact]
    public void Adapter_discovers_xunit_v3_tests() => Assert.True(true);
}
```

Run it with VSTest-compatible `dotnet test` options; add `coverlet.collector` only if this VSTest path also needs coverage.

## Enterprise implementation guidance

Upgrade the framework variant, adapter, and `Microsoft.NET.Test.Sdk` as one compatibility set. Check test counts in CLI and each supported IDE so a missing adapter cannot silently turn a green job into zero discovered tests. Produce TRX artifacts in CI and enable diagnostic logs only for discovery failures.

Keep VSTest and MTP projects in separate run configurations. Their extension points and CLI arguments differ, even though both can be launched through `dotnet test`.

## Integration with the catalog

Pair with [Microsoft.NET.Test.Sdk](microsoft-net-test-sdk.md), `xunit.v3`, and optionally [coverlet.collector](coverlet-collector.md) in a run configuration whose active platform is VSTest. It is an alternative run path to the preferred [xunit.v3](xunit-v3.md) MTP setup; do not put both platform modes in one run configuration.

## Security, performance, AOT, trimming, and operations

The adapter loads test assemblies inside test infrastructure. Restore from approved feeds, preserve lock-file review, and protect logs that expose test names, paths, or failure values. Keep the adapter private to the test project. It has no production trimming or NativeAOT role.

## Avoid

- Do not add it to production projects or let it flow to package consumers.
- Do not use it without `Microsoft.NET.Test.Sdk` and the xUnit v3 framework.
- Do not assume the adapter changes the runner selected by `global.json`.
- Do not accept a successful build as proof that tests were discovered.

## Verification checklist

- [ ] The project has an allowed test role, `IsTestProject=true`, and versionless private adapter assets.
- [ ] The xUnit v3 framework and test SDK are present, and the repository/run configuration explicitly selects VSTest.
- [ ] CLI and supported IDEs discover the same expected test count.
- [ ] CI fails on zero/unexpected discovery and retains bounded TRX diagnostics.

## Sources

- [xUnit v3 getting started](https://xunit.net/docs/getting-started/v3/getting-started)
- [xUnit v3 Microsoft Testing Platform package variants](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [`dotnet test` with VSTest](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest)
- [xunit.runner.visualstudio 3.1.5 on NuGet](https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.5)

Accessed 2026-07-27. Context7 was consulted first.
