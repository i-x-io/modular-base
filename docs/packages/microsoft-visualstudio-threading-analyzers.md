# Microsoft.VisualStudio.Threading.Analyzers

## Catalog entry

`Microsoft.VisualStudio.Threading.Analyzers` **18.7.23** — universal catalog analyzer supplied through a shared `GlobalPackageReference` with private analyzer assets.

## Decision and scope

Use to flag risky synchronous waits, fire-and-forget tasks, and threading patterns in future projects. It is a compile-time guardrail, not a replacement for an application concurrency model, cancellation design, or host-specific synchronization rules. The `VSTHRDxxxx` rule set applies to server and library code as well as Visual Studio extensions, but UI-thread-specific findings must be interpreted against the actual host.

## Recommended registration and use

The repository already owns the global registration in `Directory.Packages.props`:

```xml
<GlobalPackageReference Include="Microsoft.VisualStudio.Threading.Analyzers" Version="18.7.23"
                        PrivateAssets="all"
                        IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
```

Do not repeat it in project files. The equivalent for a different centrally managed repository that requires project scope remains versionless and private:

```xml
<PackageReference Include="Microsoft.VisualStudio.Threading.Analyzers"
                  PrivateAssets="all"
                  IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
```

Prefer fixes that preserve asynchronous flow. VSTHRD002 covers synchronous waits on tasks; VSTHRD103 covers synchronously blocking calls inside async methods; VSTHRD110 covers observing a task's result. Configure exceptions by rule ID and the narrowest source scope:

```editorconfig
[src/**/*.cs]
dotnet_diagnostic.VSTHRD002.severity = error
dotnet_diagnostic.VSTHRD103.severity = error
dotnet_diagnostic.VSTHRD110.severity = warning
```

The package includes AdditionalFiles describing known thread-switching and main-thread APIs. Add custom entries only for real framework contracts and follow the upstream file formats; an arbitrary method name can hide a defect instead of modeling a thread transition.

## Enterprise implementation guidance

Triage findings by host boundary. In ASP.NET Core and workers, propagate async execution and cancellation through I/O paths; do not import UI-thread switching patterns. In UI or extension code, document the scheduler or main-thread requirement and switch only at the narrow boundary that needs it. In public libraries, changing a synchronous API to async is an API design and compatibility decision.

A common adoption workflow is to inventory diagnostics, repair direct `.Result`, `.Wait()`, and unobserved-task cases first, then promote proven rules to warnings or errors. Test success, cancellation, timeout, and exception propagation after each async boundary change. Load-test hosted workloads when the fix changes concurrency or allocation. CI should run the same build configuration as local builds, and analyzer upgrades should be reviewed for new or reclassified `VSTHRDxxxx` diagnostics.

## Integration with the catalog

It shares the `GlobalPackageReference` and private-assets policy documented in `meziantou-analyzer.md`. Repository-wide severity defaults belong in `ModularBase.globalconfig`; source-location exceptions belong in `.editorconfig`, which takes precedence for matching files. Central package management owns version `18.7.23`, so projects do not carry versions or duplicate references.

## Security, performance, AOT, trimming, and operations

Correct async usage reduces thread-pool starvation, hangs, and request-latency risk, but analyzer compliance does not prove deadlock freedom, fairness, cancellation correctness, or bounded resource use. Test under the production host and workload. Do not expose exception details or abandon security cleanup when moving work across async boundaries.

Analyzer execution costs build and IDE CPU only. Application changes proposed in response can affect allocations, scheduling, and throughput, so measure them. Private analyzer assets prevent consumer flow, and the analyzer itself has no runtime, trimming, or NativeAOT effect. Restore only from approved feeds because the analyzer executes in compiler and IDE processes.

## Avoid

Do not silence diagnostics by wrapping I/O in `Task.Run`, block with `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` merely to preserve a synchronous signature, discard tasks without an explicit ownership/observation mechanism, add custom thread-transition declarations without a real contract, or apply UI-thread patterns to server code.

## Verification checklist

- [ ] Confirm central version `18.7.23`, `PrivateAssets=all`, and no duplicate project references.
- [ ] Build a representative async project and triage each emitted `VSTHRDxxxx` diagnostic against its host model.
- [ ] Verify a scoped `.editorconfig` severity is effective locally and in CI.
- [ ] Test success, cancellation, timeout, and exception propagation for every changed async boundary.
- [ ] Load-test affected hosted workloads when fixes change concurrency behavior.
- [ ] Inspect a packed library and confirm analyzer assets do not flow to consumers.

## Sources

- [Microsoft.VisualStudio.Threading.Analyzers 18.7.23 on NuGet](https://www.nuget.org/packages/Microsoft.VisualStudio.Threading.Analyzers/18.7.23) (Accessed 2026-07-27)
- [Microsoft vs-threading analyzer rule index](https://microsoft.github.io/vs-threading/analyzers/index.html) (Accessed 2026-07-27)
- [Microsoft vs-threading source and configuration](https://github.com/microsoft/vs-threading) (Accessed 2026-07-27)
- [Microsoft: Configuration files for .NET code-analysis rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files) (Accessed 2026-07-27)
