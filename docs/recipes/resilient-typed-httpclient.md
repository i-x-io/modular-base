# A resilient typed HttpClient with one retry owner

## Problem and boundary

This recipe gives one typed `HttpClient` a bounded standard resilience pipeline while keeping retry ownership inside `Microsoft.Extensions.Http.Resilience`. `IHttpClientFactory` owns handler/client lifetime, the typed client owns the remote API contract, the standard handler owns rate limiting, total timeout, retry, circuit breaking, and per-attempt timeout, and the caller owns its cancellation and end-to-end deadline. No controller, application service, message consumer, or remote SDK may add another retry around this client.

The example disables retries for unsafe HTTP methods. A command may opt back into retry only after the remote API and local workflow provide an idempotency key or equivalent durable deduplication contract.

## Required packages

Use central package management for the Web host. The following Web SDK block is
a standalone application illustration outside this repository's enforced
project graph:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
  </ItemGroup>
</Project>
```

`Microsoft.Extensions.Http.Resilience` supplies the HTTP-specific Polly pipeline and brings the `IHttpClientFactory` integration used by the typed-client registration. Add a direct `Microsoft.Extensions.Http` reference only in a project that uses the factory without the resilience package; the .NET 10 SDK can report that direct reference as prunable here. Do not add a second direct Polly handler to this call path.

## Define the remote contract

Keep transport behavior and response ownership explicit:

```csharp
using System.Net;
using System.Net.Http.Json;

public sealed record InventoryItem(string Sku, int Available);

public sealed class InventoryClient(HttpClient httpClient)
{
    public async Task<InventoryItem?> FindAsync(
        string sku,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        using var response = await httpClient.GetAsync(
            $"inventory/{Uri.EscapeDataString(sku)}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryItem>(
            cancellationToken);
    }
}
```

The typed client maps one expected `404` outcome to `null` and lets final transport, timeout, circuit, rate-limit, and unexpected status failures remain exceptions for the application boundary. `ResponseHeadersRead` avoids buffering an unbounded body before control returns; response disposal still releases the connection. Escaping the path segment prevents it from changing URL structure, but authorization and input policy still belong at the caller boundary.

## Register one bounded resilience handler

Validate the configured remote address before registering the client. The
allowlist is a deliberate dependency boundary: replace these example names with
the inventory service names approved for the environment, rather than accepting
an arbitrary configuration value.

```csharp
using Microsoft.Extensions.Http.Resilience;
using Polly;

var approvedInventoryHosts = new HashSet<string>(
    StringComparer.OrdinalIgnoreCase)
{
    "inventory.example.com",
    "inventory.staging.example.com"
};

Uri ValidateInventoryBaseAddress(string configuredValue)
{
    if (!Uri.TryCreate(configuredValue, UriKind.Absolute, out var address) ||
        address.Scheme != Uri.UriSchemeHttps ||
        !string.IsNullOrEmpty(address.UserInfo) ||
        !approvedInventoryHosts.Contains(address.Host))
    {
        throw new InvalidOperationException(
            "Inventory:BaseAddress must be an HTTPS URL for an approved inventory host.");
    }

    return address;
}

HttpClientHandler CreateInventoryPrimaryHandler() => new()
{
    AllowAutoRedirect = false
};

var builder = WebApplication.CreateBuilder(args);

var inventoryBaseAddress = builder.Configuration["Inventory:BaseAddress"]
    ?? throw new InvalidOperationException(
        "Inventory:BaseAddress is required.");

