# TngTech.ArchUnitNET.xUnitV3

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `TngTech.ArchUnitNET.xUnitV3` |
| Pinned version | `0.13.3` |
| Status | Direct; optional test dependency for consumer-owned architecture rules |
| Role | ArchUnitNET assertions integrated with xUnit v3 |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | ArchUnitNET extension/core, xUnit v3, compiler/bytecode, target-framework, or architecture-policy change |

## Decision and scope

Use optionally when a consuming project chooses to test compiled-code dependency, layering, naming, or cycle rules. ArchUnitNET imports compiled C# bytecode into a model and evaluates fluent rules; this repository does not prescribe a custom architecture suite or policy. It does not prove runtime behavior, source-generator behavior, or deployment correctness.

## Recommended registration and use

There is no DI registration. In a non-packable test project, use standard SDK metadata and reference both packages directly and without versions:

```xml
<PropertyGroup>
  <IsTestProject>true</IsTestProject>
  <IsPackable>false</IsPackable>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="xunit.v3" />
  <PackageReference Include="TngTech.ArchUnitNET.xUnitV3" />
</ItemGroup>
```

Load only intended production assemblies once and evaluate a focused rule:

```csharp
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

// Replace this minimal declaration with a stable marker from the production
// assembly whose architecture is under test.
public sealed class OrderService { }

public sealed class DependencyRules
{
    private static readonly Architecture s_architecture =
        new ArchLoader().LoadAssemblies(typeof(OrderService).Assembly).Build();

    [Fact]
    public void Domain_does_not_depend_on_infrastructure()
    {
        IArchRule rule = Classes().That().ResideInNamespace("Example.Domain")
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace("Example.Infrastructure");

        rule.Check(s_architecture);
    }
}
```

Use namespaces, marker types, or explicitly bounded providers that remain stable under refactoring. Confirm the exact fluent method names against 0.13.3 when adapting the example to a repository-specific naming rule.

## Enterprise implementation guidance

Treat each consumer-owned rule as an architectural contract with a clear owner and remediation path. Keep allowed-dependency lists explicit, test an intentional violation so the rule's direction is proven, and revise exceptions in the same review as the design change. Cache the immutable architecture model to avoid repeated binary imports.

When a consumer adopts these tests, run its architecture checks in Debug: upstream recommends `dotnet test -c Debug` because Release optimization can alter bytecode details. A Release run may be supplementary, but it must not replace the consumer's Debug merge gate.

### Upgrade and rollback

Upgrade this extension only after confirming its transitive `TngTech.ArchUnitNET` core and xUnit v3 assertion dependencies remain compatible with the consuming project's test stack. Re-run every rule in Debug and inject one known forbidden dependency to verify direction, scope, and diagnostic output; compiler or target-framework changes deserve the same check because the library analyzes binaries. Roll back the central pin and any rule API changes together. No production data migration is involved, but a rollback is incomplete until the intentional violation still fails.

## Integration with the catalog

This extension belongs to the MTP-based [xunit.v3](xunit-v3.md) stack and does not replace the direct xUnit framework reference. Do not add a direct `TngTech.ArchUnitNET` reference merely because the extension brings it transitively. Use [AwesomeAssertions](awesomeassertions.md) for behavioral assertions; architecture rules are a different test boundary. See [test-platform, runner, and coverage selection](../package-guidance/package-selection.md#test-platform-runners-and-coverage) and the [ArchUnitNET xUnit v3 supply-chain entry](../package-guidance/supply-chain.md#tngtech-archunitnet-xunitv3).

## Security, performance, AOT, trimming, and operations

Load only trusted build outputs. Failure text can reveal types, namespaces, assemblies, and paths, so retain logs under build-artifact policy. Import cost scales with the analyzed assembly set; load once and keep scope bounded. These rules inspect ordinary compiled assemblies and do not validate trimmed or NativeAOT-published artifacts; add a separate published-artifact test if required.

## Avoid

- Do not reference the package from production projects or test projects that do not own architecture rules.
- Do not load arbitrary third-party or untrusted binaries.
- Do not treat architecture rules as replacements for unit, integration, or runtime tests.
- Do not mix this xUnit v3/MTP setup with VSTest SDK, adapter, or collector packages.

## Verification checklist

- [ ] The test project sets `IsTestProject=true` and `IsPackable=false` and has direct, versionless references to `xunit.v3` and this extension, with no direct core package reference.
- [ ] `dotnet test -c Debug` passes for the intended production assembly set.
- [ ] An intentional forbidden dependency fails with useful, non-sensitive diagnostics.
- [ ] The architecture model is built once and the project contains no VSTest stack packages.

## Sources

- [ArchUnitNET upstream guide and example](https://github.com/TNG/ArchUnitNET)
- [ArchUnitNET user guide](https://archunitnet.readthedocs.io/en/latest/guide/)
- [TngTech.ArchUnitNET.xUnitV3 0.13.3 on NuGet](https://www.nuget.org/packages/TngTech.ArchUnitNET.xUnitV3/0.13.3)
- [xUnit v3 getting started](https://xunit.net/docs/getting-started/v3/getting-started)

Accessed 2026-07-27.
