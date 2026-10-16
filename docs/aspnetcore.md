# ASP.NET Core authorization

The `TruthWeaver.AspNetCore` package lets a TruthWeaver rule decide an ASP.NET Core authorization policy. A rule policy is an ordinary policy, so `[Authorize(Policy = ...)]`, `RequireAuthorization`, resource-based checks with `IAuthorizationService.AuthorizeAsync(user, resource, policy)` and the Blazor `AuthorizeView` component use it with no change. Back to the [README](../README.md).

The package references the `Microsoft.AspNetCore.App` shared framework, `TruthWeaver` and `TruthWeaver.Abstractions`. It has no other dependency.

## Set up

1. Compile each rule once, at startup, over `AuthorizationContext<TResource>`. Register the ready-made predicates on the registry builder.
2. Add the policies with `AddTruthWeaverPolicy` in `AddAuthorization`.
3. Register the rule handler with `AddTruthWeaverAuthorization`.
4. Optional: register problem-details responses with `AddTruthWeaverAuthorizationProblemDetails`.

```csharp
RuleCompiler<AuthorizationContext<Document>> compiler = new(
    PredicateRegistry<AuthorizationContext<Document>>
        .CreateBuilder()
        .AddAuthorizationPredicates()
        .AddIsOwner(document => document.OwnerId)
        .Build()
);
CompiledRule<AuthorizationContext<Document>> canEdit = compiler.Compile("isOwner OR isInRole(role: \"editor\")").GetRuleOrThrow();

builder.Services.AddAuthorization(options => options.AddTruthWeaverPolicy("CanEdit", canEdit));
builder.Services.AddTruthWeaverAuthorization().AddTruthWeaverAuthorizationProblemDetails();
```

`AddTruthWeaverAuthorization` registers the handler and `IHttpContextAccessor`. It does not add authorization itself, so the host still calls `AddAuthorization`. A second call adds nothing.

To add a rule to a policy that has other requirements, use `RequireRule` on the policy builder:

```csharp
options.AddPolicy("CanEdit", policy => policy.RequireAuthenticatedUser().RequireRule(canEdit));
```

## Use a policy

An endpoint policy works as any other policy. The authorization middleware evaluates the rule before the endpoint runs:

```csharp
app.MapGet("/documents/{id}", GetDocument).RequireAuthorization("CanRead");
```

For a resource-based check, load the resource, then ask the authorization service. The rule gets the resource in `AuthorizationContext<TResource>.Resource`:

```csharp
AuthorizationResult result = await authorization.AuthorizeAsync(http.User, document, "CanEdit");
if (!result.Succeeded)
{
    return Results.Forbid();
}
```

`AuthorizeView` in Blazor calls the same authorization service, so `<AuthorizeView Policy="CanRead">` evaluates the rule.

## The authorization context

`AuthorizationContext<TResource>` is the context of a rule policy. It has three properties:

| Property | Value |
| --- | --- |
| `User` | The `ClaimsPrincipal` of the check. |
| `Resource` | The resource of the check when it is a `TResource`. Otherwise `null` (or the default value of a value type). An endpoint policy gives the `HttpContext` as the resource, so `Resource` is `null` for an endpoint policy. |
| `Http` | The `HttpContext` of the request, or `null` when the check runs outside a request. |

## Ready-made predicates

`AddAuthorizationPredicates` registers three predicates, and `AddIsOwner` registers a fourth. The names are also constants on `AuthorizationPredicates`.

| Predicate | Arguments | Result |
| --- | --- | --- |
| `hasClaim` | `type` (string), optional `value` (string) | `True` when the user has a claim of the type (case is ignored), with the value when the rule gives one (ordinal comparison). Otherwise `False`. |
| `isInRole` | `role` (string) | `True` when `ClaimsPrincipal.IsInRole(role)` is true. Otherwise `False`. |
| `isAuthenticated` | None | `True` when the user has an authenticated identity. Otherwise `False`. |
| `isOwner` | None | `True` when the owner identifier of the resource equals the user identifier (ordinal comparison), `False` when they differ. `Unknown` when the check has no resource, the resource has no owner, or the user has no identifier claim. |

`AddIsOwner(ownerSelector, claimType)` takes the function that reads the owner identifier of a resource. The user identifier is the first claim of `claimType`. The default claim type is `ClaimTypes.NameIdentifier`.

`hasClaim`, `isInRole` and `isAuthenticated` never give `Unknown`, because the claims of the user are known.

## Use your own context type

To evaluate a rule over your own context type, give a map function. The function gets the `AuthorizationHandlerContext` (with the user and the resource) and the `HttpContext`, or `null` outside a request:

```csharp
options.AddTruthWeaverPolicy("CanApprove", approvalRule, (context, http) => new ApprovalContext(context.User, context.Resource as Invoice));
```

`RequireRule` has the same overload. An exception from the map function is not a fault. It goes to the caller of the authorization check.

## Results

A rule policy is fail-closed. Only a `True` rule meets the requirement, unless the policy allows `Unknown`:

