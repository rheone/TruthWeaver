# 34 - The term memo is shared across rules compiled with different registries in one PolicySet

Status: needs-triage
Severity: medium
Lens: F9 Policies
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - a `PolicySet` target compiled Lenient against a registry missing `isAdmin` memoizes Unknown for the term, then a sibling policy's condition compiled Strict against a registry where `isAdmin` always returns True also comes back Unknown (confirmed True when evaluated standalone) because both share the same `(scope, TermIdentity)` memo key with no registry component.
Blocked by: none

## Finding F9.4: Term memo is shared across rules compiled with different registries

Location: `src/TruthWeaver.Policies/PolicySet{TContext}.cs:101` (PLAUSIBLE, medium)

The shared EvaluationState keys Memo by (scope, TermIdentity) and CollectionMaterializations by (scope, collection name). Neither key includes the registry. PolicySet accepts rules from any compiler and does not check that they share one PredicateRegistry. docs/policies.md states this ('whichever rule compiled them'). The result is that one rule's answer can stand in for a different predicate or a different compile mode.

### Failure scenario

The set target is compiled in Lenient mode against a registry where `isAdmin` is missing. It memoizes Unknown for isAdmin and records a fault. A permit policy compiled in Strict mode with a registered `isAdmin` then hits the memo: the real predicate never runs, the policy is Unknown, and the set is Indeterminate. In the same way, two registries that bind the name `isAdmin` to different implementations reuse whichever ran first.

### Expected behavior

Either the set rejects rules whose registries differ (or are incompatible), or the memo key includes the registry identity, so each rule evaluates its own predicates.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
