# BenchmarkDotNet

## Catalog entry

`BenchmarkDotNet` **0.15.8** — centrally pinned benchmark harness for dedicated performance-measurement projects; it is not a production library dependency.

## Decision and scope

Use it to measure a defined library workload, allocation profile, and regression baseline. Keep benchmarks separate from packable runtime packages and use results to inform a reviewed change rather than making an unqualified performance claim.

## Recommended registration and use

Reference it versionlessly in a dedicated non-packable benchmark project. Keep benchmark inputs deterministic, compare a baseline implementation, and record the runtime, input sizes, and allocation results with the change.

## Enterprise implementation guidance

Benchmark representative public or internal library paths after correctness tests exist. Isolate I/O, network, clock, and environment variation. Review generated reports as evidence, not package artifacts. Do not run a benchmark suite as a substitute for `make test`.

## Integration with the catalog

`Directory.Packages.props` owns version `0.15.8`; the project file contains no version. Its intended use is governed by [performance and resource management](../architecture/performance-and-resource-management.md).

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
