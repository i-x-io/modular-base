# BenchmarkDotNet

## Catalog entry

`BenchmarkDotNet` **0.15.8** — centrally pinned benchmark harness for dedicated performance-measurement projects; it is not a production library dependency.

- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** Review when the BenchmarkDotNet pin, a benchmarked target framework/runtime, or CI benchmark host changes.

## Decision and scope

Use it to measure a defined library workload, allocation profile, and regression baseline. Keep benchmarks separate from packable runtime packages and use results to inform a reviewed change rather than making an unqualified performance claim.

## Recommended registration and use

Reference it versionlessly in a dedicated non-packable benchmark project. Keep benchmark inputs deterministic, compare a baseline implementation, and record the runtime, input sizes, and allocation results with the change.

| Setting | Catalog guidance |
| --- | --- |
| `Job` / `RuntimeMoniker` | Pin the runtime and toolchain when comparing releases; do not silently compare different JITs or runtimes. |
| `ArtifactsPath` | Write reports beneath a disposable benchmark-artifacts directory; do not pack them into a library. |
| Exporters and diagnosers | Commit the chosen result format and enable `MemoryDiagnoser` when allocation is part of the claim. |
| Launch, warmup, and iteration counts | Use defaults for normal evidence; override only with a recorded reason, because `Dry` or very short jobs are smoke checks rather than stable measurements. |

## Enterprise implementation guidance

Benchmark representative public or internal library paths after correctness tests exist. Isolate I/O, network, clock, and environment variation. Review generated reports as evidence, not package artifacts. Do not run a benchmark suite as a substitute for `make test`.

### Upgrade and rollback

Upgrade the central pin in a dedicated change, restore the benchmark project, and rerun a stable canary benchmark on the same host and runtime. Compare job descriptions, environment headers, warnings, result columns, exporters, diagnoser output, and baseline ratios before comparing numeric results; a harness or runtime change can invalidate a historical series even when the benchmark code is unchanged. If discovery, toolchain execution, or result shape regresses, revert the central pin and regenerate results with `0.15.8`. Retain the prior report only as evidence from its original harness/runtime combination; never splice it into the new series.

## Integration with the catalog

`Directory.Packages.props` owns version `0.15.8`; the project file contains no version. Its intended use is governed by [performance and resource management](../architecture/performance-and-resource-management.md). Review its [supply-chain record](../package-guidance/supply-chain.md#benchmarkdotnet) before changing the pin.

## Security, performance, AOT, trimming, and operations

Benchmarking executes code repeatedly and can consume substantial CPU/time; do not benchmark production secrets or shared production systems. It has no runtime or trimming cost when confined to non-packable benchmark projects.

## Avoid

Do not reference it from a production library, publish benchmark infrastructure as a consumer dependency, infer production latency from one machine, or optimize without a representative measurement.

## Verification checklist

- [ ] The project is non-packable and references `BenchmarkDotNet` without a version.
- [ ] The benchmark has a baseline, representative inputs, and allocation output.
- [ ] Correctness tests pass independently of benchmark execution.

## Sources

- [BenchmarkDotNet documentation](https://benchmarkdotnet.org/articles/overview.html) — Accessed 2026-07-27.
- [BenchmarkDotNet 0.15.8 on NuGet](https://www.nuget.org/packages/BenchmarkDotNet/0.15.8) — Accessed 2026-07-27.