| Rule result | `AllowUnknown` off (the default) | `AllowUnknown` on |
| --- | --- | --- |
| `True` | Met | Met |
| `False` | Denied | Denied |
| `Unknown` | Denied | Met |
| `Unknown` because of a fault | Denied | Met |
| `True` with a fault | Met | Met |

A fault becomes `Unknown` for its term, as in each evaluation. A rule that is `True` with a fault did not need the faulted term, so the requirement is met. To allow `Unknown` for one policy, set it in the options:

```csharp
options.AddTruthWeaverPolicy("CanPreview", canPreview, new RuleRequirementOptions { AllowUnknown = true });
```

A denied endpoint policy gives the default response of the authentication scheme: 401 (a challenge) when the user is not authenticated, and 403 (a forbid) when the user is authenticated.

## Options

`RuleRequirementOptions` has these properties:

| Property | Default | Meaning |
| --- | --- | --- |
| `Name` | `null` | The name of the requirement for logs and problem-details responses. When it is `null`, `AddTruthWeaverPolicy` uses the policy name and `RequireRule` uses `"rule"` (`RuleRequirement.DefaultName`). The name is never the rule text, because a response shows the name to the client. |
| `AllowUnknown` | `false` | When `true`, an `Unknown` result meets the requirement. |
| `Evaluation` | `null` | The `EvaluationOptions` of each check: the fault budget, the mode and the timeout. `null` gives the default options. |

## The decision of a denied check

Each denied rule requirement keeps its full `Decision`, with the faults and the trace. The decision is in three places:

- **The authorization result.** The failure has a `RuleAuthorizationFailureReason` for each denied rule requirement, with `Requirement` and `Decision`. `result.GetRuleFailures()` returns them for an `AuthorizationResult`, and `failure.GetRuleFailures()` for an `AuthorizationFailure`.
- **The request.** When the check runs in a request, the handler adds each failure to the `RuleAuthorizationFailureFeature` of the request. Read it with `HttpContext.Features.Get<RuleAuthorizationFailureFeature>()`. This is the only place for the decision of a 401 response, because a challenge result has no `AuthorizationFailure`.
- **The log.** The handler writes one `Information` entry for each denied requirement, with event id 1 (`RuleRequirementDenied`), the requirement name, the result and the number of faults. The category is `TruthWeaver.AspNetCore.RuleAuthorizationHandler`.

The decision can hold sensitive data, such as term arguments and fault messages. Write it to a log. Do not send it to the client unless you have checked what it holds.

## Problem-details responses

`AddTruthWeaverAuthorizationProblemDetails` adds the problem-details service and replaces the authorization middleware result handler. The new handler keeps the default behavior: the scheme challenges or forbids. Then, when a rule requirement denied the request and the response is a 401 or 403 with no body, it writes a problem-details body:

| Member | Value |
| --- | --- |
| `status` | 401 or 403, the status that the scheme set. |
| `detail` | The requirement name and the rule result. |
| `requirement` | The requirement name. |
| `result` | The rule result: `False` or `Unknown`. |

The body has no faults and no trace. A scheme that redirects or writes its own body, such as a cookie scheme, keeps its response. To add more to the body, read the feature in a `CustomizeProblemDetails` callback:

```csharp
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    if (context.HttpContext.Features.Get<RuleAuthorizationFailureFeature>() is { } feature)
    {
        context.ProblemDetails.Extensions["faults"] = feature.Failures[0].Decision.Faults.Count;
    }
});
```

The result handler applies to endpoint policies only. For a resource-based check, write the response in the endpoint from `result.GetRuleFailures()`.

## Services and cancellation

- **Services.** In a request, the rule evaluates with `HttpContext.RequestServices`, so a class-based predicate can use scoped services. Outside a request, it uses the root service provider, which cannot give scoped services.
- **Cancellation.** In a request, `HttpContext.RequestAborted` cancels the evaluation. A cancelled evaluation, and an expired `EvaluationOptions.Timeout`, throw `OperationCanceledException` to the caller of the check. Neither is a fault.
- **Resource-based checks.** The handler finds the request of a resource-based check with `IHttpContextAccessor`, which `AddTruthWeaverAuthorization` registers.

## Trimming and Native AOT

The package is marked as AOT-compatible, and the trim and Native AOT analyzers report no warning for it. It uses no reflection: the log entries come from the `LoggerMessage` source generator, and the package has no serializer of its own.

The problem-details body goes through `IProblemDetailsService`, which serializes it with `System.Text.Json`. The package adds only string values to the body. In a Native AOT application, a value that a `CustomizeProblemDetails` callback adds must be a type that the JSON options of the application can serialize. A class-based predicate comes from the container, as with the `TruthWeaver` package, so register its type with the container.

## Sample

The [AspNetCore sample](../samples/AspNetCore/) is a minimal API with three policies: an endpoint policy, a resource-based check and a policy whose predicate calls a service that can fail. Its [tests](../samples/AspNetCore.Tests/) run the application on a test server.
