# FluentValidation

## Catalog entry

`FluentValidation` **12.1.1** — direct catalog package; strongly typed validation-rule framework.

## Decision and scope

Use for explicit input and command validation. Validation establishes input acceptability; authorization, uniqueness, and transactional invariants remain application/domain concerns.

## Recommended registration and use

The catalog supplies the version centrally, so the consuming project keeps the reference versionless:

```xml
<ItemGroup>
  <PackageReference Include="FluentValidation" />
</ItemGroup>
```

Define one validator per request or command, inject `IValidator<T>`, and call `ValidateAsync` from asynchronous endpoints or handlers:

```csharp
using FluentValidation;

public sealed record CreateUser(string Email, int Age);

public sealed class CreateUserValidator : AbstractValidator<CreateUser>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).InclusiveBetween(18, 120);
    }
}

static async Task<IReadOnlyDictionary<string, string[]>> ValidateRequestAsync(
    CreateUser request,
    IValidator<CreateUser> validator,
    CancellationToken cancellationToken)
{
    var result = await validator.ValidateAsync(request, cancellationToken);
    return result.IsValid
        ? new Dictionary<string, string[]>()
        : result.ToDictionary();
}
```

Use rule sets only when the same contract genuinely has distinct validation modes. For an asynchronous `MustAsync`/`CustomAsync` rule, pass its supplied cancellation token to the dependency and always invoke the validator with `ValidateAsync`.

## Enterprise implementation guidance

The usual workflow is: deserialize, validate once, stop on failure, convert failures to the application result/error contract, and execute the command only after success. Map `PropertyName`, `ErrorCode`, and a client-safe message consistently at the HTTP boundary; never expose internal property paths accidentally. Make external checks asynchronous, cancellation-aware, and bounded, but keep race-sensitive uniqueness and authorization checks in the command transaction. FastEndpoints integrations should invoke the validator using the framework's supported validation path; do not assume MVC's automatic-validation behavior applies.

## Integration with the catalog

Registration/scanning lives in `fluentvalidation-dependencyinjectionextensions.md`. Convert validation failures to the contract described in `fluentresults.md`; cross-reference the catalog's FastEndpoints package documentation for endpoint wiring.

## Security, performance, AOT, trimming, and operations

Automatic MVC validation is synchronous and MVC-specific; asynchronous rules require `ValidateAsync`, and calling `Validate` when async rules exist throws in current FluentValidation. Avoid existence checks and different error wording that reveal sensitive records. Bound collection sizes before per-item validation and keep remote calls out of hot validation paths where possible. Reflection-based discovery is a trimming concern when used through the companion DI package; FluentValidation 12 requires .NET 8 or later and is compatible with this catalog's `net10.0` target.

## Avoid

Do not call `Validate` for a validator containing asynchronous rules, perform unbounded remote calls in rules, or use validation as authorization.

## Verification checklist

- [ ] Unit-test valid, invalid, boundary, and conditional rules.
- [ ] Test async rules with cancellation, timeout, and dependency failure behavior.
- [ ] Assert the boundary maps `ErrorCode` and property names to the agreed response shape.
- [ ] Verify the command or handler is not called after validation fails.

## Sources

- [NuGet Gallery: FluentValidation 12.1.1](https://www.nuget.org/packages/FluentValidation/12.1.1) (Accessed 2026-07-27)
- [FluentValidation: creating validators](https://docs.fluentvalidation.net/en/latest/start.html) (Accessed 2026-07-27)
- [FluentValidation: asynchronous validation](https://docs.fluentvalidation.net/en/latest/async.html) (Accessed 2026-07-27)
- [FluentValidation: ASP.NET Core integration](https://docs.fluentvalidation.net/en/latest/aspnet.html) (Accessed 2026-07-27)
- [FluentValidation 12 upgrade guide](https://docs.fluentvalidation.net/en/latest/upgrading-to-12.html) (Accessed 2026-07-27)
