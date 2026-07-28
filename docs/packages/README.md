# Package documentation index

This index is maintained manually from [`Directory.Packages.props`](../../Directory.Packages.props). Keep it 1:1 with the central catalog: every catalog package must have exactly one entry here and one package guide.

`Adoption` states whether and how a project may use a package. `Catalog mechanism` states only how the version or produced artifact enters the repository; a `PackageVersion` pin does not by itself authorize adoption. Package-specific role restrictions and composition requirements remain authoritative in the linked guide.

Supporting references are maintained outside this one-to-one package index:

- [Package selection guide](../package-guidance/package-selection.md) — ownership boundaries and supported package combinations.
- [Supply-chain reference](../package-guidance/supply-chain.md) — objective identity, dependency, lifecycle, advisory, and provenance facts.
- [Illustrated recipes](../recipes/README.md) — explained multi-package composition workflows.

## Core utilities, validation, mail, and resilience

| Package | Pinned version | Adoption | Catalog mechanism |
| --- | ---: | --- | --- |
| [`AngleSharp`](anglesharp.md) | `1.6.0` | Direct | PackageVersion |
| [`DeviceDetector.NET`](devicedetector-net.md) | `6.5.0` | Direct | PackageVersion |
| [`Enums.NET`](enums-net.md) | `5.0.0` | Direct | PackageVersion |
| [`FluentResults`](fluentresults.md) | `4.0.0` | Direct | PackageVersion |
| [`FluentValidation`](fluentvalidation.md) | `12.1.1` | Direct | PackageVersion |
| [`FluentValidation.DependencyInjectionExtensions`](fluentvalidation-dependencyinjectionextensions.md) | `12.1.1` | Companion | PackageVersion |
| [`Humanizer.Core`](humanizer-core.md) | `3.0.10` | Direct | PackageVersion |
| [`MailKit`](mailkit.md) | `4.17.0` | Direct | PackageVersion |
| [`Microsoft.Extensions.Http.Resilience`](microsoft-extensions-http-resilience.md) | `10.8.0` | Direct | PackageVersion |
| [`Microsoft.Extensions.Resilience`](microsoft-extensions-resilience.md) | `10.8.0` | Direct | PackageVersion |
| [`MimeKit`](mimekit.md) | `4.17.0` | Direct | PackageVersion |
| [`Polly`](polly.md) | `8.7.0` | Direct | PackageVersion |
| [`Polly.Extensions`](polly-extensions.md) | `8.7.0` | Companion | PackageVersion |
| [`Scrutor`](scrutor.md) | `7.0.0` | Direct | PackageVersion |
| [`YamlDotNet`](yamldotnet.md) | `18.1.0` | Direct | PackageVersion |

## Documentation, benchmarking, and compiler-tooling development

| Package | Pinned version | Adoption | Catalog mechanism |
| --- | ---: | --- | --- |
| [`BenchmarkDotNet`](benchmarkdotnet.md) | `0.15.8` | Direct | PackageVersion |
| [`Markdig`](markdig.md) | `1.3.2` | Direct | PackageVersion |
| [`Microsoft.CodeAnalysis.Analyzers`](microsoft-codeanalysis-analyzers.md) | `5.6.0` | Direct | PackageVersion |
| [`Microsoft.CodeAnalysis.Common`](microsoft-codeanalysis-common.md) | `5.6.0` | Direct | PackageVersion |
| [`Microsoft.CodeAnalysis.CSharp`](microsoft-codeanalysis-csharp.md) | `5.6.0` | Direct | PackageVersion |

## Produced package: consumers opt in deliberately

| Package | Version | Adoption | Catalog mechanism |
| --- | ---: | --- | --- |
| [`IX.Modularity.Analyzers`](ix-modularity-analyzers.md) | repository release version | Produced package | Produced here |

## Microsoft.Extensions foundation

| Package | Pinned version | Adoption | Catalog mechanism |
| --- | ---: | --- | --- |
| [`Microsoft.Extensions.Caching.StackExchangeRedis`](microsoft-extensions-caching-stackexchangeredis.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.Extensions.Configuration.Abstractions`](microsoft-extensions-configuration-abstractions.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.Extensions.Configuration.Binder`](microsoft-extensions-configuration-binder.md) | `10.0.10` | Companion | PackageVersion |
| [`Microsoft.Extensions.DependencyInjection`](microsoft-extensions-dependencyinjection.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.Extensions.DependencyInjection.Abstractions`](microsoft-extensions-dependencyinjection-abstractions.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.Extensions.DependencyModel`](microsoft-extensions-dependencymodel.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.Extensions.Diagnostics.HealthChecks`](microsoft-extensions-diagnostics-healthchecks.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions`](microsoft-extensions-diagnostics-healthchecks-abstractions.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.Extensions.Hosting`](microsoft-extensions-hosting.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.Extensions.Hosting.Abstractions`](microsoft-extensions-hosting-abstractions.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.Extensions.Http`](microsoft-extensions-http.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.Extensions.Logging.Abstractions`](microsoft-extensions-logging-abstractions.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.Extensions.Options`](microsoft-extensions-options.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.Extensions.Options.ConfigurationExtensions`](microsoft-extensions-options-configurationextensions.md) | `10.0.10` | Companion | PackageVersion |
| [`Microsoft.Extensions.TimeProvider.Testing`](microsoft-extensions-timeprovider-testing.md) | `10.8.0` | Direct | PackageVersion |

