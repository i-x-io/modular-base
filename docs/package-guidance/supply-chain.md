# Package supply-chain reference

This ledger covers all **89 external catalog entries** and the separately produced
`IX.Modularity.Analyzers` package. `Directory.Packages.props` is authoritative for
external pins. Facts were accessed on **2026-07-27**.

For external packages, “dependencies” is the union of direct NuGet dependencies
declared across target-framework groups; it is not a lock-file graph. “No advisory
attached” means the exact NuGet registration supplied no vulnerability record at
access time, not that the package or its transitive graph is vulnerability-free.
The repository keeps `NuGetAudit` enabled and that current restore-time result is the
release gate. “NuGet.org repository-signed” establishes repository integrity, not a
reproducible-build guarantee. Context7 was attempted first for every family but was
quota-exhausted; exact NuGet metadata and official upstream sources were used.

Shared primary sources: [NuGet registration metadata](https://learn.microsoft.com/en-us/nuget/api/registration-base-url-resource), [package auditing](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages), [signed-package verification](https://learn.microsoft.com/en-us/dotnet/core/tools/nuget-signed-package-verification), and [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

## Core utilities, validation, mail, and resilience

### anglesharp

| Field | Objective fact |
| --- | --- |
| Package / pin | [`AngleSharp 1.6.0`](https://www.nuget.org/packages/AngleSharp/1.6.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/anglesharp/1.6.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: AngleSharp. |
| Upstream | [official project/upstream](https://github.com/AngleSharp/AngleSharp). |
| Managed / external dependencies | Direct declared packages: `System.Text.Encoding.CodePages`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### devicedetector-net

| Field | Objective fact |
| --- | --- |
| Package / pin | [`DeviceDetector.NET 6.5.0`](https://www.nuget.org/packages/DeviceDetector.NET/6.5.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/devicedetector.net/6.5.0.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: totpero. |
| Upstream | [official project/upstream](https://github.com/totpero/DeviceDetector.NET). |
| Managed / external dependencies | Direct declared packages: `LiteDB`, `Microsoft.Extensions.Logging.Abstractions`, `System.Text.Json`, `YamlDotNet`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### enums-net

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Enums.NET 5.0.0`](https://www.nuget.org/packages/Enums.NET/5.0.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/enums.net/5.0.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Tyler Brinkley. |
| Upstream | [official project/upstream](https://github.com/TylerBrinkley/Enums.NET). |
| Managed / external dependencies | Direct declared packages: `System.Runtime.CompilerServices.Unsafe`, `System.ComponentModel.Annotations`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fluentresults

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FluentResults 4.0.0`](https://www.nuget.org/packages/FluentResults/4.0.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fluentresults/4.0.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Michael Altmann. |
| Upstream | [official project/upstream](https://github.com/altmann/FluentResults). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.Logging.Abstractions`, `System.Threading.Tasks.Extensions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fluentvalidation

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FluentValidation 12.1.1`](https://www.nuget.org/packages/FluentValidation/12.1.1); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fluentvalidation/12.1.1.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: Jeremy Skinner. |
| Upstream | [official project/upstream](https://github.com/JeremySkinner/fluentvalidation). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fluentvalidation-dependencyinjectionextensions

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FluentValidation.DependencyInjectionExtensions 12.1.1`](https://www.nuget.org/packages/FluentValidation.DependencyInjectionExtensions/12.1.1); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fluentvalidation.dependencyinjectionextensions/12.1.1.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: Jeremy Skinner. |
| Upstream | [official project/upstream](https://github.com/JeremySkinner/fluentvalidation). |
| Managed / external dependencies | Direct declared packages: `FluentValidation`, `Microsoft.Extensions.DependencyInjection.Abstractions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### humanizer-core

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Humanizer.Core 3.0.10`](https://www.nuget.org/packages/Humanizer.Core/3.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/humanizer.core/3.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Claire Novotny, Mehdi Khalili. |
| Upstream | [official project/upstream](https://github.com/Humanizr/Humanizer). |
| Managed / external dependencies | Direct declared packages: `System.Collections.Immutable`, `System.ComponentModel.Annotations`, `System.Memory`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with an author signature and NuGet.org repository signature; repository provenance is carried in package metadata. |

### mailkit

| Field | Objective fact |
| --- | --- |
| Package / pin | [`MailKit 4.17.0`](https://www.nuget.org/packages/MailKit/4.17.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/mailkit/4.17.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Jeffrey Stedfast. |
| Upstream | [official project/upstream](https://github.com/jstedfast/MailKit). |
| Managed / external dependencies | Direct declared packages: `System.Formats.Asn1`, `System.Threading.Tasks.Extensions`, `MimeKit`. SMTP, IMAP, and POP3 services selected by the application; depends on MimeKit. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### microsoft-extensions-http-resilience

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Http.Resilience 10.8.0`](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience/10.8.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.http.resilience/10.8.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/extensions). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.Http.Diagnostics`, `Microsoft.Extensions.Resilience`, `Microsoft.Extensions.ObjectPool`. Remote HTTP services reached by configured clients. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-resilience

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Resilience 10.8.0`](https://www.nuget.org/packages/Microsoft.Extensions.Resilience/10.8.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.resilience/10.8.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/extensions). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.Diagnostics.ExceptionSummarization`, `Microsoft.Extensions.Telemetry.Abstractions`, `Microsoft.Extensions.Diagnostics`, `Microsoft.Extensions.Options.ConfigurationExtensions`, `Polly.Extensions`, `Polly.RateLimiting`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### mimekit

| Field | Objective fact |
| --- | --- |
| Package / pin | [`MimeKit 4.17.0`](https://www.nuget.org/packages/MimeKit/4.17.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/mimekit/4.17.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Jeffrey Stedfast. |
| Upstream | [official project/upstream](https://github.com/jstedfast/MimeKit). |
| Managed / external dependencies | Direct declared packages: `System.Buffers`, `System.Memory`, `BouncyCastle.Cryptography`, `System.Security.Cryptography.Pkcs`, `System.Text.Encoding.CodePages`, `System.Data.DataSetExtensions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### polly

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Polly 8.7.0`](https://www.nuget.org/packages/Polly/8.7.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/polly/8.7.0.json). |
| License / maintainer | `BSD-3-Clause`; NuGet author attribution: Michael Wolfenden, App vNext. |
| Upstream | [official project/upstream](https://github.com/App-vNext/Polly). |
| Managed / external dependencies | Direct declared packages: `Polly.Core`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with an author signature and NuGet.org repository signature; repository provenance is carried in package metadata. |

### polly-extensions

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Polly.Extensions 8.7.0`](https://www.nuget.org/packages/Polly.Extensions/8.7.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/polly.extensions/8.7.0.json). |
| License / maintainer | `BSD-3-Clause`; NuGet author attribution: Michael Wolfenden, App vNext. |
| Upstream | [official project/upstream](https://github.com/App-vNext/Polly). |
| Managed / external dependencies | Direct declared packages: `Polly.Core`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`, `System.Diagnostics.DiagnosticSource`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with an author signature and NuGet.org repository signature; repository provenance is carried in package metadata. |

### scrutor

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Scrutor 7.0.0`](https://www.nuget.org/packages/Scrutor/7.0.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/scrutor/7.0.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Kristian Hellang. |
| Upstream | [official project/upstream](https://github.com/khellang/Scrutor). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.DependencyModel`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### yamldotnet

| Field | Objective fact |
| --- | --- |
| Package / pin | [`YamlDotNet 18.1.0`](https://www.nuget.org/packages/YamlDotNet/18.1.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/yamldotnet/18.1.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Antoine Aubry. |
| Upstream | [official project/upstream](https://github.com/aaubry/YamlDotNet). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

## Documentation, benchmarking, and compiler-tooling development

### benchmarkdotnet

| Field | Objective fact |
| --- | --- |
| Package / pin | [`BenchmarkDotNet 0.15.8`](https://www.nuget.org/packages/BenchmarkDotNet/0.15.8); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/benchmarkdotnet/0.15.8.json). |
| License / maintainer | `MIT`; NuGet author attribution: .NET Foundation and contributors. |
| Upstream | [official project/upstream](https://github.com/dotnet/BenchmarkDotNet). |
| Managed / external dependencies | Direct declared packages: `BenchmarkDotNet.Annotations`, `CommandLineParser`, `Gee.External.Capstone`, `Iced`, `Microsoft.CodeAnalysis.CSharp`, `Microsoft.Diagnostics.Runtime`, `Microsoft.Diagnostics.Tracing.TraceEvent`, `Microsoft.DotNet.PlatformAbstractions`, `Perfolizer`, `System.Management`, `Microsoft.Win32.Registry`, `System.Numerics.Vectors`, `System.Reflection.Emit`, `System.Reflection.Emit.Lightweight`, `System.Threading.Tasks.Extensions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | The direct graph includes native-capable `Gee.External.Capstone`; platform tool/runtime requirements apply. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### markdig

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Markdig 1.3.2`](https://www.nuget.org/packages/Markdig/1.3.2); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/markdig/1.3.2.json). |
| License / maintainer | `BSD-2-Clause`; NuGet author attribution: Alexandre Mutel. |
| Upstream | [official project/upstream](https://github.com/xoofx/markdig). |
| Managed / external dependencies | Direct declared packages: `System.Memory`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### microsoft-codeanalysis-analyzers

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.CodeAnalysis.Analyzers 5.6.0`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Analyzers/5.6.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.codeanalysis.analyzers/5.6.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/roslyn). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-codeanalysis-common

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.CodeAnalysis.Common 5.6.0`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Common/5.6.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.codeanalysis.common/5.6.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/roslyn). |
| Managed / external dependencies | Direct declared packages: `Microsoft.CodeAnalysis.Analyzers`, `System.Collections.Immutable`, `System.Reflection.Metadata`, `System.Memory`, `System.Runtime.CompilerServices.Unsafe`, `System.Text.Encoding.CodePages`, `System.Threading.Tasks.Extensions`, `System.Buffers`, `System.Numerics.Vectors`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-codeanalysis-csharp

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.CodeAnalysis.CSharp 5.6.0`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/5.6.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.codeanalysis.csharp/5.6.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/roslyn). |
| Managed / external dependencies | Direct declared packages: `Microsoft.CodeAnalysis.Common`, `Microsoft.CodeAnalysis.Analyzers`, `System.Collections.Immutable`, `System.Reflection.Metadata`, `System.Buffers`, `System.Memory`, `System.Numerics.Vectors`, `System.Runtime.CompilerServices.Unsafe`, `System.Text.Encoding.CodePages`, `System.Threading.Tasks.Extensions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

## Produced package

### ix-modularity-analyzers

| Field | Objective fact |
| --- | --- |
| Package / pin | `IX.Modularity.Analyzers` `0.1.0`; the version is declared by the repository project rather than `Directory.Packages.props`. |
| License / maintainer | `MIT`; declared author: ModularBase contributors. |
| Upstream / source | This repository: [`src/IX.Modularity.Analyzers/IX.Modularity.Analyzers.csproj`](../../src/IX.Modularity.Analyzers/IX.Modularity.Analyzers.csproj). Publication feed/status is not officially documented. |
| Build dependencies | Direct private build dependencies: `Microsoft.CodeAnalysis.Analyzers`, `Microsoft.CodeAnalysis.Common`, and `Microsoft.CodeAnalysis.CSharp`. The project is marked `DevelopmentDependency` and packages its analyzer under `analyzers/dotnet/cs`. |
| Runtime / service / native dependencies | Not officially documented. The project metadata does not establish a runtime, external-service, or native dependency claim. |
| Lifecycle / advisories | Formal support/EOL policy and package-specific public advisory feed are not officially documented. |
| Signing / provenance | Repository source and build inputs are controlled here; published-artifact location and author/repository signature status are not officially documented. |

## Microsoft.Extensions foundation

### microsoft-extensions-caching-stackexchangeredis

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Caching.StackExchangeRedis 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.caching.stackexchangeredis/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.Caching.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`, `StackExchange.Redis`. A Redis-compatible server; direct managed dependency `StackExchange.Redis`. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-configuration-abstractions

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Configuration.Abstractions 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Abstractions/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.configuration.abstractions/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.Primitives`, `System.ValueTuple`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-configuration-binder

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Configuration.Binder 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Binder/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.configuration.binder/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.Configuration`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-dependencyinjection

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.DependencyInjection 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.dependencyinjection/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `System.Threading.Tasks.Extensions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-dependencyinjection-abstractions

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.DependencyInjection.Abstractions 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.dependencyinjection.abstractions/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Bcl.AsyncInterfaces`, `System.Threading.Tasks.Extensions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-dependencymodel

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.DependencyModel 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyModel/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.dependencymodel/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `System.Text.Encodings.Web`, `System.Text.Json`, `System.Buffers`, `System.Memory`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-diagnostics-healthchecks

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Diagnostics.HealthChecks 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.HealthChecks/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.diagnostics.healthchecks/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions`, `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-diagnostics-healthchecks-abstractions

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.diagnostics.healthchecks.abstractions/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-hosting

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Hosting 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.Hosting/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.hosting/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.Configuration.Binder`, `Microsoft.Extensions.Configuration.CommandLine`, `Microsoft.Extensions.Configuration.EnvironmentVariables`, `Microsoft.Extensions.Configuration.FileExtensions`, `Microsoft.Extensions.Configuration.Json`, `Microsoft.Extensions.Configuration.UserSecrets`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Diagnostics`, `Microsoft.Extensions.FileProviders.Abstractions`, `Microsoft.Extensions.FileProviders.Physical`, `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Logging.Configuration`, `Microsoft.Extensions.Logging.Console`, `Microsoft.Extensions.Logging.Debug`, `Microsoft.Extensions.Logging.EventLog`, `Microsoft.Extensions.Logging.EventSource`, `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Options`, `System.Threading.Tasks.Extensions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-hosting-abstractions

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Hosting.Abstractions 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.Hosting.Abstractions/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.hosting.abstractions/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Diagnostics.Abstractions`, `Microsoft.Extensions.FileProviders.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `System.Threading.Tasks.Extensions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-http

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Http 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.Http/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.http/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Diagnostics`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-logging-abstractions

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Logging.Abstractions 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.logging.abstractions/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.DependencyInjection.Abstractions`, `System.Diagnostics.DiagnosticSource`, `System.Buffers`, `System.Memory`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-options

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Options 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.Options/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.options/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Primitives`, `System.ValueTuple`, `System.ComponentModel.Annotations`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-options-configurationextensions

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.Options.ConfigurationExtensions 10.0.10`](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.options.configurationextensions/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.Configuration.Binder`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Primitives`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-extensions-timeprovider-testing

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.Extensions.TimeProvider.Testing 10.8.0`](https://www.nuget.org/packages/Microsoft.Extensions.TimeProvider.Testing/10.8.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.extensions.timeprovider.testing/10.8.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/extensions). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Bcl.TimeProvider`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

## ASP.NET Core, FastEndpoints, OpenAPI, and API infrastructure

### asp-versioning-http

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Asp.Versioning.Http 10.0.0`](https://www.nuget.org/packages/Asp.Versioning.Http/10.0.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/asp.versioning.http/10.0.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: .NET Foundation and Contributors. |
| Upstream | [official project/upstream](https://github.com/dotnet/aspnet-api-versioning). |
| Managed / external dependencies | Direct declared packages: `Asp.Versioning.Abstractions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with an author signature and NuGet.org repository signature; repository provenance is carried in package metadata. |

### fastendpoints

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FastEndpoints 8.2.0`](https://www.nuget.org/packages/FastEndpoints/8.2.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fastendpoints/8.2.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: FastEndpoints. |
| Upstream | [official project/upstream](https://github.com/FastEndpoints/FastEndpoints.git). |
| Managed / external dependencies | Direct declared packages: `FastEndpoints.Attributes`, `FastEndpoints.JobQueues`, `FastEndpoints.Messaging`, `FluentValidation`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fastendpoints-generator

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FastEndpoints.Generator 8.2.0`](https://www.nuget.org/packages/FastEndpoints.Generator/8.2.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fastendpoints.generator/8.2.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: FastEndpoints. |
| Upstream | [official project/upstream](https://github.com/FastEndpoints/FastEndpoints.git). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fastendpoints-openapi

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FastEndpoints.OpenApi 8.2.0`](https://www.nuget.org/packages/FastEndpoints.OpenApi/8.2.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fastendpoints.openapi/8.2.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: FastEndpoints. |
| Upstream | [official project/upstream](https://github.com/FastEndpoints/FastEndpoints.git). |
| Managed / external dependencies | Direct declared packages: `FastEndpoints`, `FluentValidation`, `Microsoft.AspNetCore.OpenApi`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fastendpoints-security

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FastEndpoints.Security 8.2.0`](https://www.nuget.org/packages/FastEndpoints.Security/8.2.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fastendpoints.security/8.2.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: FastEndpoints. |
| Upstream | [official project/upstream](https://github.com/FastEndpoints/FastEndpoints.git). |
| Managed / external dependencies | Direct declared packages: `FastEndpoints`, `Microsoft.AspNetCore.Authentication.JwtBearer`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fastendpoints-testing

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FastEndpoints.Testing 8.2.0`](https://www.nuget.org/packages/FastEndpoints.Testing/8.2.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fastendpoints.testing/8.2.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: FastEndpoints. |
| Upstream | [official project/upstream](https://github.com/FastEndpoints/FastEndpoints.git). |
| Managed / external dependencies | Direct declared packages: `Bogus`, `Microsoft.AspNetCore.Mvc.Testing`, `xunit.v3.extensibility.core`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### microsoft-aspnetcore-authentication-jwtbearer

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.AspNetCore.Authentication.JwtBearer 10.0.10`](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.authentication.jwtbearer/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.IdentityModel.Protocols.OpenIdConnect`. Configured OpenID Connect/JWT issuer metadata and signing-key endpoints when discovery is enabled. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-aspnetcore-mvc-testing

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.AspNetCore.Mvc.Testing 10.0.10`](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.mvc.testing/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.AspNetCore.TestHost`, `Microsoft.Extensions.DependencyModel`, `Microsoft.Extensions.Hosting`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-aspnetcore-openapi

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.AspNetCore.OpenApi 10.0.10`](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.aspnetcore.openapi/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.OpenApi`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-openapi

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.OpenApi 2.11.0`](https://www.nuget.org/packages/Microsoft.OpenApi/2.11.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.openapi/2.11.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/Microsoft/OpenAPI.NET). |
| Managed / external dependencies | Direct declared packages: `System.Text.Json`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### scalar-aspnetcore

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Scalar.AspNetCore 2.16.16`](https://www.nuget.org/packages/Scalar.AspNetCore/2.16.16); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/scalar.aspnetcore/2.16.16.json). |
| License / maintainer | `MIT`; NuGet author attribution: Scalar. |
| Upstream | [official project/upstream](https://github.com/scalar/scalar). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

## EF Core, PostgreSQL, specifications, search, and pagination

### ardalis-specification

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Ardalis.Specification 9.3.1`](https://www.nuget.org/packages/Ardalis.Specification/9.3.1); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/ardalis.specification/9.3.1.json). |
| License / maintainer | `MIT`; NuGet author attribution: Steve Smith (@ardalis),Fati Iseni (@fiseni),Scott DePouw. |
| Upstream | [official project/upstream](https://github.com/ardalis/Specification). |
| Managed / external dependencies | Direct declared packages: `System.Buffers`, `System.Memory`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### ardalis-specification-entityframeworkcore

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Ardalis.Specification.EntityFrameworkCore 9.3.1`](https://www.nuget.org/packages/Ardalis.Specification.EntityFrameworkCore/9.3.1); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/ardalis.specification.entityframeworkcore/9.3.1.json). |
| License / maintainer | `MIT`; NuGet author attribution: Steve Smith (@ardalis),Fati Iseni (@fiseni),Scott DePouw. |
| Upstream | [official project/upstream](https://github.com/ardalis/Specification). |
| Managed / external dependencies | Direct declared packages: `Ardalis.Specification`, `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### efcore-namingconventions

| Field | Objective fact |
| --- | --- |
| Package / pin | [`EFCore.NamingConventions 10.0.1`](https://www.nuget.org/packages/EFCore.NamingConventions/10.0.1); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/efcore.namingconventions/10.0.1.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: Shay Rojansky. |
| Upstream | [official project/upstream](https://github.com/efcore/EFCore.NamingConventions). |
| Managed / external dependencies | Direct declared packages: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`, `Microsoft.Extensions.DependencyInjection.Abstractions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### entityframeworkcore-exceptions-postgresql

| Field | Objective fact |
| --- | --- |
| Package / pin | [`EntityFrameworkCore.Exceptions.PostgreSQL 10.0.1`](https://www.nuget.org/packages/EntityFrameworkCore.Exceptions.PostgreSQL/10.0.1); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/entityframeworkcore.exceptions.postgresql/10.0.1.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: Giorgi Dalakishvili. |
| Upstream | [official project/upstream](https://github.com/Giorgi/EntityFramework.Exceptions). |
| Managed / external dependencies | Direct declared packages: `DbExceptionClassifier.PostgreSQL`, `EntityFrameworkCore.Exceptions.Common`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### microsoft-entityframeworkcore

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.EntityFrameworkCore 10.0.10`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.entityframeworkcore/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.EntityFrameworkCore.Abstractions`, `Microsoft.EntityFrameworkCore.Analyzers`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.Logging`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-entityframeworkcore-design

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.EntityFrameworkCore.Design 10.0.10`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.entityframeworkcore.design/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.EntityFrameworkCore.Relational`, `Humanizer.Core`, `Microsoft.CodeAnalysis.CSharp.Workspaces`, `Microsoft.CodeAnalysis.Workspaces.MSBuild`, `Microsoft.Extensions.DependencyModel`, `Mono.TextTemplating`, `Microsoft.Build.Framework`, `Microsoft.CodeAnalysis.CSharp`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.Logging`, `Newtonsoft.Json`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-entityframeworkcore-inmemory

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.EntityFrameworkCore.InMemory 10.0.10`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.InMemory/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.entityframeworkcore.inmemory/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.Logging`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-entityframeworkcore-relational

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.EntityFrameworkCore.Relational 10.0.10`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Relational/10.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.entityframeworkcore.relational/10.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/dotnet). |
| Managed / external dependencies | Direct declared packages: `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.Logging`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable on the .NET 10 line; .NET 10 is Active LTS through 2028-11-14. A separate package-specific support window is not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### mr-entityframeworkcore-keysetpagination

| Field | Objective fact |
| --- | --- |
| Package / pin | [`MR.EntityFrameworkCore.KeysetPagination 1.6.0`](https://www.nuget.org/packages/MR.EntityFrameworkCore.KeysetPagination/1.6.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/mr.entityframeworkcore.keysetpagination/1.6.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Mohammad Rahhal. |
| Upstream | [official project/upstream](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination). |
| Managed / external dependencies | Direct declared packages: `MR.EntityFrameworkCore.KeysetPagination.Analyzers`, `Microsoft.EntityFrameworkCore`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### npgsql

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Npgsql 10.0.3`](https://www.nuget.org/packages/Npgsql/10.0.3); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/npgsql/10.0.3.json). |
| License / maintainer | `PostgreSQL`; NuGet author attribution: Shay Rojansky,Nikita Kazmin,Brar Piening,Nino Floris,Yoh Deadfall,Austin Drenski,Emil Lenngren,Francisco Figueiredo Jr.,Kenji Uno. |
| Upstream | [official project/upstream](https://github.com/npgsql/npgsql). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.Logging.Abstractions`, `System.Diagnostics.DiagnosticSource`. A PostgreSQL server. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### npgsql-entityframeworkcore-postgresql

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3`](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/npgsql.entityframeworkcore.postgresql/10.0.3.json). |
| License / maintainer | `PostgreSQL`; NuGet author attribution: Shay Rojansky,Austin Drenski,Yoh Deadfall. |
| Upstream | [official project/upstream](https://github.com/npgsql/efcore.pg). |
| Managed / external dependencies | Direct declared packages: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`, `Npgsql`. A PostgreSQL server through Npgsql. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### npgsql-opentelemetry

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Npgsql.OpenTelemetry 10.0.3`](https://www.nuget.org/packages/Npgsql.OpenTelemetry/10.0.3); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/npgsql.opentelemetry/10.0.3.json). |
| License / maintainer | `PostgreSQL`; NuGet author attribution: Shay Rojansky. |
| Upstream | [official project/upstream](https://github.com/npgsql/npgsql). |
| Managed / external dependencies | Direct declared packages: `Npgsql`, `OpenTelemetry.API`. Npgsql activity sources; an OpenTelemetry SDK/export path is application-owned. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### pgvector

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Pgvector 0.3.2`](https://www.nuget.org/packages/Pgvector/0.3.2); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/pgvector/0.3.2.json). |
| License / maintainer | `MIT`; NuGet author attribution: ankane. |
| Upstream | [official project/upstream](https://github.com/pgvector/pgvector-dotnet). |
| Managed / external dependencies | Direct declared packages: `Npgsql`. PostgreSQL with the `vector` extension; Npgsql client dependency. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### pgvector-entityframeworkcore

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Pgvector.EntityFrameworkCore 0.3.0`](https://www.nuget.org/packages/Pgvector.EntityFrameworkCore/0.3.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/pgvector.entityframeworkcore/0.3.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: ankane. |
| Upstream | [official project/upstream](https://github.com/pgvector/pgvector-dotnet). |
| Managed / external dependencies | Direct declared packages: `Pgvector`, `Npgsql.EntityFrameworkCore.PostgreSQL`. PostgreSQL with `vector`; Pgvector and Npgsql EF provider dependencies. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

## FluentStorage core and approved enterprise providers

### fluentstorage

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FluentStorage 8.0.16`](https://www.nuget.org/packages/FluentStorage/8.0.16); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fluentstorage/8.0.16.json). |
| License / maintainer | `MIT`; NuGet author attribution: Robin Rodricks, FluentStorage Contributors. |
| Upstream | [official project/upstream](https://github.com/robinrodricks/FluentStorage). |
| Managed / external dependencies | Direct declared packages: `Microsoft.IO.RecyclableMemoryStream`, `TestableIO.System.IO.Abstractions.Wrappers`, `System.Text.Json`, `System.Threading.Thread`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fluentstorage-aws

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FluentStorage.AWS 8.0.10`](https://www.nuget.org/packages/FluentStorage.AWS/8.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fluentstorage.aws/8.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Robin Rodricks, FluentStorage Contributors. |
| Upstream | [official project/upstream](https://github.com/robinrodricks/FluentStorage). |
| Managed / external dependencies | Direct declared packages: `FluentStorage`, `AWSSDK.Core`, `AWSSDK.S3`, `AWSSDK.SQS`, `AWSSDK.SecurityToken`, `MimeMapping`. AWS S3/SQS/STS endpoints through AWS SDK dependencies. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fluentstorage-azure-blobs

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FluentStorage.Azure.Blobs 8.0.10`](https://www.nuget.org/packages/FluentStorage.Azure.Blobs/8.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fluentstorage.azure.blobs/8.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Robin Rodricks, FluentStorage Contributors. |
| Upstream | [official project/upstream](https://github.com/robinrodricks/FluentStorage). |
| Managed / external dependencies | Direct declared packages: `FluentStorage.Azure`, `FluentStorage`, `Azure.Storage.Blobs`, `MimeMapping`, `System.Text.Json`. Azure Blob Storage through Azure SDK dependencies. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fluentstorage-azure-files

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FluentStorage.Azure.Files 8.0.10`](https://www.nuget.org/packages/FluentStorage.Azure.Files/8.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fluentstorage.azure.files/8.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Robin Rodricks, FluentStorage Contributors. |
| Upstream | [official project/upstream](https://github.com/robinrodricks/FluentStorage). |
| Managed / external dependencies | Direct declared packages: `FluentStorage.Azure`, `FluentStorage`, `Azure.Storage.Files.Shares`. Azure Files through Azure SDK dependencies. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fluentstorage-gcp

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FluentStorage.GCP 8.0.14`](https://www.nuget.org/packages/FluentStorage.GCP/8.0.14); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fluentstorage.gcp/8.0.14.json). |
| License / maintainer | `MIT`; NuGet author attribution: Robin Rodricks, FluentStorage Contributors. |
| Upstream | [official project/upstream](https://github.com/robinrodricks/FluentStorage). |
| Managed / external dependencies | Direct declared packages: `FluentStorage`, `Google.Cloud.Storage.V1`. Google Cloud Storage through `Google.Cloud.Storage.V1`. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fluentstorage-minio

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FluentStorage.Minio 8.0.10`](https://www.nuget.org/packages/FluentStorage.Minio/8.0.10); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fluentstorage.minio/8.0.10.json). |
| License / maintainer | `MIT`; NuGet author attribution: Robin Rodricks, FluentStorage Contributors. |
| Upstream | [official project/upstream](https://github.com/robinrodricks/FluentStorage). |
| Managed / external dependencies | Direct declared packages: `FluentStorage`, `MimeMapping`, `Minio`. A MinIO/S3-compatible service through the MinIO SDK. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### fluentstorage-sftp

| Field | Objective fact |
| --- | --- |
| Package / pin | [`FluentStorage.SFTP 8.0.16`](https://www.nuget.org/packages/FluentStorage.SFTP/8.0.16); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/fluentstorage.sftp/8.0.16.json). |
| License / maintainer | `MIT`; NuGet author attribution: Robin Rodricks, FluentStorage Contributors. |
| Upstream | [official project/upstream](https://github.com/robinrodricks/FluentStorage). |
| Managed / external dependencies | Direct declared packages: `FluentStorage`, `Polly`, `SSH.NET`. An SSH/SFTP server through SSH.NET. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

## Observability

### opentelemetry

| Field | Objective fact |
| --- | --- |
| Package / pin | [`OpenTelemetry 1.17.0`](https://www.nuget.org/packages/OpenTelemetry/1.17.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/opentelemetry/1.17.0.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: OpenTelemetry Authors. |
| Upstream | [official project/upstream](https://github.com/open-telemetry/opentelemetry-dotnet). |
| Managed / external dependencies | Direct declared packages: `OpenTelemetry.Api.ProviderBuilderExtensions`, `Microsoft.Extensions.Configuration.EnvironmentVariables`, `Microsoft.Extensions.Diagnostics.Abstractions`, `Microsoft.Extensions.Logging.Configuration`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### opentelemetry-api

| Field | Objective fact |
| --- | --- |
| Package / pin | [`OpenTelemetry.Api 1.17.0`](https://www.nuget.org/packages/OpenTelemetry.Api/1.17.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/opentelemetry.api/1.17.0.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: OpenTelemetry Authors. |
| Upstream | [official project/upstream](https://github.com/open-telemetry/opentelemetry-dotnet). |
| Managed / external dependencies | Direct declared packages: `System.Diagnostics.DiagnosticSource`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### opentelemetry-exporter-opentelemetryprotocol

| Field | Objective fact |
| --- | --- |
| Package / pin | [`OpenTelemetry.Exporter.OpenTelemetryProtocol 1.17.0`](https://www.nuget.org/packages/OpenTelemetry.Exporter.OpenTelemetryProtocol/1.17.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/opentelemetry.exporter.opentelemetryprotocol/1.17.0.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: OpenTelemetry Authors. |
| Upstream | [official project/upstream](https://github.com/open-telemetry/opentelemetry-dotnet). |
| Managed / external dependencies | Direct declared packages: `OpenTelemetry`, `Microsoft.Extensions.Configuration.Binder`. An OTLP collector/backend over configured HTTP or gRPC transport. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### opentelemetry-extensions-hosting

| Field | Objective fact |
| --- | --- |
| Package / pin | [`OpenTelemetry.Extensions.Hosting 1.17.0`](https://www.nuget.org/packages/OpenTelemetry.Extensions.Hosting/1.17.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/opentelemetry.extensions.hosting/1.17.0.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: OpenTelemetry Authors. |
| Upstream | [official project/upstream](https://github.com/open-telemetry/opentelemetry-dotnet). |
| Managed / external dependencies | Direct declared packages: `OpenTelemetry`, `Microsoft.Extensions.Hosting.Abstractions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### opentelemetry-instrumentation-aspnetcore

| Field | Objective fact |
| --- | --- |
| Package / pin | [`OpenTelemetry.Instrumentation.AspNetCore 1.17.0`](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNetCore/1.17.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/opentelemetry.instrumentation.aspnetcore/1.17.0.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: OpenTelemetry Authors. |
| Upstream | [official project/upstream](https://github.com/open-telemetry/opentelemetry-dotnet-contrib). |
| Managed / external dependencies | Direct declared packages: `OpenTelemetry.Api.ProviderBuilderExtensions`, `Microsoft.AspNetCore.Http.Abstractions`, `Microsoft.AspNetCore.Http.Features`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.Options`, `System.Text.Encodings.Web`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### opentelemetry-instrumentation-http

| Field | Objective fact |
| --- | --- |
| Package / pin | [`OpenTelemetry.Instrumentation.Http 1.17.0`](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Http/1.17.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/opentelemetry.instrumentation.http/1.17.0.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: OpenTelemetry Authors. |
| Upstream | [official project/upstream](https://github.com/open-telemetry/opentelemetry-dotnet-contrib). |
| Managed / external dependencies | Direct declared packages: `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.Options`, `OpenTelemetry.Api.ProviderBuilderExtensions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### opentelemetry-instrumentation-runtime

| Field | Objective fact |
| --- | --- |
| Package / pin | [`OpenTelemetry.Instrumentation.Runtime 1.17.0`](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Runtime/1.17.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/opentelemetry.instrumentation.runtime/1.17.0.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: OpenTelemetry Authors. |
| Upstream | [official project/upstream](https://github.com/open-telemetry/opentelemetry-dotnet-contrib). |
| Managed / external dependencies | Direct declared packages: `OpenTelemetry.Api`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

## Testing. SpecsFor is intentionally catalog-only and is the sole prerelease dependency

### awesomeassertions

| Field | Objective fact |
| --- | --- |
| Package / pin | [`AwesomeAssertions 9.5.0`](https://www.nuget.org/packages/AwesomeAssertions/9.5.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/awesomeassertions/9.5.0.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: AwesomeAssertions, Dennis Doomen, Jonas Nyrup and contributors. |
| Upstream | [official project/upstream](https://github.com/AwesomeAssertions/AwesomeAssertions). |
| Managed / external dependencies | Direct declared packages: `System.Threading.Tasks.Extensions`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### coverlet-collector

| Field | Objective fact |
| --- | --- |
| Package / pin | [`coverlet.collector 10.0.1`](https://www.nuget.org/packages/coverlet.collector/10.0.1); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/coverlet.collector/10.0.1.json). |
| License / maintainer | `MIT`; NuGet author attribution: tonerdo. |
| Upstream | [official project/upstream](https://github.com/coverlet-coverage/coverlet.git). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### microsoft-net-test-sdk

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.NET.Test.Sdk 18.8.1`](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.8.1); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.net.test.sdk/18.8.1.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/microsoft/vstest). |
| Managed / external dependencies | Direct declared packages: `Microsoft.TestPlatform.TestHost`, `Microsoft.CodeCoverage`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### specsfor

| Field | Objective fact |
| --- | --- |
| Package / pin | [`SpecsFor 8.0.0-rc2a`](https://www.nuget.org/packages/SpecsFor/8.0.0-rc2a); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/specsfor/8.0.0-rc2a.json). |
| License / maintainer | `MIT`; NuGet author attribution: Matt Honeycutt. |
| Upstream | [official project/upstream](https://github.com/MattHoneycutt/SpecsFor). |
| Managed / external dependencies | Direct declared packages: `SpecsFor.StructureMap`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed **prerelease** (`8.0.0-rc2a`); formal support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### testcontainers-postgresql

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Testcontainers.PostgreSql 4.13.0`](https://www.nuget.org/packages/Testcontainers.PostgreSql/4.13.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/testcontainers.postgresql/4.13.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Andre Hofmeister and contributors. |
| Upstream | [official project/upstream](https://github.com/testcontainers/testcontainers-dotnet). |
| Managed / external dependencies | Direct declared packages: `Testcontainers`. A Docker-API-compatible container runtime, PostgreSQL image, and started PostgreSQL container. |
| Native dependencies | No native library is declared by the module; the external container runtime has platform-native requirements. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### testcontainers-redis

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Testcontainers.Redis 4.13.0`](https://www.nuget.org/packages/Testcontainers.Redis/4.13.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/testcontainers.redis/4.13.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Andre Hofmeister and contributors. |
| Upstream | [official project/upstream](https://github.com/testcontainers/testcontainers-dotnet). |
| Managed / external dependencies | Direct declared packages: `Testcontainers`. A Docker-API-compatible container runtime, Redis image, and started Redis container. |
| Native dependencies | No native library is declared by the module; the external container runtime has platform-native requirements. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### tngtech-archunitnet-xunitv3

| Field | Objective fact |
| --- | --- |
| Package / pin | [`TngTech.ArchUnitNET.xUnitV3 0.13.3`](https://www.nuget.org/packages/TngTech.ArchUnitNET.xUnitV3/0.13.3); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/tngtech.archunitnet.xunitv3/0.13.3.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: ArchUnitNET.xUnitV3. |
| Upstream | [official project/upstream](https://github.com/TNG/ArchUnitNET). |
| Managed / external dependencies | Direct declared packages: `TngTech.ArchUnitNET`, `System.Net.Http`, `System.Text.RegularExpressions`, `xunit.v3.assert`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### xunit-runner-visualstudio

| Field | Objective fact |
| --- | --- |
| Package / pin | [`xunit.runner.visualstudio 3.1.5`](https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.5); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/xunit.runner.visualstudio/3.1.5.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: jnewkirk,bradwilson. |
| Upstream | [official project/upstream](https://github.com/xunit/visualstudio.xunit). |
| Managed / external dependencies | Direct declared packages: `Microsoft.TestPlatform.ObjectModel`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### xunit-v3

| Field | Objective fact |
| --- | --- |
| Package / pin | [`xunit.v3 3.2.2`](https://www.nuget.org/packages/xunit.v3/3.2.2); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/xunit.v3/3.2.2.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: jnewkirk,bradwilson. |
| Upstream | [official project/upstream](https://github.com/xunit/xunit). |
| Managed / external dependencies | Direct declared packages: `xunit.v3.mtp-v1`. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

## Project-scoped analyzer: packable projects opt in and own their PublicAPI files

### microsoft-codeanalysis-publicapianalyzers

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.CodeAnalysis.PublicApiAnalyzers 5.6.0`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.PublicApiAnalyzers/5.6.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.codeanalysis.publicapianalyzers/5.6.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/roslyn). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

## Universal analyzers. These are build-only and never flow into package consumers

### meziantou-analyzer

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Meziantou.Analyzer 3.0.132`](https://www.nuget.org/packages/Meziantou.Analyzer/3.0.132); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/meziantou.analyzer/3.0.132.json). |
| License / maintainer | `MIT`; NuGet author attribution: meziantou. |
| Upstream | [official project/upstream](https://github.com/meziantou/Meziantou.Analyzer). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### microsoft-codeanalysis-bannedapianalyzers

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.CodeAnalysis.BannedApiAnalyzers 5.6.0`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers/5.6.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.codeanalysis.bannedapianalyzers/5.6.0.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/dotnet/roslyn). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | Exact artifact verified with Microsoft author signature and NuGet.org repository signature; repository/commit provenance is carried in package metadata. |

### microsoft-visualstudio-threading-analyzers

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Microsoft.VisualStudio.Threading.Analyzers 18.7.23`](https://www.nuget.org/packages/Microsoft.VisualStudio.Threading.Analyzers/18.7.23); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/microsoft.visualstudio.threading.analyzers/18.7.23.json). |
| License / maintainer | `MIT`; NuGet author attribution: Microsoft. |
| Upstream | [official project/upstream](https://github.com/microsoft/vs-threading). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### roslynator-analyzers

| Field | Objective fact |
| --- | --- |
| Package / pin | [`Roslynator.Analyzers 4.15.0`](https://www.nuget.org/packages/Roslynator.Analyzers/4.15.0); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/roslynator.analyzers/4.15.0.json). |
| License / maintainer | `Apache-2.0`; NuGet author attribution: Josef Pihrt. |
| Upstream | [official project/upstream](https://github.com/dotnet/roslynator). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |

### sonaranalyzer-csharp

| Field | Objective fact |
| --- | --- |
| Package / pin | [`SonarAnalyzer.CSharp 10.30.0.144632`](https://www.nuget.org/packages/SonarAnalyzer.CSharp/10.30.0.144632); approved source: `nuget.org`; [exact registration](https://api.nuget.org/v3/registration5-gz-semver2/sonaranalyzer.csharp/10.30.0.144632.json). |
| License / maintainer | `LGPL-3.0-only`; NuGet author attribution: SonarSource. |
| Upstream | [official project/upstream](https://github.com/SonarSource/sonar-dotnet). |
| Managed / external dependencies | No direct NuGet dependency declared for the applicable package groups. No external service is required by the package itself; host/transitive runtime requirements are not officially documented. |
| Native dependencies | Direct native dependency: not officially documented; no native requirement is asserted from absence of metadata. |
| Lifecycle / advisories | NuGet-listed stable; formal package-version support/EOL policy not officially documented. No advisory was attached to the exact NuGet registration at access; continue restore-time audit. |
| Signing / provenance | NuGet.org repository-signed; author-signature status not officially documented in the sources reviewed. Repository provenance is carried in package metadata when supplied. |
