# FastEndpoints request validation with FluentResults outcomes

## Problem and boundary

This recipe separates malformed input from expected application outcomes. FastEndpoints runs a FluentValidation validator before the handler and returns the configured validation response for invalid request shape. The application service returns `FluentResults.Result<T>` for expected business outcomes. The endpoint translates typed application errors into HTTP responses once, while cancellation, programming defects, and unexpected infrastructure failures remain exceptions for the host's exception boundary and telemetry.

## Required packages and project boundary

The application service and its FluentResults contract belong in the
repository-oriented
`src/IX.Modularity.Catalog/IX.Modularity.Catalog.csproj` library project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IXModularityProjectRole>Library</IXModularityProjectRole>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentResults" />
  </ItemGroup>
</Project>
```

The following Web SDK block is a standalone application illustration outside
this repository's enforced project graph:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FastEndpoints" />
    <PackageReference Include="FluentValidation" />
    <ProjectReference Include="../IX.Modularity.Catalog/IX.Modularity.Catalog.csproj" />
  </ItemGroup>
</Project>
```

Both blocks use versions from `Directory.Packages.props`. The library's direct
`Library` role permits its public FluentResults service contract. The standalone
Web host consumes the public result types transitively through its
`ProjectReference` to `IX.Modularity.Catalog`; it does not add a direct
FluentResults package dependency. FastEndpoints already integrates
FluentValidation. The explicit `FluentValidation` reference makes the endpoint
project's validator dependency intentional; no
`FluentValidation.DependencyInjectionExtensions` reference or validator
scanning registration is needed for validators derived from
`FastEndpoints.Validator<T>`.

## Request validation

In the standalone Web host, define an immutable transport request and a
stateless validator:

```csharp
using FastEndpoints;
using FluentValidation;

public sealed record CreateProductRequest(string Sku, string Name);

public sealed class CreateProductValidator : Validator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(request => request.Sku)
            .NotEmpty()
            .MaximumLength(32)
            .Matches("^[A-Z0-9-]+$")
            .WithErrorCode("invalid_sku");

        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithErrorCode("invalid_name");
    }
}
```

FastEndpoints discovers the validator and runs it after binding but before `HandleAsync`. Invalid requests therefore never reach application logic. Rules protect the transport/application contract; a database constraint must still enforce authoritative length or uniqueness. Keep validators stateless, do not perform race-prone uniqueness checks here, and ensure messages and error codes disclose no internal data.

## Application outcome model

In `IX.Modularity.Catalog`, represent expected failures as concrete error types
with stable codes:

```csharp
using FluentResults;

public abstract class CatalogError(string message) : Error(message);

public sealed class DuplicateSkuError()
    : CatalogError("A product with this SKU already exists.")
{
    public const string Code = "sku_already_exists";
}

public sealed record ProductCreated(Guid Id, string Sku);
public sealed record CreateProductCommand(string Sku, string Name);

public interface IProductService
{
    Task<Result<ProductCreated>> CreateAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken);
}
```

The concrete error type is the application's machine-readable contract; callers do not parse `Error.Message` or package metadata. The service should return this failure only for an expected, caller-actionable conflict. It should propagate `OperationCanceledException` and unexpected storage/network exceptions so central handling records a failure and returns a generic server response.

In the same library project, this demonstration implementation shows the result
flow without pretending to provide durable uniqueness:

```csharp
using FluentResults;

public sealed class DemoProductService : IProductService
{
    public Task<Result<ProductCreated>> CreateAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.Sku == "EXISTS")
        {
            return Task.FromResult(
                Result.Fail<ProductCreated>(new DuplicateSkuError()));
        }

        return Task.FromResult(
            Result.Ok(new ProductCreated(Guid.NewGuid(), command.Sku)));
    }
}
```

`EXISTS` is a deterministic demonstration branch, not production uniqueness logic. A production implementation should rely on an authoritative database constraint, translate only the known conflict to `DuplicateSkuError`, and allow unknown persistence failures to escape. The cancellation token is checked and must be passed to every real asynchronous dependency.

## HTTP translation and host composition

Back in the standalone Web host, branch once on the result at the transport
boundary:

