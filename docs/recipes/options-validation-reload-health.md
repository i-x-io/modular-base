# Options binding, startup validation, reload, and health

## Problem and boundary

Use this recipe for settings that must be valid at startup, may reload while the process runs, and must expose their current validity through readiness. Configuration providers own change detection. `Microsoft.Extensions.Options.ConfigurationExtensions` owns binding and change-token registration; `Microsoft.Extensions.Options` owns materialization, validation, caching, and monitoring; the application owns safe adoption of a new value; health checks own the readiness signal. Reload does not automatically rebuild downstream clients or make a multi-resource transition atomic.

## Required catalog packages

The ASP.NET Core shared framework supplies the web health-check endpoint. The
following Web SDK block is a consuming-application example using centrally
managed versions:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Options" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" />
  </ItemGroup>
</Project>
```

The explicit references make ownership visible and allow central package management to keep the extensions stack aligned. A configuration provider must support and enable reload before `IOptionsMonitor<T>` can observe changes; the options packages do not poll an arbitrary source by themselves.

## Define and validate one focused contract

```csharp
using Microsoft.Extensions.Options;

public sealed class PartnerApiOptions
{
    public const string SectionName = "PartnerApi";

    public string BaseUrl { get; set; } = "";
    public int TimeoutSeconds { get; set; }
    public string ApiKey { get; set; } = "";
}

public sealed class PartnerApiOptionsValidator : IValidateOptions<PartnerApiOptions>
{
    public ValidateOptionsResult Validate(string? name, PartnerApiOptions options)
    {
        var failures = new List<string>();

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add("PartnerApi:BaseUrl must be an absolute HTTPS URI.");
        }

