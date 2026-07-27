# OpenTelemetry.Extensions.Hosting

## Catalog entry

`<PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.17.0" />`

**Role:** integration between the OpenTelemetry SDK and the .NET Generic Host/ASP.NET Core dependency-injection and lifetime model. **Status:** approved central-catalog dependency and the standard registration path for hosted applications.

## Decision and scope

Use `services.AddOpenTelemetry()` at the application composition root. It registers hosted lifetime management: providers start with the host and are shut down/disposed during host shutdown. Multiple `WithTracing`, `WithMetrics`, and `WithLogging` calls configure the same hosted providers; do not independently build a provider for the same signals.

## Recommended registration and use

With central package management, add versionless references for the hosted SDK and only the signals the application actually uses:

```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
</ItemGroup>
```

The following composition-root example uses the eight cataloged packages and one cataloged Npgsql integration. It is intended for an ASP.NET Core application; the same `builder.Services` configuration applies to a Generic Host.

```csharp
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "modular-base-api",
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString()))
    .UseOtlpExporter(
        OtlpExportProtocol.Grpc,
        new Uri("https://otel-collector.internal:4317"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddSource("ModularBase.Orders"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsqlInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("ModularBase.Orders"));
```

The source/meter names must match application library declarations. `AddNpgsql()` and `AddNpgsqlInstrumentation()` come from `Npgsql.OpenTelemetry`; its package is cataloged separately. `UseOtlpExporter` owns export for logs, metrics, and traces, while normal `ILogger` category filters still decide which logs are emitted.

For a worker, use the same workflow with `Host.CreateApplicationBuilder(args)`, omit ASP.NET Core instrumentation, register the worker `ActivitySource`/`Meter`, and add the hosted service. This keeps provider startup and disposal aligned with the worker host lifetime.

## Enterprise implementation guidance

- Set a stable service identity once with `ConfigureResource`; add approved environment, region, and deployment identifiers only when their cardinality is controlled.
- Bind exporter configuration from the host configuration system so deployment settings can vary without rebuilding. Code-set options override environment-variable settings.
- Add custom processors through the signal builder (for example, `.AddProcessor<MyProcessor>()`) after registering their dependencies with DI. Keep processors signal-specific and document ordering because processors run in registration order around export.
- Keep registration centralized. Library projects emit API telemetry; they do not decide exporters, sampling, collector endpoints, or provider lifetime.
- Establish graceful host shutdown long enough for the exporter’s normal flush/dispose path; avoid manually stopping providers in request handlers or background services.

## Integration with the catalog

- [OpenTelemetry](opentelemetry.md) is the SDK configured by this package.
- [OpenTelemetry.Exporter.OpenTelemetryProtocol](opentelemetry.exporter.opentelemetryprotocol.md) configures the `AddOtlpExporter` calls.
- [OpenTelemetry.Instrumentation.AspNetCore](opentelemetry.instrumentation.aspnetcore.md), [HTTP](opentelemetry.instrumentation.http.md), and [runtime](opentelemetry.instrumentation.runtime.md) add their signal-specific builders.
- Use `Npgsql.OpenTelemetry` together with `.AddNpgsql()` for tracing and `.AddNpgsqlInstrumentation()` for metrics; see the Npgsql package guide.

## Security, performance, AOT, trimming, and operations

Register providers before building the host so startup failures are immediate and configuration is deterministic. Do not obtain credentials from logs or hard-code collector authorization headers. Hosting itself does not make every instrumentation/exporter trim or Native-AOT safe; test the composed app. The resource service identity must be non-secret and stable across replicas.

## Avoid

- Do not combine `AddOpenTelemetry()` and a separately built `Sdk.CreateTracerProviderBuilder()` for the same hosted signals.
- Do not call provider `Dispose` manually while the host still serves requests.
- Do not call `UseOtlpExporter` together with per-signal `AddOtlpExporter`; choose one registration model.
- Do not configure a different `service.name` in every signal pipeline.
- Do not use a volatile instance/pod ID as `service.name`; model that as a separate, bounded resource attribute when needed.

## Verification checklist

- [ ] `AddOpenTelemetry()` appears once in the hosted application composition root.
- [ ] All application sources/meters are registered, and required instrumentation packages are referenced.
- [ ] Trace, metric, and log exporters share an approved resource/service identity.
- [ ] Shutdown has been exercised to confirm provider disposal and exporter flush behavior.
- [ ] A termination test sends traffic immediately before shutdown and confirms the host grace period matches exporter drain expectations.
- [ ] A production-like trimmed/AOT publish was tested if it is a deployment target.

## Sources

Accessed 2026-07-27:

- [OpenTelemetry.Extensions.Hosting 1.17.0 on NuGet](https://www.nuget.org/packages/OpenTelemetry.Extensions.Hosting/1.17.0)
- [OpenTelemetry .NET hosting integration source](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Extensions.Hosting)
- [Hosted `AddOpenTelemetry` builder guidance](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/builders/add-opentelemetry.md)
- [OpenTelemetry .NET custom processor guidance](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/trace/customizing-the-sdk/README.md)
- [OpenTelemetry .NET resource configuration](https://opentelemetry.io/docs/languages/dotnet/resources/)
