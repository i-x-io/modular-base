# Performance and resource management

## Purpose

Make performance, allocation, and resource-lifetime choices explicit and measurable for library consumers.

## Canonical definitions

### Performance and resource management

A span is a stack-only view over contiguous memory. `Memory<T>` is a heap-storable memory view suitable for asynchronous boundaries. Ownership identifies who disposes or returns a resource. A benchmark measures a representative workload under controlled conditions.

## Related and contrasting terms

`Span<T>` avoids some allocations but cannot cross `await` or be stored in heap objects. `Memory<T>` can cross asynchronous boundaries but does not make underlying storage safe to retain indefinitely. A microbenchmark is not a production latency guarantee.

## Normative rules

- Measure before and after meaningful performance changes; use `BenchmarkDotNet` only in dedicated benchmark projects, never as a production dependency.
- Prefer `ReadOnlySpan<T>`/`Span<T>` for synchronous parsing or formatting hot paths when profiling shows a benefit and lifetime is obvious.
- Use `ReadOnlyMemory<T>`/`Memory<T>` at asynchronous or stored boundaries; document ownership, mutation, and lifetime.
- Dispose owned `IDisposable`/`IAsyncDisposable` resources deterministically and never return pooled memory after its lease ends.

## Library-focused examples

An internal UTF-8 parser can accept `ReadOnlySpan<byte>` synchronously. An asynchronous reader can expose `ReadOnlyMemory<byte>` only while its documented buffer lease remains valid. A benchmark compares established representative input sizes and records allocations.

## Anti-patterns

Exposing span types across an async boundary, retaining caller-owned buffers, using `ToArray()` in a measured hot path without need, and optimizing from intuition instead of measurements are rejected.

## Review questions

- What workload, allocation, and baseline prove this optimization?
- Who owns this buffer and for how long?
- Would a simpler API be fast enough without exposing fragile lifetime semantics?

## Analyzer and build enforcement

`CA1845`, `CA1846`, and `CA1873` are errors where applicable. Performance claims require benchmark evidence; benchmarks are not part of the Makefile’s public library build interface unless explicitly added as a scoped target.

## Authoritative references

- [Span and Memory usage guidelines](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines)
- [BenchmarkDotNet guide](../packages/benchmarkdotnet.md)

## Last research/access date

2026-07-27.
