# Illustrated package recipes

These recipes compose packages from the central catalog into focused workflows.
They are explanatory documentation, not permanent sample applications. Every
project reference is versionless because `Directory.Packages.props` remains the
version authority.

- [Secure FastEndpoints API](fastendpoints-jwt-openapi-scalar.md) composes JWT
  validation, endpoint authorization, OpenAPI generation, and Scalar.
- [FastEndpoints validation and results](fastendpoints-validation-results.md)
  maps FluentValidation failures and FluentResults outcomes at the API boundary.
- [EF Core and Npgsql exception mapping](efcore-npgsql-exception-mapping.md)
  combines provider registration, naming conventions, migrations, and stable
  PostgreSQL failure mapping.
- [Pgvector hybrid ranking](pgvector-hybrid-ranking.md) combines vector distance
  with a secondary relevance signal while retaining deterministic ordering.
- [OpenTelemetry with PostgreSQL and OTLP](opentelemetry-otlp-postgresql.md)
  composes traces, metrics, runtime instrumentation, Npgsql, and OTLP export.
- [Resilient typed HttpClient](resilient-typed-httpclient.md) assigns retry
  ownership to one HTTP resilience pipeline.
- [PostgreSQL and Redis Testcontainers](testcontainers-postgresql-redis-xunit.md)
  builds isolated xUnit v3 integration fixtures for both services.
- [Durable mail outbox](durable-mail-outbox.md) separates MimeKit message
  creation, MailKit transport, and durable retry state.
- [Portable FluentStorage transfer](fluentstorage-portable-transfer.md) keeps
  application transfer logic behind one selected provider boundary.
- [Options, reload, and health](options-validation-reload-health.md) combines
  startup validation, intentional reload behavior, and health reporting.

Package-selection boundaries are documented in the
[package-selection guide](../package-guidance/package-selection.md). Each recipe
also links to its package-specific guides and primary sources.
