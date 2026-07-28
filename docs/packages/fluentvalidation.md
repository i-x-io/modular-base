# FluentValidation

## Catalog entry

`FluentValidation` **12.1.1** — direct catalog package; strongly typed validation-rule framework.

- **Adoption:** Direct
- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** `FluentValidation` version changes, target-framework changes, or validation execution/default changes.

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

### Configuration reference

| Setting | Purpose | Default behavior | Production guidance | Reload | Sensitive | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| Rule/class cascade mode | Controls whether later rules continue | FluentValidation defaults | Set deliberately where later rules would be invalid or expensive | Recreate validator/restart | No | Changes number/order of failures |
| Rule sets | Selects validation profiles | Default rules execute unless selected otherwise | Use stable application-owned names and test every endpoint profile | Code deployment | No | Rules may be omitted from execution |
| `Validate` vs `ValidateAsync` | Selects execution path | Caller chooses | Always call `ValidateAsync` when any rule is asynchronous | Per call | No | Async rule invoked synchronously throws |
| Culture/message localization | Formats built-in messages | Current/default culture | Return stable codes; localize only at presentation boundary | Per request/culture | No | Message text varies across cultures/versions |

### Upgrade and rollback

Read every intervening major-version guide and compile validators before deployment; major releases can change target frameworks and validation behavior. Exercise synchronous and asynchronous rules explicitly, and upgrade the DI extensions package on the same version line. Roll back both pins together; validation-rule changes may require the application rollback too.

## Integration with the catalog

Registration/scanning lives in [FluentValidation.DependencyInjectionExtensions](fluentvalidation-dependencyinjectionextensions.md). Convert validation failures to the contract described in [FluentResults](fluentresults.md); cross-reference [FastEndpoints](fastendpoints.md) for endpoint wiring.

See the [validation/results recipe](../recipes/fastendpoints-validation-results.md) and [`FluentValidation` supply-chain entry](../package-guidance/supply-chain.md#fluentvalidation).

## Security, performance, AOT, trimming, and operations

Automatic MVC validation is synchronous and MVC-specific; asynchronous rules require `ValidateAsync`, and calling `Validate` when async rules exist throws in current FluentValidation. Avoid existence checks and different error wording that reveal sensitive records. Bound collection sizes before per-item validation and keep remote calls out of hot validation paths where possible. Reflection-based discovery is a trimming concern when used through the companion DI package; FluentValidation 12 requires .NET 8 or later and is compatible with this catalog's `net10.0` target.

Record validation duration, stable rule/error codes, and failure counts at the application boundary. Never attach rejected values, full request bodies, tokens, or customer data to logs, spans, or metric labels.

### Troubleshooting

| Symptom | Likely cause | Diagnostic | Correction | Retry? |
| --- | --- | --- | --- | --- |
| Async validator throws during execution | `Validate`/synchronous auto-validation invoked async rules | Inspect the exception and rule chain | Invoke `ValidateAsync` in an async-capable boundary | No |
| Expected rule does not run | Rule-set selection, condition, or missing validator invocation | Assert selected rule sets and validator type in a focused test | Fix boundary selection/registration and add a contract test | No |
| Validation latency regresses | I/O rule, cascade choice, or repeated execution | Time validators/rules without recording values | Remove I/O where possible, stop safely, or call once | Retry only the underlying explicitly transient dependency |

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