        if (options.TimeoutSeconds is < 1 or > 30)
        {
            failures.Add("PartnerApi:TimeoutSeconds must be from 1 through 30.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            failures.Add("PartnerApi:ApiKey is required.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
```

The options type is a binding shape, not a secret store; the API key must come from an approved configuration/secret provider. The validator checks local shape and cross-field policy without calling the partner. Validation errors name keys and rules but never include values. Keep unreliable network checks out of startup validation and readiness fan-out.

## Bind strictly and fail invalid startup

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IValidateOptions<PartnerApiOptions>,
    PartnerApiOptionsValidator>();

builder.Services
    .AddOptions<PartnerApiOptions>()
    .Bind(
        builder.Configuration.GetRequiredSection(PartnerApiOptions.SectionName),
        binder => binder.ErrorOnUnknownConfiguration = true)
    .ValidateOnStart();

builder.Services.AddSingleton<PartnerApiSettings>();
builder.Services.AddHealthChecks()
    .AddCheck<PartnerApiOptionsHealthCheck>(
        "partner-api-options",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(1));

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Run();
```

Registration order makes the contract visible: add the validator, bind the required section with strict unknown-key handling, request startup validation, register the last-valid-value owner, and finally register readiness. `ValidateOnStart` makes invalid initial configuration fail host startup instead of the first request. Liveness deliberately runs no dependency checks; readiness evaluates the reloadable options. Keep public health responses minimal and protect detailed diagnostics with network policy or authorization.

## Adopt only successfully validated reloads

```csharp
using Microsoft.Extensions.Options;

public sealed class PartnerApiSettings : IDisposable
{
    private PartnerApiOptions _current;
    private readonly IDisposable? _subscription;

    public PartnerApiSettings(IOptionsMonitor<PartnerApiOptions> monitor)
    {
        _current = monitor.CurrentValue;
        _subscription = monitor.OnChange((next, _) =>
            Volatile.Write(ref _current, next));
    }

    public PartnerApiOptions Current => Volatile.Read(ref _current);

    public void Dispose() => _subscription?.Dispose();
}
```

`IOptionsMonitor<T>` validates a newly materialized value before invoking `OnChange`, so this singleton atomically replaces its reference only with a successfully bound and validated instance. Consumers take one `Current` snapshot per operation and never mutate it. Retaining and disposing the subscription prevents a long-lived callback leak. If the options configure a disposable client, build the replacement completely before swapping, drain the old client safely, and define rollback behavior; changing this record alone cannot reconfigure an already-created client.

## Report invalid current configuration without exposing it

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

public sealed class PartnerApiOptionsHealthCheck(
    IOptionsMonitor<PartnerApiOptions> monitor) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _ = monitor.CurrentValue;
            return Task.FromResult(
                HealthCheckResult.Healthy("Partner API configuration is valid."));
        }
        catch (OptionsValidationException)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Partner API configuration is invalid."));
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Partner API configuration cannot be bound."));
        }
    }
}
```

The health check forces materialization of the current configuration after cache invalidation and converts only expected binding/validation failures into a sanitized readiness result. It does not return option values or exception details and does not contact the partner. Existing operations can continue using `PartnerApiSettings`' last valid snapshot while readiness removes the instance from new traffic; decide whether that availability policy fits the workload before adopting it.

For a reload-capable JSON file, configure the provider at the composition root, for example `AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)`. The default web host already loads its standard JSON files with reload support, but custom providers, mounted files, and container filesystems have different change-token behavior. Do not claim reload until the deployed environment has observed it.

## Failure modes and operations

| Signal or symptom | Interpretation | Action |
| --- | --- | --- |
| Host fails with `OptionsValidationException` | Initial configuration violates the contract | Correct the source; do not bypass `ValidateOnStart` |
| Host fails binding with an unknown key | Strict schema detected a typo or rollout-order mismatch | Coordinate application and configuration schema deployment |
| File changes but callback does not run | Provider reload is disabled/unsupported or filesystem events are not delivered | Verify provider settings and deployed filesystem behavior; restart when reload is not guaranteed |
| Readiness becomes unhealthy after reload | Current source cannot bind or validate; consumer still holds last valid snapshot | Roll back/fix configuration and observe recovery; alert on the transition, not every poll |
| Readiness is healthy but client behavior is stale | The consumer captured old values or the dependent resource was never rebuilt | Audit lifetimes and implement an atomic resource-replacement owner |

Observe reload success/failure counts, last successful adoption time, readiness transitions, and stable validator/rule identifiers. Never log complete options, API keys, connection strings, bearer tokens, or invalid raw values. Reload callbacks must stay fast, thread-safe, non-blocking, and safe when invoked repeatedly or concurrently.

## Verification checklist

Authoring verification for this recipe:

- [x] The registration, monitor subscriber, validator, health check, and endpoints were compiled in a temporary `net10.0` `Microsoft.NET.Sdk.Web` project with catalog package versions.
- [x] The authoring check started the host only with valid local configuration; it did not contact an external dependency.

Checks for the consuming application:

- [ ] Prove missing sections, unknown keys, invalid startup values, and secret-safe validation messages fail as intended.
- [ ] In the deployed configuration provider/filesystem, prove a valid change is adopted once and an invalid change makes readiness unhealthy without replacing the last valid snapshot.
- [ ] Prove correction restores readiness and updates the consumer without a process restart when reload is promised.
- [ ] Exercise concurrent requests and rapid repeated reloads; verify callback disposal and downstream resource replacement.
- [ ] Confirm liveness stays process-only and public health output contains no configuration values, topology, or exceptions.

## Related package guides

- [Microsoft.Extensions.Options](../packages/microsoft-extensions-options.md)
- [Microsoft.Extensions.Options.ConfigurationExtensions](../packages/microsoft-extensions-options-configurationextensions.md)
- [Microsoft.Extensions.Configuration.Binder](../packages/microsoft-extensions-configuration-binder.md)
- [Microsoft.Extensions.Diagnostics.HealthChecks](../packages/microsoft-extensions-diagnostics-healthchecks.md)
- [Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions](../packages/microsoft-extensions-diagnostics-healthchecks-abstractions.md)

## Primary sources

- [Microsoft.Extensions.Options 10.0.10 on NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Options/10.0.10) — Accessed 2026-07-27.
- [Microsoft options pattern guidance](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) — Accessed 2026-07-27.
- [ASP.NET Core 10 options guidance](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0) — Accessed 2026-07-27.
- [ASP.NET Core 10 health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0) — Accessed 2026-07-27.
