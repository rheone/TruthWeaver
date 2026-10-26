# ADR-0015: Policy sets and combining algorithms

## Status

Accepted (2026-10-17). Feature backlog ticket 07. The owner confirmed the package, the first slice and the Unknown rule on
2026-10-17. The target and priority model and the mirror rule for permit-overrides were left to build time. This ADR
records those choices for review. Obligations are a separate ticket and are not part of this decision.

## Context

A host that decides access often has many rules, each for a group of requests, and needs one answer from them. XACML,
Cedar and OPA solve this with policies, targets and combining algorithms. TruthWeaver has one rule type and a
three-valued result, so the question is how the same ideas fit Strong Kleene logic without a silent permit when an
answer is `Unknown` ([ADR-0001](0001-kleene-failure-model.md)).

## Decision

### 1. A new package, `TruthWeaver.Policies`

It references `TruthWeaver` (for `CompiledRule<TContext>`) and `TruthWeaver.Abstractions`, and nothing else. No other
package references it ([ADR-0004](0004-package-boundaries-and-extensibility.md)). A host opts in.

### 2. A policy is a rule with an effect, an optional target and a priority

`Policy<TContext>` holds an id, a `PolicyEffect` (`Permit` or `Deny`), a condition (a `CompiledRule<TContext>`), an
optional target (a `CompiledRule<TContext>`) and an integer priority. A policy has no logic of its own: the target and the
condition are ordinary rules, so every rule format, predicate and data source works.

A policy applies when `target AND condition` is `True`, with the Strong Kleene AND. A missing target is `True`. The target
is evaluated first, and a `False` target ends the policy without evaluating the condition, so a policy outside its target
records none of the faults of its condition. This is the one place where evaluation order is observable, and it is the
point of a target. A `False` condition makes the policy not applicable even when the target is `Unknown`, as the AND of
`False` and `Unknown` is `False`.

### 3. A set has its own optional target and a combining algorithm

`PolicySet<TContext>` holds an id, a `CombiningAlgorithm`, its policies and an optional target rule. A `False` set target
gives `NotApplicable` and evaluates no policy. An `Unknown` set target keeps `NotApplicable` as it is and turns any other
outcome into `Indeterminate`, because the set may not apply at all. The set is immutable: it copies the policies. All rules of a set share one evaluation state, so the per-call bounds
(`FaultBudget` and `MaxElementEvaluations` in the `EvaluationOptions` of the call) are whole-set bounds, not per-rule
allowances. A set does not give each rule its own allowance, because that would multiply the resource cost of a set by
its number of rules.
Policy ids are unique in a set. A set does not contain a set. Nesting is not needed for the first slice and adds no
semantics that a flat set with targets cannot express.

### 4. Priority is evaluation order

A higher priority is evaluated first and equal priorities keep the order given to the constructor. For
`FirstApplicable` this is the order of the walk, so priority decides which policy wins. For the two overriding
algorithms the result does not depend on the order. Priority then decides only which policies are evaluated before the
walk stops early, and which results appear first in the `PolicyDecision`. A priority never changes an outcome by size
alone.

### 5. Three algorithms, all fail-closed on `Unknown`

The outcome of a set is `Permit`, `Deny`, `NotApplicable` or `Indeterminate`. A policy result is `Applies` (`True`,
`False` or `Unknown`) with its effect.

| Algorithm | Rule |
| --- | --- |
| `DenyOverrides` | A definite deny wins. Else an `Unknown` deny gives `Indeterminate`. Else a definite permit wins. Else an `Unknown` permit gives `Indeterminate`. Else `NotApplicable`. |
| `PermitOverrides` | The mirror: a definite permit wins. Else an `Unknown` permit gives `Indeterminate`. Else a definite deny wins. Else an `Unknown` deny gives `Indeterminate`. Else `NotApplicable`. |
| `FirstApplicable` | The first policy in priority order whose `Applies` is not `False` decides. `True` gives its effect and `Unknown` gives `Indeterminate`. The walk never skips an `Unknown` policy, as a later policy could be shadowed by it. |

The mirror rule for permit-overrides follows from the owner's rule for deny-overrides: the overriding effect is checked
first, a definite result before an `Unknown` one, and an `Unknown` policy of the overriding effect blocks the other
effect. A set therefore never turns an `Unknown` into a silent permit under deny-overrides, and never into a silent deny
under permit-overrides. There is no option to ignore `Unknown`. A fault in a rule is an `Unknown` with a `Fault`
([ADR-0001](0001-kleene-failure-model.md)), so a faulting policy blocks in the same way.

The overriding algorithms stop at the first definite result of the overriding effect, and `FirstApplicable` stops at the
first policy that is not `False`. A policy after that point is not evaluated and is absent from `PolicyDecision.Results`.

### 6. The decision is a `Decision`, fail-closed

`PolicyDecision` holds the `Outcome`, a `Decision` and the per-policy results. The `Decision.Result` is `True` for
`Permit`, `Unknown` for `Indeterminate` and `False` for `Deny` and for `NotApplicable`. `NotApplicable` is `False` because
no policy permits the request, which is the fail-closed default. The `Outcome` keeps the difference. The faults of every
evaluated rule are in `Decision.Faults`. `IsPermitted` is `Decision.IsSatisfied`, so only a permit is satisfied.

### 7. Errors

A bad argument to a constructor (a blank id, a duplicate policy id, an undefined enum value) throws
`ArgumentException`, as in other constructors of the library. Evaluation never throws for a rule that faults. Caller
cancellation and `EvaluationOptions.Timeout` of a rule throw `OperationCanceledException`, as in
[ADR-0002](0002-evaluation-semantics.md).

## Consequences

- A host gets one answer from many rules, with the `Unknown` rule of the rest of the library.
- The per-policy results show why the set answered as it did. Obligations, nested sets, a loader for policy files and
  analysis across policies (conflict and shadow detection) are not part of this change.
- Adding a fourth algorithm (for example `DenyUnlessPermit`) changes `CombiningAlgorithm` and `PolicySet.Combine` only.