builder.Services
    .AddHttpClient<InventoryClient>(client =>
    {
        client.BaseAddress = ValidateInventoryBaseAddress(inventoryBaseAddress);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("IX-CatalogApi/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(CreateInventoryPrimaryHandler)
    .AddStandardResilienceHandler(options =>
    {
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(8);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);

        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromMilliseconds(200);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;
        options.Retry.DisableForUnsafeHttpMethods();
    });
```

The validation rejects non-HTTPS addresses, embedded credentials, and hosts
outside the intentional allowlist before a request can be created. Disabling
automatic redirects prevents a response from silently crossing hosts or
downgrading transport; a `3xx` is returned to the typed client and fails through
`EnsureSuccessStatusCode()` unless the remote contract adds an explicit,
separately reviewed redirect policy.

The standard handler order is rate limiter, total timeout, retry, circuit breaker, and attempt timeout. Two retry attempts mean at most three network attempts for a handled safe-method outcome, subject to the caller and total budgets. The two-second attempt timeout fits inside the eight-second total budget, but production values must come from the dependency latency distribution and the caller's remaining SLA—not from this illustration.

By default the handler treats transport failures, per-attempt timeout rejection, HTTP `408`, `429`, and `5xx` outcomes as transient. `DisableForUnsafeHttpMethods()` removes retries for `POST`, `PUT`, `PATCH`, `DELETE`, and `CONNECT`; it does not make GET requests semantically safe if the remote API violates HTTP semantics. Keep authentication/token refresh, redirects, SDK retries, proxies, queues, and outer workflows in the retry-ownership inventory because they can still multiply attempts.

Do not use `HttpClient.Timeout` as another timeout owner here. Pass the request cancellation token so client disconnects and caller deadlines can stop the pipeline. Authentication handlers may be added when required, but they must not log credentials or replay a non-replayable body.

## Prove the maximum attempt count without a network

A deterministic primary handler can validate retry ownership and method policy in the consuming test project:

```csharp
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Hosting;

public sealed class ScriptedHandler(params HttpStatusCode[] statuses)
    : HttpMessageHandler
{
    private readonly ConcurrentQueue<HttpStatusCode> _statuses = new(statuses);

    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Calls++;
        var status = _statuses.TryDequeue(out var next)
            ? next
            : HttpStatusCode.OK;

        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = JsonContent.Create(new InventoryItem("SKU-1", 7)),
            RequestMessage = request
        });
    }
}

var handler = new ScriptedHandler(
    HttpStatusCode.ServiceUnavailable,
    HttpStatusCode.OK);

var approvedAddress = ValidateInventoryBaseAddress(
    "https://inventory.example.com/");

var insecureEndpointRejected = false;
try
{
    _ = ValidateInventoryBaseAddress("http://inventory.example.com/");
}
catch (InvalidOperationException)
{
    // Expected: only approved HTTPS endpoints are valid.
    insecureEndpointRejected = true;
}

if (!insecureEndpointRejected)
{
    throw new InvalidOperationException("An insecure endpoint was accepted.");
}

using var redirectPolicy = CreateInventoryPrimaryHandler();
if (redirectPolicy.AllowAutoRedirect)
{
    throw new InvalidOperationException("Automatic redirects were enabled.");
}

var builder = Host.CreateApplicationBuilder();
builder.Services
    .AddHttpClient<InventoryClient>(client =>
        client.BaseAddress = approvedAddress)
    .ConfigurePrimaryHttpMessageHandler(() => handler)
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.Zero;
        options.Retry.UseJitter = false;
        options.Retry.DisableForUnsafeHttpMethods();
    });

using var host = builder.Build();
var client = host.Services.GetRequiredService<InventoryClient>();
var item = await client.FindAsync("SKU-1", CancellationToken.None);

