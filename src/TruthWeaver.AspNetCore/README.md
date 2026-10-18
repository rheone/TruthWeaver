# TruthWeaver.AspNetCore

Use a TruthWeaver rule as an ASP.NET Core authorization requirement.

## Purpose

A rule becomes an ordinary `IAuthorizationRequirement`, so it works with `[Authorize(Policy = ...)]`,
`RequireAuthorization` and `IAuthorizationService.AuthorizeAsync`. The rule reads an `AuthorizationContext<TResource>`
that holds the user, the resource of the check and the HTTP context. You can also map the check to your own context type.

The requirement fails closed:

- `False` denies the request.
- `Unknown` denies the request, unless `RuleRequirementOptions.AllowUnknown` is `true`.
- A fault in the rule gives `Unknown`, so it denies the request by default.

```csharp
builder.Services.AddTruthWeaverAuthorization();
builder.Services.AddAuthorization(options =>
    options.AddTruthWeaverPolicy<Order>("CanEdit", canEditRule)
);
```

`AddTruthWeaverAuthorizationProblemDetails` makes a denial write a problem-details body. A handler can read the full
`Decision` of a failed rule with `GetRuleFailures`.

## Depends on

- [TruthWeaver.Abstractions](../TruthWeaver.Abstractions/README.md) for `TruthValue` and `Decision`.
- [TruthWeaver](../TruthWeaver/README.md) for `CompiledRule<TContext>` and the evaluator.

## Key types

| Type | Role |
| --- | --- |
| `RuleRequirement<TContext>` | The requirement. It holds a compiled rule and the map from the check to the context. |
| `RuleRequirementOptions` | The name, `AllowUnknown` and the evaluation options of one requirement. |
| `AuthorizationContext<TResource>` | The user, the resource and the HTTP context that a rule reads. |
| `AuthorizationPredicates` | Registers predicates over the user and the resource, such as `AddIsOwner`. |
| `RuleAuthorizationPolicyExtensions` | `RequireRule` and `AddTruthWeaverPolicy`. |
| `TruthWeaverAuthorizationServiceCollectionExtensions` | `AddTruthWeaverAuthorization` and `AddTruthWeaverAuthorizationProblemDetails`. |
| `RuleAuthorizationFailureReason` | The failed requirement and the `Decision` of its rule. |

## Notes

The `Decision` of a failed rule can hold term arguments and fault messages. Write it to a log, not to a response. The
problem-details body shows only the requirement name and the rule result.
