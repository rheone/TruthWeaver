# Policy sets

The `TruthWeaver.Policies` package combines many rules into one answer. A policy has an effect (permit or deny), a condition, an optional target and a priority. A policy set combines its policies with a combining algorithm. Back to the [README](../README.md).

The package references `TruthWeaver` and `TruthWeaver.Abstractions`. It has no other dependency.

## Define a set

A policy condition and target are compiled rules over the same context type. Compile them once, then build the set once and evaluate it for many contexts.

```csharp
CompiledRule<Request> isAdmin = compiler.Compile("isInRole(role: \"admin\")").GetRuleOrThrow();
CompiledRule<Request> isBlocked = compiler.Compile("isBlocked").GetRuleOrThrow();
CompiledRule<Request> isDocument = compiler.Compile("resourceIs(kind: \"document\")").GetRuleOrThrow();

PolicySet<Request> documents = new(
    "documents",
    CombiningAlgorithm.DenyOverrides,
    [
        new Policy<Request>("admins", PolicyEffect.Permit, isAdmin),
        new Policy<Request>("blocked", PolicyEffect.Deny, isBlocked, priority: 10),
    ],
    target: isDocument
);

PolicyDecision decision = await documents.EvaluateAsync(request);
if (!decision.IsPermitted)
{
    // Deny, NotApplicable and Indeterminate all end here.
}
```

## When a policy applies

A policy applies when its target and its condition are both true, using Strong Kleene AND. A policy with no target is for every request.

| Target | Condition | The policy |
| --- | --- | --- |
| `True` | `True` | applies |
| `True` | `False` | does not apply |
| `True` | `Unknown` | is `Unknown` |
| `False` | any | does not apply, and the condition is not evaluated |
| `Unknown` | `True` or `Unknown` | is `Unknown` |
| `Unknown` | `False` | does not apply |

A policy outside its target does not evaluate its condition, so it records none of the faults of the condition.

A set has its own optional target. A `False` set target gives `NotApplicable` and evaluates no policy. An `Unknown` set target turns any outcome other than `NotApplicable` into `Indeterminate`.

## Combining algorithms

| Algorithm | The set answers |
| --- | --- |
| `DenyOverrides` | `Deny` when a deny applies. Else `Indeterminate` when a deny is `Unknown`. Else `Permit` when a permit applies. Else `Indeterminate` when a permit is `Unknown`. Else `NotApplicable`. |
| `PermitOverrides` | `Permit` when a permit applies. Else `Indeterminate` when a permit is `Unknown`. Else `Deny` when a deny applies. Else `Indeterminate` when a deny is `Unknown`. Else `NotApplicable`. |
| `FirstApplicable` | The effect of the first policy, in priority order, that is not `False`. `Indeterminate` when that policy is `Unknown`. `NotApplicable` when every policy is `False`. |

An `Unknown` policy is never a silent permit under deny-overrides, and never a silent deny under permit-overrides. A rule that faults is `Unknown`, so a faulting policy blocks in the same way. There is no option to ignore `Unknown`.

## Priority

A policy with a higher priority is evaluated first. Equal priorities keep the order in which the policies were given. Under `FirstApplicable` this decides which policy wins. Under the two overriding algorithms the outcome does not depend on the order. Priority then decides which policies run before the evaluation stops, and the order of the results.

Evaluation stops early. The overriding algorithms stop at the first definite result of the overriding effect. `FirstApplicable` stops at the first policy that is not `False`. A policy that was not reached is not in `PolicyDecision.Results`.

## Shared predicates

A set evaluates the target, every policy's target and every policy's condition against one shared evaluation state for a context, so a predicate common to several of those rules runs once, not once per rule that references it. Two terms share this way when their predicate name and arguments are the same, whichever rule compiled them.

This sharing also makes the fault budget (`EvaluationOptions.FaultBudget`) a whole-set concept: once the Nth fault is recorded anywhere in the set's evaluation, every rule still to run answers `Unknown` for the rest of its own evaluation, the same way a fault budget aborts within one rule. A shared term that faults still counts once, not once per rule, so a budget is not spent faster by two rules naming the same predicate; each rule that references the faulting term still lists that one fault in `PolicyDecision.Decision.Faults`.

## The decision

`PolicyDecision` has these members.

| Member | Meaning |
| --- | --- |
| `Outcome` | `Permit`, `Deny`, `NotApplicable` or `Indeterminate`. |
| `Decision` | A `Decision` with `True` for `Permit`, `Unknown` for `Indeterminate`, and `False` for `Deny` and `NotApplicable`. Its faults come from every rule that ran. |
| `Results` | The `PolicyResult` of each evaluated policy: its id, effect and whether it applies. |
| `IsPermitted` | `true` only for `Permit`. It is `Decision.IsSatisfied`, so it fails closed. |

## Errors

A constructor throws `ArgumentException` for a blank id, a duplicate policy id or an undefined enum value. A rule that faults does not throw: it is `Unknown` and the fault is in `Decision.Faults`. Cancellation of the caller token, or the `EvaluationOptions.Timeout` of a rule, throws `OperationCanceledException`.
