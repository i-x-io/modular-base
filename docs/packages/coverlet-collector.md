# coverlet.collector

## Catalog entry

`coverlet.collector` **10.0.1** — test-only catalog package; VSTest data collector for cross-platform coverage.

## Decision and scope

Use only for a future project that deliberately selects the cataloged VSTest stack. The pinned `xunit.v3` package installs the Microsoft Testing Platform (MTP) runner, so it must not be mixed with this VSTest collector without first selecting the VSTest-compatible xUnit package variant. This collector is not the MTP-native `coverlet.MTP` integration.

## Recommended registration and use

Reference it only from an `IsTestProject=true` project that has selected the VSTest-compatible framework/adapter combination, then collect with `dotnet test --collect:"XPlat Code Coverage"`. Treat generated coverage as CI evidence, not a quality target; publish a configured report artifact outside source control.

## Enterprise implementation guidance

Set project/assembly filters deliberately and review branch coverage on critical decision paths. Keep test assemblies excluded unless a report purpose requires them. Preserve the raw coverage XML for CI diagnostics and make report publication retention explicit.

## Integration with the catalog

Use with `microsoft-net-test-sdk.md` and `xunit-runner-visualstudio.md` only for the VSTest alternative. The preferred MTP stack is documented in `xunit-v3.md`; it needs MTP-native coverage integration if coverage is required. This package is centrally versioned and test-only enforced by `Directory.Packages.props` and `Directory.Build.targets`.

## Security, performance, AOT, trimming, and operations

Instrumentation adds runtime and I/O overhead and coverage files can reveal source paths and test names. Do not run it in a production deployment or expose unrestricted report artifacts. Validate coverage separately for trimmed or NativeAOT publish modes when those become supported workloads.

## Avoid

Do not mix this collector with `coverlet.msbuild` for the same run, use it with the MTP-default `xunit.v3` package, use the MTP-only `--coverlet` switch, or gate delivery solely on a global percentage.

## Verification checklist

- Run `dotnet test --collect:"XPlat Code Coverage"` only in a future VSTest-compatible test project.
- Confirm the result contains the expected production assembly and excludes test assemblies by policy.
- Verify CI uploads the intended report without credentials or unrestricted local paths.

## Sources

- https://www.nuget.org/packages/coverlet.collector/10.0.1 (Accessed 2026-07-27)
- https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage (Accessed 2026-07-27)
- https://github.com/coverlet-coverage/coverlet (Accessed 2026-07-27)
