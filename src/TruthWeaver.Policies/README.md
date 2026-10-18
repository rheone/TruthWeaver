# TruthWeaver.Policies

Policies and policy sets: combine several rules into one permit, deny, not applicable or indeterminate outcome.

## Purpose

A `Policy<TContext>` has an effect (`Permit` or `Deny`), a condition rule, an optional target rule and a priority. A
`PolicySet<TContext>` holds policies and a `CombiningAlgorithm`. You build the set once and evaluate it for many
contexts. The set evaluates in Strong Kleene logic, and the policies share one fault budget.

An `Unknown` result never becomes a permit. If a policy may apply and the answer could change the outcome, the set
returns `Indeterminate`.

| Algorithm | Which policy wins |
| --- | --- |
| `DenyOverrides` | A definite deny. Without one, an `Unknown` policy that would deny gives `Indeterminate`. Then a permit wins. |
| `PermitOverrides` | A definite permit. Without one, an `Unknown` policy that would permit gives `Indeterminate`. Then a deny wins. |
| `FirstApplicable` | The first policy in priority order that is not `NotApplicable`. An `Unknown` policy stops the walk with `Indeterminate`. |

```csharp
PolicySet<Order> set = new(
    "orders",
    CombiningAlgorithm.DenyOverrides,
    [
        new Policy<Order>("staff-permit", PolicyEffect.Permit, staffRule),
        new Policy<Order>("blocked-deny", PolicyEffect.Deny, blockedRule, priority: 10),
    ]
);

PolicyDecision decision = await set.EvaluateAsync(order);
bool allowed = decision.Outcome == PolicyOutcome.Permit;
```

## Depends on

- [TruthWeaver.Abstractions](../TruthWeaver.Abstractions/README.md) for `TruthValue` and `Decision`.
- [TruthWeaver](../TruthWeaver/README.md) for `CompiledRule<TContext>` and the evaluator.

## Key types

| Type | Role |
| --- | --- |
| `Policy<TContext>` | One effect, a condition, an optional target and a priority. |
| `PolicySet<TContext>` | The policies, the algorithm and an optional target. `EvaluateAsync` returns a `PolicyDecision`. |
| `CombiningAlgorithm` | `DenyOverrides`, `PermitOverrides` or `FirstApplicable`. |
| `PolicyEffect` | What a policy asks for when it applies: `Permit` or `Deny`. |
| `PolicyOutcome` | `Permit`, `Deny`, `NotApplicable` or `Indeterminate`. |
| `PolicyDecision` | The outcome, the combined `Decision` and one `PolicyResult` for each policy. |
| `PolicyResult` | The policy identifier, its effect and whether it applies. |

## Notes

Treat `Indeterminate` as a denial unless you have a reason not to. Only `Permit` is a permit.
