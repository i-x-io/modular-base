# TngTech.ArchUnitNET.xUnitV3

## Catalog entry

`TngTech.ArchUnitNET.xUnitV3` **0.13.3** — direct, architecture-test-only package. It is centrally pinned in `Directory.Packages.props`; the designated architecture-test project references it without a `Version` attribute. The package targets `netstandard2.0`, so it is compatible with this repository's `net10.0` baseline.

## Decision and scope

Use this package to express and enforce architectural dependency, naming, layering, and cycle rules in xUnit v3 tests. It is approved only for architecture tests: it is not a production dependency, a DI library, or a general-purpose reflection utility. ArchUnitNET imports compiled C# bytecode into its architecture model, then evaluates fluent rules against that model; it does not prove runtime behavior.

## Recommended registration and use

There is no DI registration. In the designated project with `IXModularityProjectRole=ArchitectureTest` and `IsTestProject=true`, add the extension as a direct, versionless package reference alongside the direct `xunit.v3` reference:

```xml
<PackageReference Include="xunit.v3" />
<PackageReference Include="TngTech.ArchUnitNET.xUnitV3" />
```

Load the assemblies under test once per test class or fixture, create rules with the fluent API, and call `Check(architecture)` from `[Fact]` tests. Do not add a direct `TngTech.ArchUnitNET` reference merely because it is transitive from this extension, and do not add a version to either project `PackageReference`. The xUnit v3 package owns MTP runner integration; this extension supplies architecture-rule assertions, not a test runner.

## Enterprise implementation guidance

Define rules at a bounded-solution boundary and load only the intended production assemblies, not every dependency in the test process. Cache the immutable architecture model per fixture/test class to avoid repeated binary import; keep rules deterministic, explicit, and failure messages free of internal secrets or sensitive paths. Treat a rule failure as a design-contract failure and remediate the forbidden dependency or intentionally revise the rule in the same review.

Run architecture tests in the Debug configuration authoritatively. The upstream project warns that it reads analyzed binaries and recommends `dotnet test -c Debug`; Release-optimized binaries can omit or transform details relevant to bytecode-level dependency checks. If Release runs are retained as an additional signal, they must not replace Debug as the merge-gating result.

## Integration with the catalog

This package extends the MTP-based xUnit v3 testing stack documented in [xunit-v3.md](xunit-v3.md); it is not compatible with using the cataloged VSTest adapter/SDK/collector combination as the active runner stack. `xunit.v3` remains a direct dependency because the extension's transitive assertion package is not a substitute for the xUnit v3 framework and MTP runner. Use [awesomeassertions.md](awesomeassertions.md) for ordinary behavioral assertions and the Testcontainers package guides for integration tests; neither replaces architectural rules.

## Security, performance, AOT, trimming, and operations

Architecture tests load and inspect compiled assemblies, so restore only from approved feeds and do not point the loader at untrusted binaries. Their test output can disclose type, namespace, and assembly details; protect CI logs under the normal build-artifact policy. Binary import and rule evaluation add test-time CPU and memory cost, which is why the architecture is loaded once and scope is limited.

This package has no production deployment role and is not a NativeAOT or trimming dependency. Do not infer that architecture rules validate a trimmed or NativeAOT-published artifact: trimming, ahead-of-time compilation, generated code, dynamic loading, and reflection can change the binary surface the loader sees. Add a separately designed published-artifact test when that behavior matters.

## Avoid

Do not reference this package from production projects; add versions to project references; treat it as a replacement for unit/integration tests; load untrusted or arbitrary third-party assemblies; rely on Release configuration as the authoritative architecture result; or mix the xUnit v3/MTP configuration with the cataloged VSTest adapter, `Microsoft.NET.Test.Sdk`, or `coverlet.collector` packages.

## Verification checklist

- Confirm the architecture-test project has `IsTestProject=true`, direct versionless references to both `xunit.v3` and `TngTech.ArchUnitNET.xUnitV3`, and no direct core `TngTech.ArchUnitNET` reference.
- Run its architecture tests with `dotnet test -c Debug` and verify an intentionally forbidden dependency fails with useful, non-sensitive diagnostics.
- Confirm the loaded assembly list is limited to the intended production assemblies and the architecture model is built once per fixture/test class.
- Confirm the project does not combine this MTP-based setup with VSTest SDK, adapter, or collector packages.

## Sources

- https://www.nuget.org/packages/TngTech.ArchUnitNET.xUnitV3/0.13.3 (Accessed 2026-07-27)
- https://github.com/TNG/ArchUnitNET (Accessed 2026-07-27)
- https://archunitnet.readthedocs.io/en/latest/guide/ (Accessed 2026-07-27)
- https://xunit.net/docs/getting-started/v3/getting-started (Accessed 2026-07-27)
