# OpenTelemetry.Api

## Catalog entry

`<PackageVersion Include="OpenTelemetry.Api" Version="1.17.0" />`

**Role:** the vendor-neutral .NET telemetry API and semantic-conventions support used by instrumentation libraries. **Status:** approved central-catalog dependency; use it in reusable libraries that emit activities or meters without configuring an SDK.

## Decision and scope

`OpenTelemetry.Api` is not the SDK and does not export, sample, or collect telemetry by itself. Application composition roots use `OpenTelemetry` plus hosting/exporter packages to configure providers. Library code should depend on this API (and .NET `System.Diagnostics`/`System.Diagnostics.Metrics`) rather than taking a dependency on a backend or building a provider.

## Recommended registration and use

Use one static `ActivitySource` and `Meter` per logical library/component and guard expensive tag construction with `activity?.IsAllDataRequested`.

```csharp
using System.Diagnostics;

internal static class OrderTelemetry
{
    internal static readonly ActivitySource Source = new("ModularBase.Orders");
}

using Activity? activity = OrderTelemetry.Source.StartActivity("orders.validate");
if (activity?.IsAllDataRequested == true)
{
    activity.SetTag("orders.validation.outcome", "accepted");
}
```

The consuming application must call `.AddSource("ModularBase.Orders")` on its tracer provider. `StartActivity` may return `null` when no listener requests data; this is intentional and the example handles it.

## Enterprise implementation guidance

- Publish source/meter names and low-cardinality tag keys as compatibility contracts; changing either can break dashboards and alerts.
- Prefer OpenTelemetry semantic conventions where they apply. Use namespaced custom attributes for domain-specific data.
- Record meaningful operation boundaries, not every method call. Keep tag values finite and sanitized at the emission point when possible.
- Use baggage only for small, propagated, approved correlation values; it crosses process boundaries and is not a secure storage channel.

## Integration with the catalog

- [OpenTelemetry](opentelemetry.md) configures the SDK pipeline that consumes API emissions.
- [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md) is the recommended hosted application integration.
- HTTP, ASP.NET Core, runtime, and Npgsql packages produce their own built-in source/meter telemetry; application code should add custom spans only where that telemetry does not already establish the operation boundary.

## Security, performance, AOT, trimming, and operations

API emission should remain cheap when no provider listens. Avoid eagerly allocating tags, serializing objects, or collecting request bodies. Never add secrets or personal data to `Activity`, `Baggage`, or metric labels. Static, explicit sources/meters are trim/AOT-friendly; validate actual production publishing because listeners and downstream packages determine the full runtime behavior.

## Avoid

- Do not call `Sdk.CreateTracerProviderBuilder()` from a class library.
- Do not use `Activity.Current` as an authorization, tenancy, or persistence mechanism.
- Do not use dynamically generated source names, operation names, or tag keys.
- Do not assume every `StartActivity` call yields a non-null activity.

## Verification checklist

- [ ] Each library uses stable, documented source and meter names.
- [ ] The host registers those names through `.AddSource`/`.AddMeter`.
- [ ] Null `Activity` behavior is covered where code would otherwise dereference it.
- [ ] Custom tags are bounded, redacted, and reviewed against the telemetry data policy.
- [ ] The library has no exporter, collector endpoint, or provider-lifetime ownership.

## Sources

Accessed 2026-07-27:

- https://www.nuget.org/packages/OpenTelemetry.Api/1.17.0
- https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Api
- https://opentelemetry.io/docs/languages/dotnet/instrumentation/
- https://opentelemetry.io/docs/specs/semconv/general/recording-errors/
