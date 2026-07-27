# Microsoft.Extensions.Http

## Catalog entry

| Version | Role | Status |
| --- | --- | --- |
| `10.0.10` | `IHttpClientFactory`, named/typed client registration, and handler pooling | Approved outbound HTTP integration |

## Decision and scope

Use this package for DI-integrated outbound HTTP clients. It provides factory-managed handler pooling and named/typed client configuration; it does not define business API contracts or a resilience policy by itself.

## Recommended registration and use

Use a named client for shared external-service configuration or a typed client for a focused API adapter. Configure base address, headers, timeouts, and handlers in one owned registration. Typed clients are transient/short-lived; inject `IHttpClientFactory` into singleton services and create a client when needed. Add resilience through the catalogued `Microsoft.Extensions.Http.Resilience` integration, with service-specific timeout/retry/circuit-breaker policy.

## Enterprise implementation guidance

Give every external service a stable client name, ownership, endpoint configuration, authentication mechanism, retry budget, and telemetry. Use typed clients at anti-corruption boundaries and keep request/response mapping inside the client. Configure idempotency-aware resilience: never retry unsafe requests without an explicit contract.

## Integration with the catalog

Register through [DependencyInjection](microsoft-extensions-dependencyinjection.md). Bind client settings using [Options.ConfigurationExtensions](microsoft-extensions-options-configurationextensions.md) and validate them with [Options](microsoft-extensions-options.md). Use [Logging.Abstractions](microsoft-extensions-logging-abstractions.md) and health checks for dependency telemetry, not per-request health probing.

## Security, performance, AOT, trimming, and operations

Never accept unvalidated base addresses or forward sensitive headers across trust boundaries. Handler pooling means handlers can outlive a request; do not store request-specific state or cookies in shared handlers. Configure DNS/connection lifetime deliberately for the environment. Avoid logging credentials or sensitive response bodies. The registration model is static; reflection-based client discovery must be tested for trim/AOT.

## Avoid

- Do not create a new `HttpClient` handler per request.
- Do not separately register a typed client as a plain transient; that breaks its factory configuration.
- Do not inject a typed client into a singleton service.

## Verification checklist

- Client names/types, endpoints, auth, timeout, and handler lifetime are tested.
- Resilience policy is idempotency-aware and validated under timeout/DNS/5xx conditions.
- Logs and traces redact credentials and sensitive payloads.

## Sources

- [NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.Http) (Accessed 2026-07-27)
- [Use IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) (Accessed 2026-07-27)
- [IHttpClientFactory troubleshooting](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory-troubleshooting) (Accessed 2026-07-27)
