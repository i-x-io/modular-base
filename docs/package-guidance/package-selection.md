# Package selection and ownership

Use this guide before composing overlapping packages. A valid combination means
the packages have distinct responsibilities; it does not replace the individual
[package guides](../packages/README.md). `Directory.Packages.props` is the version
authority, but each consuming project decides which packages its behavior
requires and adds those references explicitly. Sources were accessed on
**2026-07-27**.

## Validation and expected results

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Validation rules | [`FluentValidation`](../packages/fluentvalidation.md) owns validator definitions and execution. | Reference it wherever code defines or invokes `IValidator<T>`. | FluentValidation may be used alone or with its DI companion. | Inputs or commands need explicit, reusable validation rules. | Treating validation as authorization, a uniqueness guarantee, or a transactional invariant. |
| Validator discovery | [`FluentValidation.DependencyInjectionExtensions`](../packages/fluentvalidation-dependencyinjectionextensions.md) owns assembly scanning and Microsoft DI registration. | Reference it only where the service collection is composed; keep assembly selection and lifetimes explicit. | Combine with FluentValidation when scanner-based registration is useful. | The application wants conventional DI discovery instead of individual registrations. | Scanning arbitrary assemblies or giving validators lifetimes incompatible with their dependencies. |
| Expected operation outcomes | [`FluentResults`](../packages/fluentresults.md) owns `Result` and `Result<T>` values. | Reference it wherever those result types are part of an implementation or contract. | Results can carry failures produced after validation; neither package depends on the other. | Callers must distinguish success from expected, actionable failure without exceptions. | Converting programming faults, cancellation, corrupt state, or infrastructure failures into ordinary failed results. |

## API, OpenAPI, and versioning

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| FastEndpoints APIs | [`FastEndpoints`](../packages/fastendpoints.md) owns endpoint discovery, binding, execution, and endpoint metadata. | Reference it in projects that declare or compose FastEndpoints endpoints. | Pair with FastEndpoints OpenAPI, security, testing, or generator packages only for the selected capabilities. | The application chooses FastEndpoints as its HTTP endpoint framework. | Assuming the framework replaces ASP.NET Core authentication or authorization middleware. |
| FastEndpoints documents | [`FastEndpoints.OpenApi`](../packages/fastendpoints-openapi.md) owns FastEndpoints-aware document registration. | Reference it where the FastEndpoints OpenAPI pipeline is composed. | Combine with FastEndpoints and optionally Scalar; let it own the document pipeline. | Generated documents must reflect FastEndpoints metadata and transformers. | Registering a second raw ASP.NET Core document for the same contract. |
| Raw ASP.NET Core documents | [`Microsoft.AspNetCore.OpenApi`](../packages/microsoft-aspnetcore-openapi.md) owns `AddOpenApi` and `MapOpenApi`; [`Microsoft.OpenApi`](../packages/microsoft-openapi.md) supplies document model types. | Reference the ASP.NET Core package for a raw pipeline; add Microsoft.OpenApi directly only when code uses its types. | Combine with Scalar for a UI, or use separately from a deliberately distinct FastEndpoints document. | Minimal APIs or other ASP.NET Core endpoints need first-party document generation. | Adding Microsoft.OpenApi as though it registers or serves documents. |
| Interactive API reference | [`Scalar.AspNetCore`](../packages/scalar-aspnetcore.md) owns the browser UI over an existing OpenAPI document. | Reference it in the web host that maps the UI. | Combine with either document-generation path and keep names/routes aligned. | Users need an interactive reference, normally in development or behind explicit protection. | Assuming Scalar generates the document or exposing it publicly without a security decision. |
| HTTP contract versions | [`Asp.Versioning.Http`](../packages/asp-versioning-http.md) owns HTTP version readers and routing metadata. | Reference it where HTTP versioning is registered and mapped. | Combine with the chosen endpoint and document pipeline after defining one public reader strategy. | The external API contract needs multiple explicit versions. | Confusing HTTP API versions with document release labels or framework-specific endpoint versions. |

