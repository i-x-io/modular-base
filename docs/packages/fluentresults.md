# FluentResults

## Catalog entry

`FluentResults` **4.0.0** — direct catalog package; result-pattern types that model successful values and expected failures.

- **Owner:** IX
- **Last reviewed:** 2026-07-27
**Review trigger:** `FluentResults` version changes, target-framework changes, or upstream result/error API changes.

## Decision and scope

Use FluentResults for expected, caller-actionable outcomes from externally visible service operations. `Library`, `Contracts`, `Abstractions`, `Adapter`, and `Integration` may intentionally expose this public dependency because the result shape is part of their service contract. This narrow exception does not authorize hosting, transport, persistence, or logging implementation dependencies in neutral roles. Do not use results to conceal programming faults, cancellation, corrupt state, broken invariants, or unexpected infrastructure failures.

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

public sealed class CustomerRequiredError : Error
{
    public const string Code = "customer_required";

    public CustomerRequiredError()
        : base("A customer is required.")
    {
    }
}

public static class Example
{
    public static void Main()
    {
        Result<Guid> result = CreateOrder("customer-42");
        if (result.IsFailed)
        {
            if (result.Errors[0] is CustomerRequiredError)
            {
                Console.WriteLine($"Rejected: {CustomerRequiredError.Code}");
            }
            return;
        }

        Console.WriteLine($"Created: {result.Value}");
    }

    private static Result<Guid> CreateOrder(string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Result.Fail<Guid>(new CustomerRequiredError());
        }

        return Result.Ok(Guid.NewGuid());
    }
}
```

## Enterprise implementation guidance

Each concrete business error derives from `Error` and declares its own `public const string Code` using lowercase snake case. The code is the stable machine-readable contract; the message is explanatory text only. Callers branch on the concrete error type or code, never on `Error.Message`. Keep error messages safe for clients and protected diagnostic detail in structured telemetry or the preserved exception chain. A boundary maps its own errors once to HTTP, messaging, or UI states; reusable services must not introduce transport envelopes or status codes. Every public error code needs a stable, safe boundary mapping. Treat a missing or unknown code as a server-side mapping defect, not a client failure; a boundary may use that mapping to distinguish rejection from retry decisions.

Compose operations with `Bind`/`Map` and preserve all meaningful `Result.Errors`. Do not use string-only `Result.Fail`, directly instantiate the unclassified `Error` for a business failure, or call `Result.Try`: its broad internal catch cannot prove that every translated exception is a documented expected outcome. The analyzer verifies direct, statically visible construction; factories, exception translation, message safety, and state changes still need review.

When several failures are useful to the caller, preserve `Errors` instead of collapsing them into one string. Use `CausedBy` for internal causal detail only if the resulting error will not cross a trust boundary.

### Upgrade and rollback

Before upgrading, compile all composition and mapping code, then test multi-error preservation, metadata keys, causal chains, and boundary serialization. Keep application error codes independent of package messages. Rollback is a central-version re-pin and redeploy unless result objects were incorrectly persisted or exposed as a wire contract.

## Integration with the catalog

Use `fluentvalidation.md` for request validation; translate validation failures into the application result contract at the boundary. `polly.md` reports resilience outcomes; it should not be substituted for domain results.

See the [validation/results recipe](../recipes/fastendpoints-validation-results.md) and [`FluentResults` supply-chain entry](../package-guidance/supply-chain.md#fluentresults).

## Security, performance, AOT, trimming, and operations

Avoid recording secrets, exception text, or customer data in errors and metadata that can cross process boundaries. `Value` throws for failed results; prefer explicit branching, and do not use `ValueOrDefault` when `default` is a valid value. Result objects do not create telemetry automatically: record the stable code, operation, and correlation identifier at the boundary without logging the full result graph. No package AOT/trimming guarantee is documented; validate the actual published workload.

## Avoid

Do not wrap every exception as a generic failure, translate cancellation, use `Result.Try`, return successful results with error payloads, or use `ValueOrDefault` when a missing value would be ambiguous. Catch a specific exception only when it is a fully understood, documented, caller-actionable outcome and return a coded error; otherwise propagate it. A broad `catch (Exception)` or untyped catch may log or clean up only before a bare `throw;`.

## Verification checklist

- [ ] Test success, expected failure, and unexpected exception paths.
- [ ] Assert every concrete public business error has an own `public const string Code` in lowercase snake case.
- [ ] Assert every public error code has a stable, safe boundary mapping; unknown codes fail as server-side mapping defects.
- [ ] Assert expected failures preserve all meaningful errors and unexpected exceptions, including cancellation, propagate.
- [ ] Verify failed `Result<T>` paths never access `Value`.
- [ ] Confirm logs and serialized errors contain no secrets or personal data.

## Sources

- [NuGet Gallery: FluentResults 4.0.0](https://www.nuget.org/packages/FluentResults/4.0.0) (Accessed 2026-07-27)
- [FluentResults upstream documentation and examples](https://github.com/altmann/FluentResults) (Accessed 2026-07-27)
