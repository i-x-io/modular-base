# Package selection and ownership

Use this guide before composing overlapping packages. A valid combination means
the packages have distinct responsibilities; it does not replace the individual
[package guides](../packages/README.md). Sources were accessed on **2026-07-27**.

## resilience-and-retry-ownership

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Resilient `HttpClient` | [`Microsoft.Extensions.Http.Resilience`](../packages/microsoft-extensions-http-resilience.md) owns handler-pipeline registration. | Reference it in the composition root that calls `AddStandardResilienceHandler` or `AddResilienceHandler`. | Combine with [`Microsoft.Extensions.Http`](../packages/microsoft-extensions-http.md); application code need not reference Polly merely to consume the handler. | The application wants Microsoft HTTP defaults and DI integration. | Adding another Polly retry around the same request, multiplying attempts and latency. |
| General/custom pipeline | [`Polly`](../packages/polly.md) owns `ResiliencePipeline`; [`Polly.Extensions`](../packages/polly-extensions.md) owns Microsoft DI/options integration. | Reference only where code constructs or consumes those APIs. | Custom Polly pipelines may coexist with Microsoft HTTP resilience for different operations; designate one retry owner per operation. | A non-HTTP operation or policy needs explicit strategies and ordering. | Retrying unsafe work or nesting retry owners. |

