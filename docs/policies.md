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

A policy with a higher priority is evaluated first. Equal priorities keep the order in which the policies were given. Under `FirstApplicable` this decides which policy wins. Under the two overriding algorithms the outcome does not depend on the order, as long as no shared budget abort fires during the set's evaluation. A shared `FaultBudget` or `MaxElementEvaluations` abort, described under [Shared predicates](#shared-predicates), makes every rule that runs after it answer `Unknown`. A later-priority rule can then be forced to `Unknown` where an earlier position would have let it decide, so the outcome can depend on priority. Priority then decides which policies run before the evaluation stops, and the order of the results.

Evaluation stops early. The overriding algorithms stop at the first definite result of the overriding effect. `FirstApplicable` stops at the first policy that is not `False`. A policy that was not reached is not in `PolicyDecision.Results`.

## Shared predicates

A set evaluates the target, every policy's target and every policy's condition against one shared evaluation state for a context, so a predicate common to several of those rules runs once, not once per rule that references it. Two terms share this way when their predicate name and arguments are the same and the rules were compiled against the same predicate registry. Rules compiled against different registries never share a term, even when the name and arguments match: a `PolicySet` whose policies mix registries (a different compile mode, or a different implementation bound to the same predicate name) always evaluates each rule's own predicate. Data-source answers are shared as well. A `(source, query)` pair that the set's data sources answer is queried once for the whole set. A `from(variable, ...)` reference to a quantifier element is shared only between rules whose collection comes from the same registry, because two registries can project the elements of a like-named collection to different sources.

This sharing also makes the fault budget (`EvaluationOptions.FaultBudget`) a whole-set concept: once the Nth fault is recorded anywhere in the set's evaluation, every rule still to run answers `Unknown` for the rest of its own evaluation, the same way a fault budget aborts within one rule. A shared term that faults still counts once, not once per rule, so a budget is not spent faster by two rules naming the same predicate; each rule that references the faulting term still lists that one fault in `PolicyDecision.Decision.Faults`.

The element bound (`EvaluationOptions.MaxElementEvaluations`) is a whole-set bound too. Every quantifier element that any rule of the set evaluates counts against one total for the set's evaluation. A set of policies that each stay within the bound alone can exceed it together. When the total reaches the bound, the set aborts: every rule still to run answers `Unknown`, and the limit is recorded as a fault in `PolicyDecision.Decision.Faults`. A host with a set of many quantifier-heavy policies raises `MaxElementEvaluations` in the options that it passes to `EvaluateAsync`.

A `FORALL`/`EXISTS` quantifier's own fault (a throwing collection selector, `EvaluationOptions.MaxElementEvaluations` exceeded, or an unregistered collection) lists in `PolicyDecision.Decision.Faults` the same way: it belongs to every rule of the set that quantifies over that collection, and to no other rule.

## The decision

`PolicyDecision` has these members.

| Member | Meaning |
| --- | --- |
| `Outcome` | `Permit`, `Deny`, `NotApplicable` or `Indeterminate`. |
| `Decision` | A `Decision` with `True` for `Permit`, `Unknown` for `Indeterminate`, and `False` for `Deny` and `NotApplicable`. Its faults come from every rule that ran. |
| `Results` | The `PolicyResult` of each evaluated policy: its id, effect and whether it applies. |
| `IsPermitted` | `true` only for `Permit`. It is `Decision.IsSatisfied`, so it fails closed. |

## The trace

Set `EvaluationOptions.IncludeTrace` to `true` to get a trace of the set's evaluation in `PolicyDecision.Decision.Trace`. It is one flat trace of every rule that ran, in evaluation order. The text of each entry starts with the rule that the entry comes from:

| Prefix | Rule |
| --- | --- |
| `set target: ` | The target of the set. |
| `policy '<id>' target: ` | The target of the policy with that id. |
| `policy '<id>' condition: ` | The condition of the policy with that id. |

A rule that did not run has no entries. For example, a policy whose target is `False` has no condition entries, and the policies after the one that decided the outcome have no entries. A term that an earlier rule already evaluated still has an entry in each rule that references it.

Without `IncludeTrace`, `Decision.Trace` is `null` and the set collects no trace. The set's `Decision.TraceTree` is always `null`, because the set has no single expression tree.

## Errors

A constructor throws `ArgumentException` for a blank id, a duplicate policy id or an undefined enum value. A rule that faults does not throw: it is `Unknown` and the fault is in `Decision.Faults`. Cancellation of the caller token, or the `EvaluationOptions.Timeout` of a rule, throws `OperationCanceledException`.
