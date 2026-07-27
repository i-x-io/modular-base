# Microsoft.VisualStudio.Threading.Analyzers

## Catalog entry

`Microsoft.VisualStudio.Threading.Analyzers` **18.7.23** — universal catalog analyzer supplied through a shared `GlobalPackageReference` with private analyzer assets.

## Decision and scope

Use to flag risky synchronous waits and threading patterns in future projects. It is a compile-time guardrail, not a replacement for an application concurrency model or cancellation design.

## Recommended registration and use

The central global package reference applies the analyzer to future projects. Resolve diagnostics by propagating asynchronous execution, accepting `CancellationToken` at supported boundaries, and avoiding blocking on `Task`/`ValueTask`; configure exceptional rule severity through `.editorconfig` or `ModularBase.globalconfig`.

## Enterprise implementation guidance

Review analyzer findings against the host model: web request handlers, background services, UI code, and libraries have different synchronization constraints. Make async API changes deliberately, retain cancellation through I/O paths, and load-test high-contention changes rather than suppressing a warning by default.

## Integration with the catalog

It shares the `GlobalPackageReference` and private-assets policy documented in `meziantou-analyzer.md`. Repository-wide settings belong in `ModularBase.globalconfig`; source-location settings belong in `.editorconfig`.

## Security, performance, AOT, trimming, and operations

Correct async usage reduces thread-pool starvation and request latency risk, but analyzer compliance does not prove deadlock freedom. Analyzer execution has build/IDE cost only and does not affect runtime trimming or NativeAOT.

## Avoid

Do not silence diagnostics by adding `Task.Run` around I/O, block with `.Result`/`.Wait()` to preserve a synchronous signature, or apply UI-thread-specific patterns to server-side code without evidence.

## Verification checklist

- Build a representative async future project and triage emitted diagnostics.
- Test cancellation, timeouts, and exception propagation for changed async boundaries.
- Load-test the affected hosted workload when a finding changes concurrency behavior.
- Confirm no analyzer package reference is duplicated in individual projects.

## Sources

- https://www.nuget.org/packages/Microsoft.VisualStudio.Threading.Analyzers/18.7.23 (Accessed 2026-07-27)
- https://github.com/microsoft/vs-threading (Accessed 2026-07-27)
- https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files (Accessed 2026-07-27)