## PostgreSQL queries, paging, and vectors

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Reusable query intent | [`Ardalis.Specification`](../packages/ardalis-specification.md) owns provider-neutral specification models; [`Ardalis.Specification.EntityFrameworkCore`](../packages/ardalis-specification-entityframeworkcore.md) owns EF evaluation. | Reference the core package where specifications are defined and the EF companion where they are executed. | Combine both for EF-backed specifications after verifying compatibility with the selected EF/provider versions. | Several callers need the same bounded filters, ordering, projection, or paging intent. | Treating a specification as proof that a query is supported or efficient on PostgreSQL. |
| PostgreSQL naming | [`EFCore.NamingConventions`](../packages/efcore-namingconventions.md) owns model-to-database naming conversion. | Reference and configure it in the EF Core PostgreSQL setup. | Combine with the Npgsql EF provider and migrations. | The schema deliberately uses a convention such as snake case. | Enabling it against an existing schema without reviewing the resulting migration. |
| Constraint-error classification | [`EntityFrameworkCore.Exceptions.PostgreSQL`](../packages/entityframeworkcore-exceptions-postgresql.md) owns PostgreSQL-specific EF update exception classification. | Reference it where Npgsql EF options are configured. | Combine with the Npgsql EF provider; translate classified exceptions at the application boundary. | Known constraint failures must become explicit application outcomes. | Treating every database error as a safe or expected conflict. |
| Keyset pagination | [`MR.EntityFrameworkCore.KeysetPagination`](../packages/mr-entityframeworkcore-keysetpagination.md) owns keyset predicate construction for EF queries. | Reference it where stable ordered pages are built. | Combine with EF Core and Npgsql; align the ordering with a supporting index. | Large or changing result sets need stable next/previous navigation. | Using a non-unique ordering or exposing an unsigned cursor as trusted input. |
| Direct vector access | [`Pgvector`](../packages/pgvector.md) owns Npgsql vector mappings and vector values. | Reference it when direct Npgsql code stores or queries vectors. | Combine with Npgsql and a provisioned PostgreSQL `vector` extension. | SQL or low-level data access owns vector operations. | Omitting dimension checks, extension provisioning, or an explicit distance/index policy. |
| EF vector access | [`Pgvector.EntityFrameworkCore`](../packages/pgvector.entityframeworkcore.md) owns EF model, migration, index, and LINQ integration. | Reference it where EF owns vector schema and queries. | Combine with Pgvector and the Npgsql EF provider, registering vector support in both data-source and EF options. | EF Core owns vector persistence and migrations. | Assuming the package supplies hybrid search, ranking policy, or extension deployment automatically. |

## Mail and MIME

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Message construction and parsing | [`MimeKit`](../packages/mimekit.md) owns MIME messages, addresses, bodies, headers, and attachments. | Reference it wherever messages are created or parsed. | Use alone for MIME processing or combine with MailKit for transport. | Correct structured MIME handling is required without a transport dependency. | Building MIME by string concatenation or expecting MimeKit to deliver messages. |
| SMTP, IMAP, and POP | [`MailKit`](../packages/mailkit.md) owns protocol clients and connection/authentication lifecycles. | Reference it where mail transport is implemented. | Combine with MimeKit, whose message model MailKit consumes. | The application directly sends or receives mail through standard protocols. | Treating a successful SMTP call as an outbox, retry policy, or end-to-end delivery guarantee. |