Primary sources: [Microsoft HTTP resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience), [Polly pipelines](https://www.pollydocs.org/pipelines/).

## test-platform-runners-and-coverage

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| xUnit v3 on Microsoft Testing Platform (MTP) | [`xunit.v3`](../packages/xunit-v3.md) supplies the framework and MTP executable integration. | Reference `xunit.v3` in test projects; do not add VSTest packages unless a tool requires that protocol. | MTP-native xUnit plus assertion, architecture, and Testcontainers libraries. | Default for new xUnit v3 tests in this catalog. | Assuming `Microsoft.NET.Test.Sdk` is required for every `dotnet test`. |
| VSTest compatibility | [`Microsoft.NET.Test.Sdk`](../packages/microsoft-net-test-sdk.md) owns VSTest host assets; [`xunit.runner.visualstudio`](../packages/xunit-runner-visualstudio.md) owns xUnit's adapter. | Add both only for an intentional VSTest path. | VSTest SDK + VS runner; it is an alternative execution path, not another framework. | An IDE, CI service, or extension requires VSTest. | Mixing MTP and VSTest switches and expecting identical extension behavior. |
| Coverage collector | [`coverlet.collector`](../packages/coverlet-collector.md) owns its VSTest data collector. | Reference only in projects using VSTest `--collect`. | Coverlet collector + VSTest SDK + compatible runner. | Coverage must use the VSTest collector protocol. | Treating it as an MTP-native extension. |

Primary sources: [xUnit v3](https://xunit.net/docs/getting-started/v3/getting-started), [.NET test platforms](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-intro).

## relational-test-fidelity

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Narrow non-relational double | [`Microsoft.EntityFrameworkCore.InMemory`](../packages/microsoft-entityframeworkcore-inmemory.md) owns an in-process, non-relational provider. | Reference only where tests knowingly do not assert relational behavior. | EF Core + InMemory for simple state-oriented legacy tests. | SQL translation, transactions, constraints, migrations, raw SQL, collation, and PostgreSQL features are outside the assertion. | Calling an InMemory test proof of PostgreSQL correctness. |
| PostgreSQL integration test | [`Testcontainers.PostgreSql`](../packages/testcontainers-postgresql.md) owns container lifecycle; the application owns EF/Npgsql registration. | Reference in integration-test projects; set an explicit PostgreSQL image policy in CI. | Testcontainers + Npgsql/EF provider + xUnit v3. | Tests must exercise production-provider behavior. | Sharing mutable databases between parallel tests or assuming Docker without a preflight. |

Primary source: [EF Core testing strategy](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy).

## postgresql-data-access

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Direct PostgreSQL access | [`Npgsql`](../packages/npgsql.md) owns `NpgsqlDataSource`, connections, commands, pooling, and type mapping. | Reference when application code directly uses Npgsql APIs. | Direct Npgsql can coexist with EF for deliberately separate paths; share deliberate data-source configuration. | SQL, COPY, batching, or low-level control is required. | Adding Npgsql only because EF already depends on it, then creating an unrelated pool. |
| EF Core PostgreSQL | [`Npgsql.EntityFrameworkCore.PostgreSQL`](../packages/npgsql.entityframeworkcore.postgresql.md) owns `UseNpgsql` and EF provider behavior. | Reference from EF infrastructure; its dependency supplies the driver unless direct driver APIs are used. | EF Core + provider + aligned extensions. | Persistence is expressed through EF Core. | Treating Npgsql alone as an EF provider or mismatching EF/provider majors. |

Primary source: [Npgsql EF Core provider](https://www.npgsql.org/efcore/).

## api-authentication-ownership

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Standard JWT bearer validation | [`Microsoft.AspNetCore.Authentication.JwtBearer`](../packages/microsoft-aspnetcore-authentication-jwtbearer.md) owns the ASP.NET Core handler and scheme validation. | Reference where the app calls `AddAuthentication().AddJwtBearer(...)`. | JWT bearer + FastEndpoints authorization, with authentication middleware before dependent endpoints. | The app owns issuer, audience, signature, lifetime, and challenge settings. | Incomplete validation or registering the same scheme twice. |
| FastEndpoints conveniences | [`FastEndpoints.Security`](../packages/fastendpoints-security.md) wraps ASP.NET Core auth and adds helpers. | Reference only when those helpers/token features are wanted; it does not replace the handler. | FastEndpoints.Security + FastEndpoints, with one scheme-registration owner. | The team chooses its documented convenience surface. | Calling its bearer helper and manual `AddJwtBearer` for the same scheme. |

Primary sources: [FastEndpoints security](https://fast-endpoints.com/docs/security), [ASP.NET Core JWT bearer](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).

## opentelemetry-composition

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Instrumentation API | [`OpenTelemetry.Api`](../packages/opentelemetry.api.md) owns telemetry-facing APIs. | Libraries reference the API, not hosting or exporters. | API in libraries; application supplies SDK later. | Emitting telemetry without choosing collection/export policy. | A reusable library registering global SDK/export policy. |
| SDK/host lifecycle | [`OpenTelemetry`](../packages/opentelemetry.md) owns providers/processors; [`OpenTelemetry.Extensions.Hosting`](../packages/opentelemetry.extensions.hosting.md) owns DI lifecycle. | Applications reference what they configure. | API + SDK + hosting is the hosted-app core. | The process collects telemetry and owns shutdown/flush. | Building duplicate providers for one signal. |
| Signal sources | Selected instrumentation owns subscriptions: [ASP.NET Core](../packages/opentelemetry.instrumentation.aspnetcore.md), [HTTP](../packages/opentelemetry.instrumentation.http.md), [runtime](../packages/opentelemetry.instrumentation.runtime.md), or [Npgsql](../packages/npgsql.opentelemetry.md). | Reference only enabled sources. | Multiple instrumentations can feed one provider. | The signal and attribute/cardinality policy are defined. | Assuming instrumentation exports data itself. |
| OTLP export | [`OpenTelemetry.Exporter.OpenTelemetryProtocol`](../packages/opentelemetry.exporter.opentelemetryprotocol.md) owns OTLP transport. | Reference in processes exporting via OTLP. | One SDK can use deliberately configured exporters. | A collector/backend accepts OTLP. | Exporting secrets/high-cardinality values or treating export as business delivery. |

Primary source: [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/instrumentation/).

## storage-abstraction-and-provider-sdks

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Portable blob operations | [`FluentStorage`](../packages/fluentstorage.md) owns common abstractions; the selected provider owns backend construction. | Reference core where consumed and selected providers in infrastructure. | Multiple providers only when selection is explicit. | Required operations fit the common interface and portability matters. | Assuming identical consistency, metadata, auth, retry, or rename semantics. |
| Provider-native features | AWS/Azure/GCP/MinIO/SFTP SDKs own vendor-specific APIs; FluentStorage providers depend on them. | Add direct SDK references only when app code calls their APIs or policy anchors the version. | Native SDK code can coexist behind an explicit boundary. | Leases, signed URLs, IAM, multipart controls, or diagnostics exceed the abstraction. | Leaking provider types or creating two retry owners. |

Primary source: [FluentStorage upstream](https://github.com/robinrodricks/FluentStorage).

## microsoft-abstractions-and-runtime-implementations

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Reusable-library contract | The relevant `Microsoft.Extensions.*.Abstractions` package owns interfaces/minimal shared types. | Reference the narrow abstraction package when its types are used; do not add concrete hosting/DI only for interfaces. | Abstraction in a library + runtime chosen by its application. | The library remains composition-neutral. | Expecting abstractions to register services or provide a working host/container. |
| Executable runtime | Concrete packages such as [`Microsoft.Extensions.Hosting`](../packages/microsoft-extensions-hosting.md), [`Microsoft.Extensions.DependencyInjection`](../packages/microsoft-extensions-dependencyinjection.md), and [`Microsoft.Extensions.Diagnostics.HealthChecks`](../packages/microsoft-extensions-diagnostics-healthchecks.md) own implementations. | Reference from the executable/composition root. | Implementations naturally depend on matching abstractions; align Microsoft versions. | The process owns lifetime, DI, diagnostics, or health execution. | Referencing both for symmetry or assuming package presence performs registration. |

Primary sources: [.NET dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection), [generic host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host).
