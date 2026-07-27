# FluentValidation

## Catalog entry

`FluentValidation` **12.1.1** — direct catalog package; strongly typed validation-rule framework.

## Decision and scope

Use for explicit input and command validation. Validation establishes input acceptability; authorization, uniqueness, and transactional invariants remain application/domain concerns.

## Recommended registration and use

Inject `IValidator<T>` and call `ValidateAsync` from asynchronous endpoints/handlers. Keep validators focused on one request type and use rule sets only when the contract genuinely has distinct validation modes.

## Enterprise implementation guidance

Make external checks asynchronous, cancellation-aware, and bounded. Map validation failures consistently at the HTTP boundary. FastEndpoints integrations should invoke the validator using the framework's supported validation path; do not assume MVC's automatic-validation behavior applies.

## Integration with the catalog

Registration/scanning lives in `fluentvalidation-dependencyinjectionextensions.md`. Convert validation failures to the contract described in `fluentresults.md`; cross-reference the catalog's FastEndpoints package documentation for endpoint wiring.

## Security, performance, AOT, trimming, and operations

Automatic MVC validation is synchronous and MVC-specific; asynchronous rules require `ValidateAsync`. Avoid existence checks that reveal sensitive records. Reflection-based discovery is a trimming concern when used through the companion DI package.

## Avoid

Do not call `Validate` for a validator containing asynchronous rules, perform unbounded remote calls in rules, or use validation as authorization.

## Verification checklist

- Unit-test valid, invalid, boundary, and conditional rules.
- Test async rules with cancellation and dependency failure behavior.
- Verify FastEndpoints returns the agreed validation response shape.

## Sources

- https://www.nuget.org/packages/FluentValidation/12.1.1 (Accessed 2026-07-27)
- https://docs.fluentvalidation.net/en/latest/async.html (Accessed 2026-07-27)
- https://docs.fluentvalidation.net/en/latest/aspnet.html (Accessed 2026-07-27)
