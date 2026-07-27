# Microsoft.Extensions.Logging.Abstractions

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Logging contracts such as `ILogger`, `ILoggerFactory`, and `LogLevel` | Approved library-facing abstraction |

| Documentation owner | Last reviewed | Review trigger |
| --- | --- | --- |
| IX | 2026-07-27 | Package-version, target-framework, logging contract, source generator, or telemetry privacy-policy change |

## Decision and scope

Reference this package when application or library code needs structured logging contracts without selecting a provider. It does not configure console, OpenTelemetry, or other sinks; host/application composition supplies providers and filtering.

## Recommended registration and use

With Central Package Management, a library references the abstractions without selecting a provider:

```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
```

Inject `ILogger<TCategoryName>` and use stable structured-property names. For hot or high-volume paths, define source-generated logging methods in a partial type:

```csharp
using Microsoft.Extensions.Logging;

internal static partial class OrderLog
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Order {OrderId} completed in {ElapsedMs} ms")]
    internal static partial void Completed(
        ILogger logger, Guid orderId, long elapsedMs);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Order {OrderId} failed")]
    internal static partial void Failed(
        ILogger logger, Guid orderId, Exception exception);
}

public sealed class OrderProcessor(ILogger<OrderProcessor> logger)
{
    public void RecordSuccess(Guid orderId, long elapsedMs) =>
        OrderLog.Completed(logger, orderId, elapsedMs);

    public void RecordFailure(Guid orderId, Exception exception) =>
        OrderLog.Failed(logger, orderId, exception);
}
```

The first `Exception` parameter is handled specially by the generator and should not also appear as a message-template placeholder. Log at the boundary where an actionable event occurs, preserving the exception object rather than formatting its text yourself. Application composition, not this package, selects providers and filtering.

## Enterprise implementation guidance

Define event IDs, categories, property naming, correlation identifiers, retention, redaction, and alert ownership. Treat logs as an operational interface: include enough context to diagnose failure without putting customer data, tokens, credentials, or raw request bodies into telemetry.

A common workflow is to emit one completion event at an owned boundary, attach stable business-safe identifiers, and let distributed tracing carry request correlation. Use `BeginScope` only for values that genuinely apply to all nested events and always dispose the returned scope. Choose levels by operator action: `Information` for meaningful state transitions, `Warning` for recoverable abnormal conditions, and `Error` for failed operations. Avoid logging the same exception at every layer.

### Upgrade and rollback

Keep abstractions compatible with the host's concrete logging providers and exporters. Rebuild source-generated logging methods and verify event IDs, levels, scopes, structured-property names, redaction, and sink filtering after upgrade. No data migration is required, but schema changes can break dashboards and alerts. Roll back the application/provider set together and preserve stable event fields during mixed-version deployment.

## Integration with the catalog

[Hosting](microsoft-extensions-hosting.md) configures the normal logging pipeline. [DependencyInjection.Abstractions](microsoft-extensions-dependencyinjection-abstractions.md) supplies constructor injection. HTTP and health-check integrations should log outcomes with the same privacy rules.

Use the [abstraction-versus-runtime selection guide](../package-guidance/package-selection.md#microsoft-abstractions-and-runtime-implementations) when selecting providers at the host boundary. See the [supply-chain entry](../package-guidance/supply-chain.md#microsoft-extensions-logging-abstractions).

## Security, performance, AOT, trimming, and operations

Use structured templates rather than interpolation to preserve queryable fields and avoid unnecessary formatting. Source-generated logging parses templates at compile time, reduces boxing/temporary allocations, and is trimming/AOT-friendly. Guard expensive argument construction with `logger.IsEnabled(level)`; source generation cannot avoid evaluating a method argument before the call. Keep property cardinality bounded, especially user-controlled IDs and error text. Validate filtering, sampling, scope inclusion, redaction, and retention in the deployed provider because abstractions cannot enforce sink policy.

## Avoid

- Do not log secrets, authorization headers, access tokens, or unrestricted personal data.
- Do not use exceptions for control-flow logging at high volume.
- Do not build dynamic categories or property keys from untrusted input.

## Verification checklist

- [ ] The library has a versionless abstractions reference and restores catalog version `10.0.10` without selecting a provider.
- [ ] Important success/failure events use stable event IDs and sanitized structured properties.
- [ ] Exceptions are preserved as exception parameters and are not duplicated across layers.
- [ ] Redaction, filtering, scopes, and retention are verified with the production-equivalent sink.
- [ ] High-volume messages use appropriate levels, bounded-cardinality fields, and source generation; expensive arguments are guarded.

## Sources

- [NuGet: Microsoft.Extensions.Logging.Abstractions 10.0.10](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/10.0.10) (Accessed 2026-07-27)
- [Logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging) (Accessed 2026-07-27)
- [Compile-time logging source generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator) (Accessed 2026-07-27)
- [Logging guidance for .NET library authors](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging-library-authors) (Accessed 2026-07-27)
