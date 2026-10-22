# 54 - Evaluator hard-codes operator trace labels instead of reading OperatorDefinitions

Status: needs-triage
Severity: low
Lens: F4 Evaluation
Verdict: PLAUSIBLE
Blocked by: none

## Finding F4.7: Evaluator hard-codes operator trace labels instead of reading OperatorDefinitions

Location: `src/TruthWeaver/Evaluation/Evaluator.cs:178` (PLAUSIBLE, low)

Evaluated nodes get literal labels ("AND", "OR", "COALESCE", "XOR", "PARITY", "ExactlyOne", and $"{OpName}({K})" for thresholds). Skipped nodes go through TraceText.Of, which reads OperatorDefinitions.Label. CLAUDE.md says the Evaluator trace labels come from that table, and RuleCoverageAccumulator compares trace text against TraceText output.

### Failure scenario

A change to the PARITY label in OperatorDefinitions (for example "Parity") changes skipped nodes and coverage text, but evaluated nodes still say "PARITY". Trace tree labels then disagree, and coverage matching against the trace breaks.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