```csharp
using FastEndpoints;

public sealed record ApiError(string Code, string Message);

public sealed class CreateProductEndpoint(IProductService service)
    : Endpoint<CreateProductRequest, object>
{
    public override void Configure()
    {
        Post("/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(request.Sku, request.Name);
        var result = await service.CreateAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            await Send.CreatedAtAsync<GetProductEndpoint>(
                new { id = result.Value.Id },
                result.Value,
                cancellation: cancellationToken);
            return;
        }

        if (result.Errors is [DuplicateSkuError duplicate])
        {
            await Send.ResponseAsync(
                new ApiError(DuplicateSkuError.Code, duplicate.Message),
                StatusCodes.Status409Conflict,
                cancellationToken);
            return;
        }

        throw new InvalidOperationException("An application error has no HTTP mapping.");
    }
}

public sealed class GetProductEndpoint : EndpointWithoutRequest<ProductCreated>
{
    public override void Configure()
    {
        Get("/products/{id:guid}");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken cancellationToken) =>
        Send.NotFoundAsync(cancellationToken);
}
```

The endpoint never reads `result.Value` until success is established. It maps the single known conflict to `409`; an unmapped application error is a server-side contract defect, not a generic client validation failure. `CreatedAtAsync` uses the GET endpoint's route metadata to form the location. Both endpoints are anonymous solely to keep authentication outside this recipe; production endpoints must make that choice explicitly and normally apply a reviewed policy.

Register the application service and FastEndpoints once:

```csharp
using FastEndpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IProductService, DemoProductService>();
builder.Services.AddFastEndpoints();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.UseFastEndpoints(configuration =>
    configuration.Errors.UseProblemDetails(options =>
        options.IndicateErrorCode = true));

await app.RunAsync();
```

FastEndpoints owns validator discovery, execution, and validation response construction; ASP.NET Core problem-details services support the host exception boundary for unexpected failures. `UseProblemDetails` includes FluentValidation error codes in validation problem responses, keeping malformed-input responses separate from the typed `ApiError` used for the business conflict. In production, configure a deliberate exception handler that returns safe problem details and records correlation data without request secrets.

## Failure modes and operations

| Symptom | Likely boundary | Observation and safe response |
| --- | --- | --- |
| Invalid input reaches the service | Validator discovery | Confirm the validator derives from `FastEndpoints.Validator<T>`, its assembly is discovered, and only one validation strategy owns the endpoint. |
| A failed result throws while mapping | Endpoint mapping | Search for `Value` access before `IsSuccess`; branch first and test every failure subtype. |
| Known duplicates return `500` | Application/transport contract | Verify the persistence adapter creates the expected typed error and the endpoint maps it to `409`. Do not catch all exceptions as duplicates. |
| Unexpected database failures return `409` | Persistence adapter | Narrow exception classification to the known database constraint and rethrow unknown failures for central handling. |
| Validation telemetry has high cardinality | Observability | Count stable validation/error codes and endpoint names, not raw values, messages, SKUs, request bodies, or full result graphs. |

Observe validation rejection rate by rule code, expected outcome rate by typed error code, endpoint latency, cancellation, and unhandled exception rate. A sudden increase in `sku_already_exists` may be normal contention or a caller retry loop; correlate it with operation and trace identifiers, never customer-provided values.

## Verification checklist

Authoring evidence:

- [ ] The role-separated library and standalone Web fragments were not
  recompiled after this documentation boundary correction.
- [ ] The deterministic service was not presented as a database integration test.

Consuming-application checks:

- [ ] Empty, too-long, and malformed values are rejected before the service is invoked.
- [ ] A successful service result produces `201` and a valid location.
- [ ] The known duplicate result produces the stable `sku_already_exists` response with `409`.
- [ ] Cancellation and unknown infrastructure failures reach the central exception/telemetry boundary.
- [ ] Every concrete application error has an explicit HTTP mapping test.
- [ ] Responses, logs, metrics, and traces contain no request secrets or full error causal graphs.

## Related guides

- [FastEndpoints](../packages/fastendpoints.md)
- [FluentValidation](../packages/fluentvalidation.md)
- [FluentResults](../packages/fluentresults.md)
- [FastEndpoints.OpenApi](../packages/fastendpoints-openapi.md)

## Primary sources

Accessed 2026-07-27.

- [FastEndpoints validation](https://fast-endpoints.com/docs/validation)
- [FastEndpoints configuration settings](https://fast-endpoints.com/docs/configuration-settings)
- [FastEndpoints 8.2.0 on NuGet](https://www.nuget.org/packages/FastEndpoints/8.2.0)
- [FluentValidation documentation](https://docs.fluentvalidation.net/)
- [FluentValidation 12.1.1 on NuGet](https://www.nuget.org/packages/FluentValidation/12.1.1)
- [FluentResults upstream repository and documentation](https://github.com/altmann/FluentResults)
- [FluentResults 4.0.0 on NuGet](https://www.nuget.org/packages/FluentResults/4.0.0)
