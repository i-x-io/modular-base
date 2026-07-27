# coverlet.collector

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `coverlet.collector` |
| Pinned version | `10.0.1` |
| Status | Approved test-only dependency for the VSTest alternative |
| Role | VSTest data collector for cross-platform line and branch coverage |

## Decision and scope

Use only in a future configuration that deliberately selects VSTest as its test platform. The same `xunit.v3` framework can expose native MTP support and be run by the VSTest adapter; those capabilities do not inherently conflict, but this collector works only when the active `dotnet test` platform is VSTest. MTP coverage requires a separately approved `coverlet.MTP` catalog entry; it is not supplied by this package.

## Recommended registration and use

Use versionless references in a `Test` or `ArchitectureTest` project with `IsTestProject=true`:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit.v3" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="coverlet.collector" />
```

The repository's current `global.json` selects `Microsoft.Testing.Platform`, so this command is not valid under the current default. Adopt the alternative only through an architecture-approved, coherent VSTest runner selection for the repository/run configuration (for .NET 10, select `VSTest` through `test.runner` or omit the MTP selection), then collect Cobertura output:

```bash
dotnet test --collect:"XPlat Code Coverage" \
  --results-directory artifacts/test-results
```

For stable filters and formats, commit a VSTest `.runsettings` file and pass `--settings coverlet.runsettings`; keep generated `TestResults/**/coverage.cobertura.xml` outside source control and publish it as a bounded CI artifact.

## Enterprise implementation guidance

Measure line and branch coverage on production assemblies, exclude generated code and test assemblies by an explicit reviewed policy, and keep the raw Cobertura file for diagnosis. Gate on meaningful changed/critical paths rather than a repository-wide percentage alone. Pin SDK, adapter, framework variant, and collector as one validated toolchain, and run coverage in a dedicated CI job to keep the fast test signal clear.

## Integration with the catalog

Pair with [Microsoft.NET.Test.Sdk](microsoft-net-test-sdk.md), [xunit.runner.visualstudio](xunit-runner-visualstudio.md), and `xunit.v3` in a run configuration whose active platform is VSTest. Do not invoke this collector in the preferred MTP run described by [xunit.v3](xunit-v3.md). Coverlet's upstream documentation explicitly distinguishes `coverlet.collector`/VSTest from `coverlet.MTP`/MTP.

## Security, performance, AOT, trimming, and operations

Instrumentation adds CPU, memory, and I/O cost. Coverage artifacts may reveal source paths, type names, and test names, so restrict access and retention and do not upload credentials or production data. Coverage of ordinary test builds does not validate a trimmed or NativeAOT-published artifact; test that artifact separately if it is a supported workload.

## Avoid

- Do not mix `coverlet.collector` and `coverlet.msbuild` in one project or run.
- Do not invoke this collector while `dotnet test` is using MTP.
- Do not pass the MTP-only `--coverlet` switch to this VSTest workflow.
- Do not treat a single percentage as proof of test quality.

## Verification checklist

- [ ] The project uses one coherent SDK/framework/adapter/collector set with versionless references and the run configuration explicitly selects VSTest.
- [ ] `dotnet test --collect:"XPlat Code Coverage"` produces a non-empty Cobertura attachment for the intended production assembly.
- [ ] Filters and threshold policy are reviewed and deterministic in CI.
- [ ] CI publishes only bounded coverage artifacts without secrets or unrestricted local paths.

## Sources

- [Coverlet VSTest integration](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/VSTestIntegration.md)
- [Coverlet runner integrations and MTP/VSTest distinction](https://github.com/coverlet-coverage/coverlet)
- [.NET code coverage guidance](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)
- [coverlet.collector 10.0.1 on NuGet](https://www.nuget.org/packages/coverlet.collector/10.0.1)

Accessed 2026-07-27.
