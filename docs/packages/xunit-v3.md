# xunit.v3

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `xunit.v3` |
| Pinned version | `3.2.2` |
| Status | Direct; preferred test framework for test-role projects using Microsoft Testing Platform |
| Role | xUnit v3 framework with Microsoft Testing Platform v1 runner integration |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | xUnit v3 meta-package/runner variant, .NET SDK/MTP, target-framework, test configuration, or CI runner change |

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
using Xunit;

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

internal static class Slug
{
    internal static Task<string> NormalizeAsync(string value)
    {
        string normalized = string.Join(
            '-',
            value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();

        return Task.FromResult(normalized);
    }
}
```

Run the preferred stack with `dotnet test`. For MTP/xUnit query filters, pass runner arguments after `--`, for example `dotnet test -- --filter-query /[category=fast]`, after assigning the corresponding trait/category in the suite.

## Enterprise implementation guidance

Inject clocks, randomness, environment, filesystem, and network boundaries so unit tests are repeatable. Use `IAsyncLifetime` for asynchronous external-resource setup/cleanup and collection fixtures only when sharing is explicit and reset is guaranteed. Never depend on execution order. Partition Docker-backed integration tests from fast unit tests by project or an explicit trait/filter contract.

Keep runner configuration repository-owned. On .NET 10+, `global.json` selects `Microsoft.Testing.Platform`; CI and local commands must use MTP options consistently. Record expected test counts or other discovery evidence so a zero-test run cannot appear successful.

Keep runner configuration explicit and repository-owned:

| Setting | Purpose | Default behavior | Repository guidance | Reload/failure behavior |
| --- | --- | --- | --- | --- |
| `parallelizeTestCollections` | Run independent collections concurrently | Runner-defined enabled behavior | Disable only for a measured shared-resource constraint; prefer fixture isolation | Read at process start; unsafe sharing produces nondeterministic failures |
| `maxParallelThreads` | Bound test concurrency | Runner-calculated | Cap for constrained CI or external resources, and measure the effect | Read at process start; excessive concurrency causes contention |

Store supported settings in `xunit.runner.json`, copy it to the output directory, and restart the test process after changes. Do not place secrets in runner configuration or diagnostic messages.

### Upgrade and rollback

Inspect the exact `xunit.v3` meta-package dependency before upgrading because it selects the MTP runner variant; version 3.2.2 resolves `xunit.v3.mtp-v1` exactly. Upgrade companion assertion/architecture extensions after confirming compatibility, keep the repository's .NET 10 runner selection coherent, and compare discovery counts, filters, fixtures, parallel behavior, and failure output in all test projects. Roll back the central pin and any runner/configuration changes together. There is no persistent-data migration, but restored discovery and execution—not compilation—prove rollback.

## Integration with the catalog

Use [AwesomeAssertions](awesomeassertions.md) for richer diagnostics, Testcontainers for real PostgreSQL/Redis tests, and [TngTech.ArchUnitNET.xUnitV3](tngtech-archunitnet-xunitv3.md) only in the architecture-test project. Do not add [Microsoft.NET.Test.Sdk](microsoft-net-test-sdk.md), [xunit.runner.visualstudio](xunit-runner-visualstudio.md), or [coverlet.collector](coverlet-collector.md) to this MTP stack. MTP coverage requires a separately approved `coverlet.MTP` entry. See [test-platform, runner, and coverage selection](../package-guidance/package-selection.md#test-platform-runners-and-coverage), the [PostgreSQL and Redis Testcontainers recipe](../recipes/testcontainers-postgresql-redis-xunit.md), and the [xunit.v3 supply-chain entry](../package-guidance/supply-chain.md#xunit-v3).

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
- [xUnit v3 configuration files](https://xunit.net/docs/config-xunit-runner-json)
- [xUnit v3 query filter language](https://xunit.net/docs/query-filter-language)
- [`dotnet test` runner selection](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)
- [xunit.v3 3.2.2 on NuGet](https://www.nuget.org/packages/xunit.v3/3.2.2)

Accessed 2026-07-27. Context7 was consulted first.
