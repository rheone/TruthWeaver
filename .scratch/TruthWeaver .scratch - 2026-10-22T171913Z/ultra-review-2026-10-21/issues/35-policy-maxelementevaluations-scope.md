# 35 - MaxElementEvaluations is a whole-set bound but documented as per evaluation

Status: needs-triage
Severity: medium
Lens: F9 Policies
Verdict: PLAUSIBLE
Blocked by: none

## Finding F9.5: MaxElementEvaluations is a whole-set bound but is documented as per evaluation

Location: `src/TruthWeaver.Policies/PolicySet{TContext}.cs:101` (PLAUSIBLE, medium)

state.ElementEvaluations lives in the shared EvaluationState, so the 10,000 default bound (EvaluationOptions.cs:24-31, 'in one evaluation') is spent across all rules of the set. docs/policies.md and ADR-0015 say only the fault budget becomes a whole-set concept. When the bound is hit, the set aborts and every later rule is Unknown. Because of the first finding, the limit fault is also dropped.

### Failure scenario

Three policies each contain `FORALL(l IN lines, ...)` over a 4,000-line order. Each rule alone is under the limit. In the set, the third quantifier exceeds 10,000, the evaluation aborts, the third and later policies are Unknown, and the outcome is Indeterminate with no fault in PolicyDecision.Decision.Faults.

### Expected behavior

docs/policies.md says the element bound is shared across the set, or the set accounts for the element bound per rule.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
