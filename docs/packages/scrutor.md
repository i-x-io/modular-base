# Scrutor

## Catalog entry

`Scrutor` **7.0.0** — direct catalog package; assembly scanning and service-decoration extensions for `Microsoft.Extensions.DependencyInjection`.

## Decision and scope

Use for narrowly bounded convention-based registration or well-defined decorators. It is not a replacement for an explicit composition root.

## Recommended registration and use

Scan from a fixed marker type in an application-owned assembly, select only the required classes/interfaces, and set lifetimes explicitly. Use decoration for cross-cutting behavior where wrapping order and lifetime are understood.

## Enterprise implementation guidance

Keep scans deterministic and small; constrain namespaces, assignability, or attributes rather than scanning all loaded assemblies. Document decorator order and make decorators transparent for cancellation, exceptions, and disposal. Prefer explicit registrations for critical services and trimming-sensitive deployments.

## Integration with the catalog

The fixed-marker policy aligns with `fluentvalidation-dependencyinjectionextensions.md`. Decorators may host `polly.md` behavior, but `microsoft-extensions-http-resilience.md` remains the preferred HTTP integration.

## Security, performance, AOT, trimming, and operations

Assembly scanning is reflection-based and therefore a trimming/NativeAOT risk unless reachability is preserved. It also increases startup work. Test the complete registration graph and publish artifact; do not assume types discovered in a development build remain available after trimming.

## Avoid

Do not scan every loaded assembly, accidentally register framework/third-party implementations, depend on registration order implicitly, or decorate types with incompatible lifetimes.

## Verification checklist

- Assert expected registrations and lifetimes from the built container.
- Test decorator ordering and disposal/cancellation propagation.
- Run trimmed/NativeAOT startup and resolution smoke tests if scanning is retained.

## Sources

- https://www.nuget.org/packages/Scrutor/7.0.0 (Accessed 2026-07-27)
- https://github.com/khellang/Scrutor (Accessed 2026-07-27)
