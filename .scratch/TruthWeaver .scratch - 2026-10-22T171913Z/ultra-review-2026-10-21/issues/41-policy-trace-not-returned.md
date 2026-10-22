# 41 - IncludeTrace collects a trace that PolicyDecision never returns

Status: needs-triage
Severity: low
Lens: F9 Policies
Verdict: PLAUSIBLE
Blocked by: none

## Finding F9.6: IncludeTrace collects a trace that PolicyDecision never returns

Location: `src/TruthWeaver.Policies/PolicySet{TContext}.cs:189` (PLAUSIBLE, low)

The shared state collects trace entries when options.IncludeTrace is true. Each rule's Decision copies the full trace so far, so the cost grows quadratically with the number of rules. Build then creates a new Decision with no trace, so the caller gets nothing for this cost. No doc says what IncludeTrace does for a set.

### Failure scenario

A caller passes EvaluationOptions(IncludeTrace: true) to a 50-policy set to debug a denial. Every rule allocates a copy of the growing trace, and PolicyDecision.Decision.Trace is still null.

### Expected behavior

Either the set's decision carries the trace (or each policy's trace), or the docs say that IncludeTrace has no effect on a set and the set does not collect one.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
