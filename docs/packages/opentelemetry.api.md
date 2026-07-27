# OpenTelemetry.Api

> **Owner:** `IX`
> **Last reviewed:** `2026-07-27`
> **Review trigger:** Review when the OpenTelemetry API version, target framework, propagation specification, or semantic conventions change.

## Catalog entry

`<PackageVersion Include="OpenTelemetry.Api" Version="1.17.0" />`

**Role:** the vendor-neutral .NET telemetry API and semantic-conventions support used by instrumentation libraries. **Status:** approved central-catalog dependency; use it in reusable libraries that emit activities or meters without configuring an SDK.

## Decision and scope

`OpenTelemetry.Api` is not the SDK and does not export, sample, or collect telemetry by itself. Application composition roots use `OpenTelemetry` plus hosting/exporter packages to configure providers. Library code should depend on this API (and .NET `System.Diagnostics`/`System.Diagnostics.Metrics`) rather than taking a dependency on a backend or building a provider.

## Recommended registration and use

With central package management, reusable libraries add a versionless reference:

```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry.Api" />
</ItemGroup>
```

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

Use the same stable instrumentation name for metrics when it represents the same component, while keeping instrument names specific and units explicit:

```csharp
using System.Diagnostics.Metrics;

internal static class OrderMetrics
{
    internal static readonly Meter Meter = new("ModularBase.Orders");
    internal static readonly Counter<long> Accepted =
        Meter.CreateCounter<long>("orders.accepted", unit: "{order}");
}

OrderMetrics.Accepted.Add(1, new("orders.channel", "api"));
```

The application must register `.AddMeter("ModularBase.Orders")`. Keep `orders.channel` to a documented finite set; never substitute an order or customer identifier.

## Enterprise implementation guidance

- Publish source/meter names and low-cardinality tag keys as compatibility contracts; changing either can break dashboards and alerts.
- Prefer OpenTelemetry semantic conventions where they apply. Use namespaced custom attributes for domain-specific data.
- Dispose dynamically created `ActivitySource`/`Meter` instances, but prefer process-lifetime static instances for stable library instrumentation. Do not create either object per operation.
- Record meaningful operation boundaries, not every method call. Keep tag values finite and sanitized at the emission point when possible.
- Use baggage only for small, propagated, approved correlation values; it crosses process boundaries and is not a secure storage channel.

### Source and propagation contract

| Contract | Default behavior | Library guidance | Failure and sensitivity |
| --- | --- | --- | --- |
| `ActivitySource` name/version | Only listeners subscribed to the source receive its activities. | Use a stable, documented name; add an instrumentation version when consumers need it. | A name mismatch silently yields `null` activities. Names are non-secret but become query contracts. |
| `Meter` and instrument names | Only registered meters are collected; instrument name, unit, type, and meaning form a contract. | Keep names stable, units explicit, and attributes finite. | Changing name/type/unit breaks dashboards; unbounded values create series growth. |
| `Activity.Current` | Carries ambient context through supported async execution. | Use it only for correlation and parentage; pass explicit domain/security context separately. | Suppressed/lost execution context can break correlation and must never change authorization behavior. |
| `Propagators.DefaultTextMapPropagator` | Injects/extracts the process-wide configured propagation formats. | Let the application choose propagators; libraries should use the global API rather than replacing it. | Extracted baggage/headers are untrusted input. Never propagate secrets or use baggage for authorization. |

### Upgrade and rollback

Upgrade `OpenTelemetry.Api` with the aligned SDK family. For reusable libraries, verify the package’s target-framework compatibility and treat source names, meter/instrument definitions, semantic attributes, and propagation behavior as public contracts. Test both with no listener (including `StartActivity() == null`) and with the cataloged `1.17.0` SDK. Roll back the API and SDK family together if consumers cannot load the assembly or correlation changes; reverting the package does not restore dashboards after an instrumentation-name or metric-contract change, so revert those library changes too.

## Integration with the catalog

- [OpenTelemetry](opentelemetry.md) configures the SDK pipeline that consumes API emissions.
- [OpenTelemetry.Extensions.Hosting](opentelemetry.extensions.hosting.md) is the recommended hosted application integration.
- HTTP, ASP.NET Core, runtime, and Npgsql packages produce their own built-in source/meter telemetry; application code should add custom spans only where that telemetry does not already establish the operation boundary.
- See the catalog-wide [OpenTelemetry composition decision](../package-guidance/package-selection.md#opentelemetry-composition), the [OTLP observability recipe](../recipes/opentelemetry-otlp-postgresql.md), and the [OpenTelemetry.Api supply-chain entry](../package-guidance/supply-chain.md#opentelemetry-api).

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

- [OpenTelemetry.Api 1.17.0 on NuGet](https://www.nuget.org/packages/OpenTelemetry.Api/1.17.0)
- [OpenTelemetry.Api 1.17.0 source](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.17.0/src/OpenTelemetry.Api)
- [Manual instrumentation for OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/instrumentation/)
- [OpenTelemetry .NET 1.17.0 tracing API guidance](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.17.0/docs/trace/README.md)
- [OpenTelemetry error-recording semantic conventions](https://opentelemetry.io/docs/specs/semconv/general/recording-errors/)
