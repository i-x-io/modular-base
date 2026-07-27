# Microsoft.Extensions.Logging.Abstractions

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | Logging contracts such as `ILogger`, `ILoggerFactory`, and `LogLevel` | Approved library-facing abstraction |

## Decision and scope

Reference this package when application or library code needs structured logging contracts without selecting a provider. It does not configure console, OpenTelemetry, or other sinks; host/application composition supplies providers and filtering.

## Recommended registration and use

Inject `ILogger<TCategoryName>` into services and log structured properties with stable names. Use source-generated logging with `LoggerMessage` for hot paths or high-volume messages. Log at the boundary where an actionable event occurs, preserving exception objects rather than formatting exception text yourself.

## Enterprise implementation guidance

Define event categories, property naming, correlation identifiers, retention, redaction, and alert ownership. Treat logs as an operational interface: include enough context to diagnose failure without putting customer data, tokens, credentials, or raw request bodies into telemetry.

## Integration with the catalog

[Hosting](microsoft-extensions-hosting.md) configures the normal logging pipeline. [DependencyInjection.Abstractions](microsoft-extensions-dependencyinjection-abstractions.md) supplies constructor injection. HTTP and health-check integrations should log outcomes with the same privacy rules.

## Security, performance, AOT, trimming, and operations

Use structured logging rather than interpolation to preserve queryable fields and avoid formatting when disabled. Source-generated logging reduces boxing/parsing overhead and is trimming/AOT-friendly. Validate log filtering and redaction in the deployed provider, because abstractions cannot enforce sink policy.

## Avoid

- Do not log secrets, authorization headers, access tokens, or unrestricted personal data.
- Do not use exceptions for control-flow logging at high volume.
- Do not build dynamic categories or property keys from untrusted input.

## Verification checklist

- Important success/failure events include stable, sanitized structured properties.
- Redaction and filtering are verified with the production-equivalent sink.
- High-volume messages use appropriate log levels and source generation where warranted.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions) (Accessed 2026-07-27)
- [Logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging) (Accessed 2026-07-27)
- [Compile-time logging source generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator) (Accessed 2026-07-27)
