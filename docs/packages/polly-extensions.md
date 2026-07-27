# Polly.Extensions

## Catalog entry

`Polly.Extensions` **8.7.0** — companion catalog package; `IServiceCollection` registration and lookup integration for Polly resilience pipelines.

## Decision and scope

Use when a service needs a centrally named pipeline through DI. Keep the key part of the application dependency contract.

## Recommended registration and use

Register each pipeline once with `AddResiliencePipeline` and resolve it via `ResiliencePipelineProvider<TKey>` or the corresponding keyed service. Use a typed pipeline only where the result type is part of the strategy contract.

## Enterprise implementation guidance

Define keys in a shared application-owned location, avoid key strings scattered across features, and make each dependency's policy visible in composition-root code. Test replacement/reload behavior only if the host explicitly configures it.

## Integration with the catalog

This is the DI companion to `polly.md`. Use `microsoft-extensions-resilience.md` for Microsoft telemetry/enricher integration and `microsoft-extensions-http-resilience.md` for `HttpClient` handlers.

## Security, performance, AOT, trimming, and operations

Registration produces cached pipelines through the provider. Pipeline keys can select materially different operational behavior, so validate them as carefully as configuration names. Test the deployed trim/AOT artifact if DI or strategy configuration is reflection-sensitive.

## Avoid

Do not register multiple unrelated policies under an ambiguous key or resolve an untyped pipeline where a result-specific predicate is required.

## Verification checklist

- Build the provider and resolve every registered key.
- Assert a pipeline's intended strategy behavior through a controlled dependency fake.
- Verify metrics/logs carry enough dependency context to diagnose a key.

## Sources

- https://www.nuget.org/packages/Polly.Extensions/8.7.0 (Accessed 2026-07-27)
- https://www.pollydocs.org/getting-started.html (Accessed 2026-07-27)
- https://github.com/App-vNext/Polly/blob/main/docs/advanced/dependency-injection.md (Accessed 2026-07-27)
