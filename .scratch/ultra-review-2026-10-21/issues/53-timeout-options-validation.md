# 53 - Timeout and MaxElementEvaluations are not validated; Timeout is cooperative only

Status: needs-triage
Severity: low
Lens: F4 Evaluation
Verdict: PLAUSIBLE
Blocked by: none

## Finding F4.6: Timeout and MaxElementEvaluations are not validated; Timeout is cooperative only

Location: `src/TruthWeaver/Evaluation/CompiledRule.Evaluation.cs:55` (PLAUSIBLE, low)

FaultBudget below 1 is rejected up front. A negative Timeout (other than -1 ms) or one above int.MaxValue ms only fails inside CancelAfter, with an undocumented ArgumentOutOfRangeException. In EvaluateManyAsync that happens during enumeration, not at the call as the docs promise for bad options. MaxElementEvaluations <= 0 is accepted, and every quantifier then faults. The timeout is only noticed at the next token check, so a predicate that blocks synchronously and ignores the token runs past the 'overall wall-clock bound'.

### Failure scenario

EvaluationOptions(Timeout: TimeSpan.FromSeconds(-5)) passed to EvaluateManyAsync throws ArgumentOutOfRangeException from CancelAfter on the first MoveNextAsync. Separately, a Timeout of 1 s with a predicate doing a 30 s blocking call returns only after 30 s.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