if (item?.Available != 7 || handler.Calls != 2)
{
    throw new InvalidOperationException("The retry contract changed.");
}
```

The endpoint guard test proves that the same validation used by the registration
rejects HTTP before the client is built; the redirect-policy assertion proves
that the same primary-handler factory disables automatic redirects. The fake is
the primary transport; the real resilience delegating handler still runs above
it. A `503` followed by `200` proves one retry and two total sends without
external timing or a server. Zero delay is test-only and should not leak into
production configuration. Add a separate redirect test that returns `3xx` with
a `Location` header and asserts the typed operation fails rather than following
it. Also add an unsafe-method client operation/test before claiming command
replay behavior is covered, and test caller cancellation and timeout rejection
with a controllable handler rather than `Thread.Sleep`.

## Failure modes and operations

| Symptom | Likely boundary | Observation and safe response |
| --- | --- | --- |
| More remote calls than the configured maximum | Duplicate retry owner | Correlate one logical operation and count caller, handler, SDK, proxy, redirect, and auth-refresh attempts. Remove the extra owner before tuning. |
| Duplicate command side effects | Unsafe replay/idempotency | Disable retries for that method immediately, then implement and test durable deduplication before any opt-in. |
| Requests exceed the caller SLA | Conflicting budgets | Compare caller cancellation, total timeout, attempt timeout, delays, and transport timing. Fit all attempts and backoff inside the remaining caller budget. |
| Circuit never opens or stays open | Traffic/threshold mismatch | Observe handled outcomes, sampling duration, minimum throughput, state transitions, and recovery probe behavior under representative load. |
| Healthy calls are rejected locally | Rate limiter/capacity | Inspect permits, queue length, connection-pool pressure, and caller concurrency. Align bounded concurrency with dependency capacity; do not add an unbounded queue. |
| `Retry-After` creates surprising delay | Server guidance and total budget | Record bounded retry-delay telemetry and confirm the header policy still fits the total/caller deadline. |

Observe logical-call duration, attempt count, final outcome, handled status category, timeout stage, circuit transitions/rejections, and rate-limit rejections by stable client/dependency name. Never tag telemetry with authorization headers, cookies, request/response bodies, full URLs or query strings, customer identifiers, idempotency keys, or exception messages. Alert on sustained final failures and protective-state changes rather than every individual retry.

## Verification checklist

Authoring evidence:

- [x] The Web SDK registration, typed client, and deterministic-handler sample compiled with the catalog's pinned package graph.
- [x] The deterministic sample ran without a network and observed exactly two sends for `503` followed by `200`.
- [x] The deterministic sample accepted an allowlisted HTTPS endpoint and rejected its HTTP counterpart before creating a client.
- [x] Circuit transitions, rate-limit behavior, real DNS/TLS/connectivity, and production time budgets were not integration-tested during authoring.

Consuming-application checks:

- [ ] Inventory every retry layer and prove that this handler is the sole owner on the complete call path.
- [ ] Test handled and unhandled status codes, transport failure, caller cancellation, attempt timeout, total timeout, and malformed/oversized responses.
- [ ] Assert maximum attempts for safe methods and zero resilience retries for each unsafe method unless a documented idempotency contract exists.
- [ ] Return a `3xx` with a `Location` header and assert the typed operation surfaces it instead of following the redirect.
- [ ] Assert non-HTTPS, credential-bearing, and non-allowlisted endpoint configuration fails during startup; test every approved environment host.
- [ ] Exercise circuit open/half-open/recovery and rate-limit rejection under representative concurrency and controlled faults.
- [ ] Fit attempt, delay, total, and caller budgets to the dependency SLO and verify retry amplification during an outage.
- [ ] Confirm logs, metrics, and traces contain no credentials, bodies, query strings, personal identifiers, or unbounded tags.

## Related guides

- [Microsoft.Extensions.Http](../packages/microsoft-extensions-http.md)
- [Microsoft.Extensions.Http.Resilience](../packages/microsoft-extensions-http-resilience.md)
- [Microsoft.Extensions.Resilience](../packages/microsoft-extensions-resilience.md)
- [Polly](../packages/polly.md)
- [Resilience and retry ownership](../package-guidance/package-selection.md#resilience-and-retry-ownership)

## Primary sources

Accessed 2026-07-27.

- [Microsoft Learn: build resilient HTTP apps](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
- [Microsoft Learn: IHttpClientFactory with .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)
- [Microsoft.Extensions.Http.Resilience source and README](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Http.Resilience)
- [Polly retry strategy](https://www.pollydocs.org/strategies/retry.html)
- [Polly circuit-breaker strategy](https://www.pollydocs.org/strategies/circuit-breaker.html)
- [Microsoft.Extensions.Http.Resilience 10.8.0 on NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience/10.8.0)
