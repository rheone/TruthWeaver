# 51 - Collection-level faults skip RecordFault: no log, no fault metric

Status: needs-triage
Severity: low
Lens: F4 Evaluation
Verdict: PLAUSIBLE
Blocked by: none

## Finding F4.5: Collection-level faults skip RecordFault: no log, no fault metric

Location: `src/TruthWeaver/Registry/CollectionBinding.cs:111` (PLAUSIBLE, low)

A throwing selector and the MaxElementEvaluations fault add directly to state.Faults. Evaluator.RecordFault (Evaluator.cs:650) is the path that also writes EvaluationLog.PredicateFaulted and calls TruthWeaverMetrics.FaultRecorded. Its budget check is copied inline instead.

### Failure scenario

A collection selector throws in production. The Decision carries the fault, but no log line is written and the truthweaver fault counter does not increase, so dashboards under-report faults.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
