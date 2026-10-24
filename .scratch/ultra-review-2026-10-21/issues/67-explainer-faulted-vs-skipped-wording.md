# 67 - The explainer words for faulted (could not be evaluated) and skipped (not evaluated) are near-identical

Status: needs-triage
Severity: low
Lens: L5 Least surprise
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - reading the current source confirms DecisionExplainer.Phrase and TraceReasons.ForLeaf both render a faulted term as "could not be evaluated" while TraceReasons.Skipped and the notEvaluated JSON field (reserved for short-circuit skips per ADR-0002/CONTEXT.md) use "Not evaluated", the near-identical wording the ticket describes.
Blocked by: none

## Finding L5.4: "could not be evaluated" (faulted) and "not evaluated" (skipped) are near-identical words for different states

Location: `src/TruthWeaver/Evaluation/DecisionExplainer.cs:77` (PLAUSIBLE, low)

`Explain` phrases a faulted term as "could not be evaluated" (DecisionExplainer.cs:75-78; TraceReasons.cs:65 says the same), while the trace, the decision record and the debugger views use "not evaluated" / `notEvaluated` for a term that short-circuiting skipped (TraceEntry.cs:24, TraceNode.cs:36, TraceReasons.cs:7, docs/decision-records.md:173). ADR-0002 and the glossary define NotEvaluated as skipped only. A faulted term is `notEvaluated: false` in the same record.

### Failure scenario

Rule `isOff AND boom` evaluated in ShortCircuit mode: the record shows `boom` with `notEvaluated: true` (skipped). The same rule in Exhaustive mode, with `boom` throwing: `Explain` returns "Not satisfied." for the False result but for `boom OR isOff` returns "Unknown because boom could not be evaluated" while the record shows `notEvaluated: false`. A support reader searching logs for "not evaluated" conflates skipped and faulted terms.

### Expected behavior

One word per state: faulted terms say "faulted" (the project term in CONTEXT.md), and "not evaluated" is reserved for short-circuit skips.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