## ASP.NET Core, FastEndpoints, OpenAPI, and API infrastructure

| Package | Pinned version | Adoption | Catalog mechanism |
| --- | ---: | --- | --- |
| [`Asp.Versioning.Http`](asp-versioning-http.md) | `10.0.0` | Catalog-only | PackageVersion |
| [`FastEndpoints`](fastendpoints.md) | `8.2.0` | Catalog-only | PackageVersion |
| [`FastEndpoints.Generator`](fastendpoints-generator.md) | `8.2.0` | Catalog-only | PackageVersion |
| [`FastEndpoints.OpenApi`](fastendpoints-openapi.md) | `8.2.0` | Catalog-only | PackageVersion |
| [`FastEndpoints.Security`](fastendpoints-security.md) | `8.2.0` | Catalog-only | PackageVersion |
| [`FastEndpoints.Testing`](fastendpoints-testing.md) | `8.2.0` | Catalog-only | PackageVersion |
| [`Microsoft.AspNetCore.Authentication.JwtBearer`](microsoft-aspnetcore-authentication-jwtbearer.md) | `10.0.10` | Catalog-only | PackageVersion |
| [`Microsoft.AspNetCore.Mvc.Testing`](microsoft-aspnetcore-mvc-testing.md) | `10.0.10` | Catalog-only | PackageVersion |
| [`Microsoft.AspNetCore.OpenApi`](microsoft-aspnetcore-openapi.md) | `10.0.10` | Companion | PackageVersion |
| [`Microsoft.OpenApi`](microsoft-openapi.md) | `2.11.0` | Companion | PackageVersion |
| [`Scalar.AspNetCore`](scalar-aspnetcore.md) | `2.16.16` | Catalog-only | PackageVersion |

## EF Core, PostgreSQL, specifications, search, and pagination

| Package | Pinned version | Adoption | Catalog mechanism |
| --- | ---: | --- | --- |
| [`Ardalis.Specification`](ardalis-specification.md) | `9.3.1` | Direct | PackageVersion |
| [`Ardalis.Specification.EntityFrameworkCore`](ardalis-specification-entityframeworkcore.md) | `9.3.1` | Companion | PackageVersion |
| [`EFCore.NamingConventions`](efcore-namingconventions.md) | `10.0.1` | Companion | PackageVersion |
| [`EntityFrameworkCore.Exceptions.PostgreSQL`](entityframeworkcore-exceptions-postgresql.md) | `10.0.1` | Companion | PackageVersion |
| [`Microsoft.EntityFrameworkCore`](microsoft-entityframeworkcore.md) | `10.0.10` | Direct | PackageVersion |
| [`Microsoft.EntityFrameworkCore.Design`](microsoft-entityframeworkcore-design.md) | `10.0.10` | Companion | PackageVersion |
| [`Microsoft.EntityFrameworkCore.InMemory`](microsoft-entityframeworkcore-inmemory.md) | `10.0.10` | Companion | PackageVersion |
| [`Microsoft.EntityFrameworkCore.Relational`](microsoft-entityframeworkcore-relational.md) | `10.0.10` | Companion | PackageVersion |
| [`MR.EntityFrameworkCore.KeysetPagination`](mr-entityframeworkcore-keysetpagination.md) | `1.6.0` | Companion | PackageVersion |
| [`Npgsql`](npgsql.md) | `10.0.3` | Direct | PackageVersion |
| [`Npgsql.EntityFrameworkCore.PostgreSQL`](npgsql.entityframeworkcore.postgresql.md) | `10.0.3` | Companion | PackageVersion |
| [`Npgsql.OpenTelemetry`](npgsql.opentelemetry.md) | `10.0.3` | Companion | PackageVersion |
| [`Pgvector`](pgvector.md) | `0.3.2` | Companion | PackageVersion |
| [`Pgvector.EntityFrameworkCore`](pgvector.entityframeworkcore.md) | `0.3.0` | Companion | PackageVersion |

## FluentStorage core and approved enterprise providers

