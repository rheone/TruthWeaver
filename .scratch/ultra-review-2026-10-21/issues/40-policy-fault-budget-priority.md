# 40 - A shared fault budget makes the overriding policy outcome depend on priority, against ADR-0015 section 4

Status: needs-triage
Severity: low
Lens: F9 Policies
Verdict: CONFIRMED
Blocked by: none

## Finding F9.2: Shared fault budget makes the overriding outcome depend on priority, against ADR-0015 section 4

Location: `src/TruthWeaver.Policies/PolicySet{TContext}.cs:101` (CONFIRMED, low)

ADR-0015 section 4 and docs/policies.md say that under DenyOverrides and PermitOverrides the outcome does not depend on order, and that a priority never changes an outcome. The set uses one EvaluationState, so FaultBudget and MaxElementEvaluations are spent across policies in priority order. After an earlier policy spends the budget, every later rule's root returns Unknown (Evaluator.cs:140). Reordering the policies then changes the outcome. The docs describe the shared budget but keep the order-independence claim. One of the two statements is wrong.

### Failure scenario

DenyOverrides with FaultBudget = 1. Permit policy P (priority 10) has a condition that faults. Deny policy D (priority 0) has `isBlocked`, which is True. Order P then D: P is Unknown and the budget aborts, so D is Unknown and the outcome is Indeterminate. Swap the priorities: D is True, the walk stops, and the outcome is Deny. Both results fail closed, but the documented invariant does not hold.

### Expected behavior

Either the docs and ADR say that a fault budget or element bound can make the outcome depend on priority, or the set gives each rule its own budget accounting, so the order-independence claim holds.

### Verification

I traced the path in the code. src/TruthWeaver.Policies/PolicySet{TContext}.cs:101 creates one EvaluationState for the whole set. Policies run in priority order (:59 sorts, :123 loops). Each rule is evaluated through rule.EvaluateAsync(context, sharedState, ...) (:171). Evaluator.cs:649-659 sets state.Aborted = true once state.Faults.Count >= FaultBudget. The state is shared, so the flag stays set for later rules. Evaluator.cs:140-148 then returns Unknown for the root node of every later rule without running it. Concrete case: DenyOverrides, FaultBudget = 1, permit policy P (priority 10) with a faulting condition, deny policy D (priority 0) with isBlocked = True. P runs first, faults and sets Aborted. D then returns Unknown, so Decides() never fires and Combine() reaches Has(Deny, Unknown) and returns Indeterminate. With D at the higher priority, D evaluates True, Decides() stops the walk, and the outcome is Deny. The outcome therefore depends on priority. docs/policies.md:62 says that under the overriding algorithms the outcome does not depend on the order. docs/policies.md:70 states the whole-set fault budget in the same page, so the two statements contradict each other. I did not open ADR-0015 section 4. The same shared-state flag applies to MaxElementEvaluations, because that limit also sets Aborted. Both results fail closed (Indeterminate maps to Unknown, and IsSatisfied is fail-closed), so this is an inconsistency between documented behaviour and a bounded edge case. It is not a security hole.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