## Compiler and analyzer tooling

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Compiler API | [`Microsoft.CodeAnalysis.Common`](../packages/microsoft-codeanalysis-common.md) owns language-neutral Roslyn APIs; [`Microsoft.CodeAnalysis.CSharp`](../packages/microsoft-codeanalysis-csharp.md) owns C# syntax and compilation APIs. | Reference them privately only in compiler-tooling implementations or isolated tests, keeping their versions aligned. | Combine Common and CSharp for C# tooling; add authoring analyzers for implementation feedback. | Building an analyzer, source generator, or compiler-based tool. | Shipping Roslyn assemblies as a normal runtime or transitive library dependency. |
| Authoring diagnostics | [`Microsoft.CodeAnalysis.Analyzers`](../packages/microsoft-codeanalysis-analyzers.md) checks analyzer and generator implementations. | Reference it as a private build-time dependency in compiler-tooling projects. | Combine with the aligned Roslyn compiler API packages. | Compiler tooling needs standard Roslyn authoring guidance. | Treating authoring diagnostics as runtime behavior or mixing incompatible Roslyn versions. |
| Repository-wide forbidden APIs | [`Microsoft.CodeAnalysis.BannedApiAnalyzers`](../packages/microsoft-codeanalysis-bannedapianalyzers.md) consumes the repository's `BannedSymbols.txt`. | It is already supplied globally; do not duplicate it in consuming projects. | Combine with other analyzers because it owns only explicit forbidden-symbol rules. | A symbol must be prohibited with a documented replacement. | Inventing a custom diagnostic when the standard banned-API mechanism expresses the rule. |
| Public API baselines | [`Microsoft.CodeAnalysis.PublicApiAnalyzers`](../packages/microsoft-codeanalysis-publicapianalyzers.md) compares public symbols with project-owned baseline files. | Packable libraries opt in explicitly with a private analyzer reference and `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt`. | Combine with package validation for complementary source and binary compatibility checks. | A library treats its public API as a reviewed release contract. | Applying it indiscriminately to applications or assuming a central version activates it. |
| Framework source generation | [`FastEndpoints.Generator`](../packages/fastendpoints-generator.md) owns FastEndpoints compile-time discovery and generated metadata. | Reference it privately in each project that declares the endpoints it must inspect. | Combine with FastEndpoints when source-generated startup, permissions, serialization, or AOT support is selected. | FastEndpoints needs its framework-specific generated artifacts. | Referencing it only from a host that cannot see endpoint declarations, or treating it as a general Roslyn authoring library. |

## Resilience and retry ownership

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Resilient `HttpClient` | [`Microsoft.Extensions.Http.Resilience`](../packages/microsoft-extensions-http-resilience.md) owns handler-pipeline registration. | Reference it in the composition root that calls `AddStandardResilienceHandler` or `AddResilienceHandler`. | Combine with [`Microsoft.Extensions.Http`](../packages/microsoft-extensions-http.md); application code need not reference Polly merely to consume the handler. | The application wants Microsoft HTTP defaults and DI integration. | Adding another Polly retry around the same request, multiplying attempts and latency. |
| General/custom pipeline | [`Polly`](../packages/polly.md) owns `ResiliencePipeline`; [`Polly.Extensions`](../packages/polly-extensions.md) owns Microsoft DI/options integration. | Reference only where code constructs or consumes those APIs. | Custom Polly pipelines may coexist with Microsoft HTTP resilience for different operations; designate one retry owner per operation. | A non-HTTP operation or policy needs explicit strategies and ordering. | Retrying unsafe work or nesting retry owners. |

