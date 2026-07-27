# IX.Modularity.Analyzers

`IX.Modularity.Analyzers` enforces XML documentation for public modularity contracts and services, service operations that use FluentResults, coded business failures, and stack-preserving broad exception catches. It also recommends records for simple data objects.

The package contains analyzer assets only. Add it to a C# project as an analyzer package; it has no runtime library dependency, including on FluentResults. The `IXM3001`–`IXM3003` rules are warnings by default; this repository promotes them to errors.
