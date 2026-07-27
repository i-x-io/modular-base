# xunit.v3

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `xunit.v3` |
| Pinned version | `3.2.2` |
| Status | Preferred approved test-only dependency |
| Role | xUnit v3 framework with Microsoft Testing Platform v1 runner integration |

## Decision and scope

Use for all new unit, integration, and architecture tests. Version 3.2.2's `xunit.v3` metadata selects `xunit.v3.mtp-v1`, matching this repository's .NET 10 `global.json` runner selection. It is the preferred replacement for catalog-only SpecsFor.

## Recommended registration and use

In a project with `IXModularityProjectRole=Test` or `ArchitectureTest`, set `IsTestProject=true` and add a versionless direct reference:

```xml
<PropertyGroup>
  <IsTestProject>true</IsTestProject>
  <IXModularityProjectRole>Test</IXModularityProjectRole>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="xunit.v3" />
</ItemGroup>
```

Use `[Fact]` for one fixed case and `[Theory]` for data-driven behavior. Await asynchronous work and keep each case independent:

```csharp
public sealed class SlugTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData(" already-clean ", "already-clean")]
    public async Task NormalizeAsync_returns_a_stable_slug(
        string input,
        string expected)
    {
        var actual = await Slug.NormalizeAsync(input);

        Assert.Equal(expected, actual);
    }
}
```

Run the preferred stack with `dotnet test`. For MTP/xUnit query filters, pass runner arguments after `--`, for example `dotnet test -- --filter-query /[category=fast]`, after assigning the corresponding trait/category in the suite.

## Enterprise implementation guidance

Inject clocks, randomness, environment, filesystem, and network boundaries so unit tests are repeatable. Use `IAsyncLifetime` for asynchronous external-resource setup/cleanup and collection fixtures only when sharing is explicit and reset is guaranteed. Never depend on execution order. Partition Docker-backed integration tests from fast unit tests by project or an explicit trait/filter contract.

Keep runner configuration repository-owned. On .NET 10+, `global.json` selects `Microsoft.Testing.Platform`; CI and local commands must use MTP options consistently. Record expected test counts or other discovery evidence so a zero-test run cannot appear successful.

## Integration with the catalog

Use [AwesomeAssertions](awesomeassertions.md) for richer diagnostics, Testcontainers for real PostgreSQL/Redis tests, and `TngTech.ArchUnitNET.xUnitV3` only in the architecture-test project. Do not add [Microsoft.NET.Test.Sdk](microsoft-net-test-sdk.md), [xunit.runner.visualstudio](xunit-runner-visualstudio.md), or [coverlet.collector](coverlet-collector.md) to this MTP stack. MTP coverage requires a separately approved `coverlet.MTP` entry.

## Security, performance, AOT, trimming, and operations

Test and fixture code can execute arbitrary processes and external calls. Use synthetic data and least-privilege test credentials, never production secrets, and keep failure output safe. Bound fixture setup, teardown, and retry/polling deadlines so CI cannot hang. The framework is test-only and does not validate production trimming or NativeAOT publication; exercise published artifacts separately where required.

## Avoid

- Do not mix VSTest SDK, adapter, or collector packages into this MTP project.
- Do not block asynchronous calls with `.Wait()` or `.Result`.
- Do not share mutable state without an explicit lifecycle and reset contract.
- Do not use sleep-based timing, execution order, local timezone, or ambient culture as hidden inputs.

## Verification checklist

- [ ] The test project has an allowed role, `IsTestProject=true`, and a direct versionless `xunit.v3` reference.
- [ ] `dotnet test` runs the expected `[Fact]` and `[Theory]` cases through the repository's MTP selection.
- [ ] Unit and Docker-backed integration suites can run independently and repeatedly.
- [ ] Failure diagnostics contain no secrets, and CI detects zero or unexpectedly missing tests.
- [ ] The project contains no VSTest SDK, adapter, or collector packages.

## Sources

- [xUnit v3 getting started](https://xunit.net/docs/getting-started/v3/getting-started)
- [xUnit v3 Microsoft Testing Platform guidance](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [xUnit v3 query filter language](https://xunit.net/docs/query-filter-language)
- [`dotnet test` runner selection](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)
- [xunit.v3 3.2.2 on NuGet](https://www.nuget.org/packages/xunit.v3/3.2.2)

Accessed 2026-07-27. Context7 was consulted first.
