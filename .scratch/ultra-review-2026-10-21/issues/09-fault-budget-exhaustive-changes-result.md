# 09 - With FaultBudget set, Exhaustive mode changes Decision.Result, against the ADR-0002 invariant

Status: needs-triage
Severity: medium
Lens: F4 Evaluation
Verdict: CONFIRMED
Blocked by: none

## Finding F4.2: With FaultBudget set, Exhaustive mode changes Decision.Result, against the ADR-0002 invariant

Location: `src/TruthWeaver/Evaluation/Evaluator.cs:657` (CONFIRMED, medium)

ADR-0002 and docs/strong-k3/specification/evaluation.md say Exhaustive mode never changes Result. Exhaustive runs terms that ShortCircuit skips, so it records more faults and reaches the budget sooner. The abort then skips operands that would have settled the result. No doc states which rule wins when the two options are combined.

### Failure scenario

FaultBudget = 1, rule `(isOn OR boom) AND isOff`. ShortCircuit: the OR stops at isOn, boom never runs, isOff gives False. Exhaustive: boom runs and faults, the abort skips isOff, and Result is True with the current drop bug (Unknown even after that bug is fixed). The two modes give different results for the same input.

### Verification

I traced the scenario `(isOn OR boom) AND isOff` with FaultBudget=1. It does give different Results in the two modes.

- RecordFault (Evaluator.cs:650-661) sets state.Aborted = true once Faults.Count >= budget.
- ShortCircuit: the inner OR stops at isOn (Evaluator.cs:499-502), so boom never runs and no fault is recorded. The outer AND then runs isOff and returns False.
- Exhaustive: the OR does not stop (`!exhaustive && stops(...)` is false), so boom runs and faults. The budget is reached and Aborted becomes true.
- The outer AND loop (Evaluator.cs:488-494) then marks isOff as NotEvaluated and continues. It does not feed that operand into the accumulator, so AND(True, skipped) stays True. Result is True in Exhaustive and False in ShortCircuit.
- This contradicts docs/adr/0002-evaluation-semantics.md:141-145 and docs/strong-k3/specification/evaluation.md:29. Both say Exhaustive never changes Result, and ADR-0002 says the invariant must hold for any future change.
- Neither document says how FaultBudget interacts with Exhaustive. evaluation.md:23 only says the budget stops evaluation at the Nth fault.
- Two aspects are separate from this finding. Dropping aborted operands lets an abort produce a definite, even fail-open True, and that is a distinct defect. Also, the budget abort makes Result order- and mode-dependent whenever it fires.
- Even with the drop bug fixed (aborted operands counted as Unknown), Exhaustive would give Unknown and ShortCircuit would give False. So the mode-dependence stays.

I rate it medium rather than high. It needs the opt-in FaultBudget together with Exhaustive, which is documented as diagnostic/support use. The fail-open True comes from the separate drop bug.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