| Package | Pinned version | Adoption | Catalog mechanism |
| --- | ---: | --- | --- |
| [`FluentStorage`](fluentstorage.md) | `8.0.16` | Direct | PackageVersion |
| [`FluentStorage.AWS`](fluentstorage-aws.md) | `8.0.10` | Companion | PackageVersion |
| [`FluentStorage.Azure.Blobs`](fluentstorage-azure-blobs.md) | `8.0.10` | Companion | PackageVersion |
| [`FluentStorage.Azure.Files`](fluentstorage-azure-files.md) | `8.0.10` | Companion | PackageVersion |
| [`FluentStorage.GCP`](fluentstorage-gcp.md) | `8.0.14` | Companion | PackageVersion |
| [`FluentStorage.Minio`](fluentstorage-minio.md) | `8.0.10` | Companion | PackageVersion |
| [`FluentStorage.SFTP`](fluentstorage-sftp.md) | `8.0.16` | Companion | PackageVersion |

## Observability

| Package | Pinned version | Adoption | Catalog mechanism |
| --- | ---: | --- | --- |
| [`OpenTelemetry`](opentelemetry.md) | `1.17.0` | Direct | PackageVersion |
| [`OpenTelemetry.Api`](opentelemetry.api.md) | `1.17.0` | Direct | PackageVersion |
| [`OpenTelemetry.Exporter.OpenTelemetryProtocol`](opentelemetry.exporter.opentelemetryprotocol.md) | `1.17.0` | Companion | PackageVersion |
| [`OpenTelemetry.Extensions.Hosting`](opentelemetry.extensions.hosting.md) | `1.17.0` | Companion | PackageVersion |
| [`OpenTelemetry.Instrumentation.AspNetCore`](opentelemetry.instrumentation.aspnetcore.md) | `1.17.0` | Companion | PackageVersion |
| [`OpenTelemetry.Instrumentation.Http`](opentelemetry.instrumentation.http.md) | `1.17.0` | Companion | PackageVersion |
| [`OpenTelemetry.Instrumentation.Runtime`](opentelemetry.instrumentation.runtime.md) | `1.17.0` | Companion | PackageVersion |

## Testing: SpecsFor is intentionally catalog-only and is the sole prerelease dependency

| Package | Pinned version | Adoption | Catalog mechanism |
| --- | ---: | --- | --- |
| [`AwesomeAssertions`](awesomeassertions.md) | `9.5.0` | Direct | PackageVersion |
| [`coverlet.collector`](coverlet-collector.md) | `10.0.1` | Catalog-only | PackageVersion |
| [`Microsoft.NET.Test.Sdk`](microsoft-net-test-sdk.md) | `18.8.1` | Catalog-only | PackageVersion |
| [`SpecsFor`](specsfor.md) | `8.0.0-rc2a` | Catalog-only | PackageVersion |
| [`Testcontainers.PostgreSql`](testcontainers-postgresql.md) | `4.13.0` | Direct | PackageVersion |
| [`Testcontainers.Redis`](testcontainers-redis.md) | `4.13.0` | Direct | PackageVersion |
| [`TngTech.ArchUnitNET.xUnitV3`](tngtech-archunitnet-xunitv3.md) | `0.13.3` | Direct | PackageVersion |
| [`xunit.runner.visualstudio`](xunit-runner-visualstudio.md) | `3.1.5` | Catalog-only | PackageVersion |
| [`xunit.v3`](xunit-v3.md) | `3.2.2` | Direct | PackageVersion |

## Project-scoped analyzer: packable projects opt in and own their PublicAPI files

| Package | Pinned version | Adoption | Catalog mechanism |
| --- | ---: | --- | --- |
| [`Microsoft.CodeAnalysis.PublicApiAnalyzers`](microsoft-codeanalysis-publicapianalyzers.md) | `5.6.0` | Direct | PackageVersion |

## Universal analyzers: build-only and never flow into package consumers

| Package | Pinned version | Adoption | Catalog mechanism |
| --- | ---: | --- | --- |
| [`Meziantou.Analyzer`](meziantou-analyzer.md) | `3.0.132` | Global analyzer | GlobalPackageReference |
| [`Microsoft.CodeAnalysis.BannedApiAnalyzers`](microsoft-codeanalysis-bannedapianalyzers.md) | `5.6.0` | Global analyzer | GlobalPackageReference |
| [`Microsoft.VisualStudio.Threading.Analyzers`](microsoft-visualstudio-threading-analyzers.md) | `18.7.23` | Global analyzer | GlobalPackageReference |
| [`Roslynator.Analyzers`](roslynator-analyzers.md) | `4.15.0` | Global analyzer | GlobalPackageReference |
| [`SonarAnalyzer.CSharp`](sonaranalyzer-csharp.md) | `10.30.0.144632` | Global analyzer | GlobalPackageReference |
