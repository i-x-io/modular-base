# FluentResults

## Catalog entry

`FluentResults` **4.0.0** — direct catalog package; result-pattern types that model successful values and expected failures.

## Decision and scope

Use for expected, caller-actionable outcomes across application boundaries. Do not use it to conceal programming faults, cancellation, or infrastructure failures that need normal exception/telemetry handling.

## Recommended registration and use

The catalog supplies the version centrally, so the consuming project keeps the reference versionless:

```xml
<ItemGroup>
  <PackageReference Include="FluentResults" />
</ItemGroup>
```

No dependency-injection registration is required. Return `Result` for no-value outcomes and `Result<T>` for successful values. Inspect `IsSuccess`/`IsFailed` before reading `Value`:

```csharp
using FluentResults;

static Result<Guid> CreateOrder(string customerId)
{
    if (string.IsNullOrWhiteSpace(customerId))
    {
        return Result.Fail<Guid>(
            new Error("A customer is required.")
                .WithMetadata("code", "customer_required"));
    }

    return Result.Ok(Guid.NewGuid());
}

var result = CreateOrder("customer-42");
if (result.IsFailed)
{
    var code = result.Errors[0].Metadata["code"];
    Console.WriteLine($"Rejected: {code}");
    return;
}

Console.WriteLine($"Created: {result.Value}");
```

## Enterprise implementation guidance

Keep error messages safe for clients and retain diagnostic detail in structured logs. A common workflow is: create a typed domain error, compose operations with `Bind`/`Map`, branch once at the application boundary, then map the application-owned metadata code to HTTP problem details, a message rejection, or a retry decision. Preserve stable codes rather than making callers parse `Error.Message`; treat missing or unknown codes as an explicit server-side mapping defect.

When several failures are useful to the caller, preserve `Errors` instead of collapsing them into one string. Use `CausedBy` for internal causal detail only if the resulting error will not cross a trust boundary.

## Integration with the catalog

Use `fluentvalidation.md` for request validation; translate validation failures into the application result contract at the boundary. `polly.md` reports resilience outcomes; it should not be substituted for domain results.

## Security, performance, AOT, trimming, and operations

Avoid recording secrets, exception text, or customer data in errors and metadata that can cross process boundaries. `Value` throws for failed results; prefer explicit branching, and do not use `ValueOrDefault` when `default` is a valid value. Result objects do not create telemetry automatically: record the stable code, operation, and correlation identifier at the boundary without logging the full result graph. No package AOT/trimming guarantee is documented; validate the actual published workload.

## Avoid

Do not wrap every exception as a generic failure, return successful results with error payloads, or use `ValueOrDefault` when a missing value would be ambiguous.

## Verification checklist

- [ ] Test success, expected failure, and unexpected exception paths.
- [ ] Assert every public error code maps to a stable, safe transport response.
- [ ] Verify failed `Result<T>` paths never access `Value`.
- [ ] Confirm logs and serialized errors contain no secrets or personal data.

## Sources

- [NuGet Gallery: FluentResults 4.0.0](https://www.nuget.org/packages/FluentResults/4.0.0) (Accessed 2026-07-27)
- [FluentResults upstream documentation and examples](https://github.com/altmann/FluentResults) (Accessed 2026-07-27)