Primary sources: [Microsoft HTTP resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience), [Polly pipelines](https://www.pollydocs.org/pipelines/).

## Test platform, runners, and coverage

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| xUnit v3 on Microsoft Testing Platform (MTP) | [`xunit.v3`](../packages/xunit-v3.md) supplies the framework and MTP executable integration. | Reference `xunit.v3` in test projects; do not add VSTest packages unless a tool requires that protocol. | MTP-native xUnit plus assertion, architecture, and Testcontainers libraries. | Default for new xUnit v3 tests in this catalog. | Assuming `Microsoft.NET.Test.Sdk` is required for every `dotnet test`. |
| VSTest compatibility | [`Microsoft.NET.Test.Sdk`](../packages/microsoft-net-test-sdk.md) owns VSTest host assets; [`xunit.runner.visualstudio`](../packages/xunit-runner-visualstudio.md) owns xUnit's adapter. | Add both only for an intentional VSTest path. | VSTest SDK + VS runner; it is an alternative execution path, not another framework. | An IDE, CI service, or extension requires VSTest. | Mixing MTP and VSTest switches and expecting identical extension behavior. |
| Coverage collector | [`coverlet.collector`](../packages/coverlet-collector.md) owns its VSTest data collector. | Reference only in projects using VSTest `--collect`. | Coverlet collector + VSTest SDK + compatible runner. | Coverage must use the VSTest collector protocol. | Treating it as an MTP-native extension. |

Primary sources: [xUnit v3](https://xunit.net/docs/getting-started/v3/getting-started), [.NET test platforms](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-intro).

## Relational test fidelity

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Narrow non-relational double | [`Microsoft.EntityFrameworkCore.InMemory`](../packages/microsoft-entityframeworkcore-inmemory.md) owns an in-process, non-relational provider. | Reference only where tests knowingly do not assert relational behavior. | EF Core + InMemory for simple state-oriented legacy tests. | SQL translation, transactions, constraints, migrations, raw SQL, collation, and PostgreSQL features are outside the assertion. | Calling an InMemory test proof of PostgreSQL correctness. |
| PostgreSQL integration test | [`Testcontainers.PostgreSql`](../packages/testcontainers-postgresql.md) owns container lifecycle; the application owns EF/Npgsql registration. | Reference in integration-test projects; set an explicit PostgreSQL image policy in CI. | Testcontainers + Npgsql/EF provider + xUnit v3. | Tests must exercise production-provider behavior. | Sharing mutable databases between parallel tests or assuming Docker without a preflight. |

Primary source: [EF Core testing strategy](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy).

## PostgreSQL data access

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Direct PostgreSQL access | [`Npgsql`](../packages/npgsql.md) owns `NpgsqlDataSource`, connections, commands, pooling, and type mapping. | Reference when application code directly uses Npgsql APIs. | Direct Npgsql can coexist with EF for deliberately separate paths; share deliberate data-source configuration. | SQL, COPY, batching, or low-level control is required. | Adding Npgsql only because EF already depends on it, then creating an unrelated pool. |
| EF Core PostgreSQL | [`Npgsql.EntityFrameworkCore.PostgreSQL`](../packages/npgsql.entityframeworkcore.postgresql.md) owns `UseNpgsql` and EF provider behavior. | Reference from EF infrastructure; its dependency supplies the driver unless direct driver APIs are used. | EF Core + provider + aligned extensions. | Persistence is expressed through EF Core. | Treating Npgsql alone as an EF provider or mismatching EF/provider majors. |

Primary source: [Npgsql EF Core provider](https://www.npgsql.org/efcore/).

## API authentication ownership

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Standard JWT bearer validation | [`Microsoft.AspNetCore.Authentication.JwtBearer`](../packages/microsoft-aspnetcore-authentication-jwtbearer.md) owns the ASP.NET Core handler and scheme validation. | Reference where the app calls `AddAuthentication().AddJwtBearer(...)`. | JWT bearer + FastEndpoints authorization, with authentication middleware before dependent endpoints. | The app owns issuer, audience, signature, lifetime, and challenge settings. | Incomplete validation or registering the same scheme twice. |
| FastEndpoints conveniences | [`FastEndpoints.Security`](../packages/fastendpoints-security.md) wraps ASP.NET Core auth and adds helpers. | Reference only when those helpers/token features are wanted; it does not replace the handler. | FastEndpoints.Security + FastEndpoints, with one scheme-registration owner. | The team chooses its documented convenience surface. | Calling its bearer helper and manual `AddJwtBearer` for the same scheme. |

Primary sources: [FastEndpoints security](https://fast-endpoints.com/docs/security), [ASP.NET Core JWT bearer](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).

## OpenTelemetry composition

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Instrumentation API | [`OpenTelemetry.Api`](../packages/opentelemetry.api.md) owns telemetry-facing APIs. | Libraries reference the API, not hosting or exporters. | API in libraries; application supplies SDK later. | Emitting telemetry without choosing collection/export policy. | A reusable library registering global SDK/export policy. |
| SDK/host lifecycle | [`OpenTelemetry`](../packages/opentelemetry.md) owns providers/processors; [`OpenTelemetry.Extensions.Hosting`](../packages/opentelemetry.extensions.hosting.md) owns DI lifecycle. | Applications reference what they configure. | API + SDK + hosting is the hosted-app core. | The process collects telemetry and owns shutdown/flush. | Building duplicate providers for one signal. |
| Signal sources | Selected instrumentation owns subscriptions: [ASP.NET Core](../packages/opentelemetry.instrumentation.aspnetcore.md), [HTTP](../packages/opentelemetry.instrumentation.http.md), [runtime](../packages/opentelemetry.instrumentation.runtime.md), or [Npgsql](../packages/npgsql.opentelemetry.md). | Reference only enabled sources. | Multiple instrumentations can feed one provider. | The signal and attribute/cardinality policy are defined. | Assuming instrumentation exports data itself. |
| OTLP export | [`OpenTelemetry.Exporter.OpenTelemetryProtocol`](../packages/opentelemetry.exporter.opentelemetryprotocol.md) owns OTLP transport. | Reference in processes exporting via OTLP. | One SDK can use deliberately configured exporters. | A collector/backend accepts OTLP. | Exporting secrets/high-cardinality values or treating export as business delivery. |

Primary source: [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/instrumentation/).

## Storage abstraction and provider SDKs

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Portable blob operations | [`FluentStorage`](../packages/fluentstorage.md) owns common abstractions; the selected provider owns backend construction. | Reference core where consumed and selected providers in infrastructure. | Multiple providers only when selection is explicit. | Required operations fit the common interface and portability matters. | Assuming identical consistency, metadata, auth, retry, or rename semantics. |
| Provider-native features | AWS/Azure/GCP/MinIO/SFTP SDKs own vendor-specific APIs; FluentStorage providers depend on them. | Add direct SDK references only when app code calls their APIs or policy anchors the version. | Native SDK code can coexist behind an explicit boundary. | Leases, signed URLs, IAM, multipart controls, or diagnostics exceed the abstraction. | Leaking provider types or creating two retry owners. |

Primary source: [FluentStorage upstream](https://github.com/robinrodricks/FluentStorage).

## Microsoft abstractions and runtime implementations

| Concern | Runtime/registration owner | Direct reference guidance | Valid combinations | Choose when | Common misuse |
| --- | --- | --- | --- | --- | --- |
| Reusable-library contract | The relevant `Microsoft.Extensions.*.Abstractions` package owns interfaces/minimal shared types. | Reference the narrow abstraction package when its types are used; do not add concrete hosting/DI only for interfaces. | Abstraction in a library + runtime chosen by its application. | The library remains composition-neutral. | Expecting abstractions to register services or provide a working host/container. |
| Executable runtime | Concrete packages such as [`Microsoft.Extensions.Hosting`](../packages/microsoft-extensions-hosting.md), [`Microsoft.Extensions.DependencyInjection`](../packages/microsoft-extensions-dependencyinjection.md), and [`Microsoft.Extensions.Diagnostics.HealthChecks`](../packages/microsoft-extensions-diagnostics-healthchecks.md) own implementations. | Reference from the executable/composition root. | Implementations naturally depend on matching abstractions; align Microsoft versions. | The process owns lifetime, DI, diagnostics, or health execution. | Referencing both for symmetry or assuming package presence performs registration. |

Primary sources: [.NET dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection), [generic host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host).
